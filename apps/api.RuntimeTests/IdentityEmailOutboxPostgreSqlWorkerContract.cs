using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using YangSpace.Api;

internal static class IdentityEmailOutboxPostgreSqlWorkerContract
{
    private static void Check(bool ok,string marker)
    {
        if(!ok) throw new InvalidOperationException("F2B_O3_PG_FAILED="+marker);
        Console.WriteLine(marker+"=PASS");
    }

    internal static async Task RunAsync()
    {
        var migratorCs=Required("YANGSPACE_F2B_O3_PG_MIGRATOR_CONNECTION");
        var runtimeCs=Required("YANGSPACE_F2B_O3_PG_RUNTIME_CONNECTION");
        var scopeToken=Required("YANGSPACE_F2B_O3_PG_SCOPE_TOKEN");
        var keys=Required("YANGSPACE_F2B_O3_PG_KEY_DIR");
        Directory.CreateDirectory(keys);
        try
        {
            ValidateConnection(migratorCs,"ys_o3_migrator");
            ValidateConnection(runtimeCs,"ys_o3_runtime");
            var migratorServices=CreateIdentityServices(migratorCs,keys);
            using var migratorProvider=migratorServices.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes=true, ValidateOnBuild=true });
            await using var migratorScope=migratorProvider.CreateAsyncScope();
            var migratorDb=migratorScope.ServiceProvider.GetRequiredService<DirectoryDb>();
            await migratorDb.Database.MigrateAsync();
            var applied=(await migratorDb.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(applied.Contains("20261115101421_F2bIdentityEmailOutbox") &&
                !(await migratorDb.Database.GetPendingMigrationsAsync()).Any(),"F2B_O3_PG_MIGRATIONS_APPLIED");
            Check(await VerifyScope(migratorCs,scopeToken,"ys_o3_migrator"),"F2B_O3_PG_MIGRATOR_TLS_SCOPE");
            await GrantRuntime(migratorCs);
            Check(await VerifyRuntimePrivileges(runtimeCs,scopeToken),"F2B_O3_PG_RUNTIME_LEAST_PRIVILEGE");
            Check(await DeniedOperations(runtimeCs),"F2B_O3_PG_RUNTIME_DENIED_OPERATIONS");

            var clock=new MutableClock(DateTimeOffset.UtcNow);
            using var releaseAuthorization=new TestReleaseAuthorizationScope(
                clock.GetUtcNow(),Required("YANGSPACE_RELEASE_COMMIT_SHA"));
            var fake=new FakeTransport();
            var logCapture = new CapturingLoggerProvider();
            var metricNames = new ConcurrentBag<string>();
            var metricTagKeys = new ConcurrentBag<string>();
            using var metricListener = new MeterListener();
            metricListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == IdentityEmailOutboxWorkerTelemetry.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            };
            metricListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                metricNames.Add(instrument.Name);
                foreach (var tag in tags) metricTagKeys.Add(tag.Key);
            });
            metricListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                metricNames.Add(instrument.Name);
                foreach (var tag in tags) metricTagKeys.Add(tag.Key);
            });
            metricListener.Start();
            var config=releaseAuthorization.Configuration;
            Check(IdentityEmailOutboxDispatchPolicy.IsEnabled(
                config,releaseAuthorization.HostEnvironment,clock),
                "F2B_O3_PG_SIGNED_RELEASE_AUTHORIZATION");
            var runtimeServices=CreateIdentityServices(runtimeCs,keys);
            runtimeServices.AddLogging(logging =>
            {
                logging.AddProvider(logCapture);
                logging.SetMinimumLevel(LogLevel.Debug);
            });
            runtimeServices.AddSingleton<TimeProvider>(clock);
            runtimeServices.AddSingleton<IHostEnvironment>(releaseAuthorization.HostEnvironment);
            runtimeServices.AddSingleton<IConfiguration>(config);
            runtimeServices.AddScoped<IEmailSender<IdentityUser>,OutboxIdentityEmailSender>();
            runtimeServices.AddSingleton<IIdentityEmailOutboxTransport>(fake);
            runtimeServices.AddHostedService<IdentityEmailOutboxWorker>();
            runtimeServices.Configure<IdentityEmailOutboxOptions>(o=>{
                o.LeaseSeconds=30; o.BatchSize=20; o.MaxAttempts=3;
                o.SentRetentionDays=30; o.SuppressedRetentionDays=30; o.DeadLetterRetentionDays=90;
            });
            using var runtimeProvider=runtimeServices.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes=true, ValidateOnBuild=true });
            await using var runtimeScope=runtimeProvider.CreateAsyncScope();
            var db=runtimeScope.ServiceProvider.GetRequiredService<DirectoryDb>();
            var queue=runtimeScope.ServiceProvider.GetRequiredService<IEmailSender<IdentityUser>>();
            var users=migratorScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Check(await VerifyScope(runtimeCs,scopeToken,"ys_o3_runtime"),"F2B_O3_PG_RUNTIME_TLS_SCOPE");
            Check(IdentityEmailOutboxDispatchPolicy.DispatcherAvailable,"F2B_O3_PG_DISPATCH_IMPLEMENTATION_AVAILABLE");
            Check(runtimeProvider.GetServices<IHostedService>().Any(x=>x is IdentityEmailOutboxWorker),"F2B_O3_PG_DI_ACTIVATION");
            Check(ReferenceEquals(runtimeScope.ServiceProvider.GetRequiredService<IIdentityEmailOutboxTransport>(),fake),"F2B_O3_PG_FAKE_TRANSPORT_ONLY");

            var worker=NewWorker(runtimeProvider,clock,config);
            var user=await NewUser(users,"pg-worker-confirm@example.invalid",false);
            await queue.SendConfirmationLinkAsync(user,user.Email!,"discarded");
            var id=await IntentId(db,user.Id);
            await worker.ProcessBatchOnceAsync();
            var row=await Message(db,id);
            var delivery=fake.Deliveries.Single(x=>x.Email==user.Email);
            var code=QueryHelpers.ParseQuery(new Uri(delivery.Payload).Query)["code"].ToString();
            var tokenValue=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var confirmed=await users.ConfirmEmailAsync(user,tokenValue);
            Check(row.Status==IdentityEmailOutboxStatus.Sent && confirmed.Succeeded,"F2B_O3_PG_CONFIRMATION_DISPATCH");
            await queue.SendPasswordResetCodeAsync(user,user.Email!,"discarded");
            var resetId=await db.IdentityEmailOutbox.AsNoTracking()
                .Where(x=>x.UserId==user.Id && x.Purpose==IdentityEmailPurpose.ResetPassword &&
                    x.Status==IdentityEmailOutboxStatus.Pending).Select(x=>x.Id).SingleAsync();
            await worker.ProcessBatchOnceAsync();
            var resetDelivery=fake.Deliveries.Last(x=>x.Email==user.Email);
            var resetToken=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetDelivery.Payload));
            var reset=await users.ResetPasswordAsync(user,resetToken,"ReplacementPassword123");
            Check(reset.Succeeded && (await Message(db,resetId)).Status==IdentityEmailOutboxStatus.Sent,
                "F2B_O3_PG_PASSWORD_RESET");

            var suppressed=await NewUser(users,"pg-worker-suppressed@example.invalid",true);
            await queue.SendConfirmationLinkAsync(suppressed,suppressed.Email!,"discarded");
            var suppressedId=await IntentId(db,suppressed.Id);
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,suppressedId)).Status==IdentityEmailOutboxStatus.Suppressed &&
                !fake.Deliveries.Any(x=>x.Email==suppressed.Email),
                "F2B_O3_PG_CONFIRMED_USER_SUPPRESSION");

            var retry=await NewUser(users,"pg-worker-retry@example.invalid",false);
            await queue.SendConfirmationLinkAsync(retry,retry.Email!,"discarded");
            var retryId=await IntentId(db,retry.Id);
            fake.FailBeforeOnce();
            await worker.ProcessBatchOnceAsync();
            var failed=await Message(db,retryId);
            Check(failed.Status==IdentityEmailOutboxStatus.Pending && failed.AttemptCount==1 &&
                failed.LastFailureCode=="dispatch_failed","F2B_O3_PG_RETRY_BACKOFF");
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,retryId)).Status==IdentityEmailOutboxStatus.Sent,"F2B_O3_PG_RETRY_RECOVERY");

            var uncertain=await NewUser(users,"pg-worker-uncertain@example.invalid",false);
            await queue.SendConfirmationLinkAsync(uncertain,uncertain.Email!,"discarded");
            var uncertainId=await IntentId(db,uncertain.Id);
            fake.FailAfterOnce();
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,uncertainId)).Status==IdentityEmailOutboxStatus.Pending,
                "F2B_O3_PG_UNCERTAIN_ACK_PENDING_RETRY");
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,uncertainId)).Status==IdentityEmailOutboxStatus.Sent &&
                fake.Deliveries.Count(x=>x.Email==uncertain.Email)==2,
                "F2B_O3_PG_UNCERTAIN_ACK_AT_LEAST_ONCE");

            var concurrent=await NewUser(users,"pg-worker-race@example.invalid",false);
            await queue.SendConfirmationLinkAsync(concurrent,concurrent.Email!,"discarded");
            var raceId=await IntentId(db,concurrent.Id);
            fake.BlockNext();
            var first=worker.ProcessBatchOnceAsync();
            var second=NewWorker(runtimeProvider,clock,config).ProcessBatchOnceAsync();
            await fake.WaitBlockedAsync();
            fake.Release();
            var counts=await Task.WhenAll(first,second);
            Check(counts.Sum()==1 && (await Message(db,raceId)).Status==IdentityEmailOutboxStatus.Sent &&
                fake.Deliveries.Count(x=>x.Email==concurrent.Email)==1,"F2B_O3_PG_CONCURRENT_CLAIM");

            var lease=await NewUser(users,"pg-worker-lease@example.invalid",false);
            await queue.SendConfirmationLinkAsync(lease,lease.Email!,"discarded");
            var leaseId=await IntentId(db,lease.Id);
            await db.IdentityEmailOutbox.Where(x=>x.Id==leaseId).ExecuteUpdateAsync(s=>s
                .SetProperty(x=>x.Status,IdentityEmailOutboxStatus.Leased)
                .SetProperty(x=>x.LeaseId,(Guid?)Guid.NewGuid())
                .SetProperty(x=>x.LeasedUntilUtc,(DateTime?)clock.GetUtcNow().UtcDateTime.AddSeconds(-1))
                .SetProperty(x=>x.AttemptCount,1)
                .SetProperty(x=>x.Revision,x=>x.Revision+1));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,leaseId)).Status==IdentityEmailOutboxStatus.Sent &&
                (await Message(db,leaseId)).AttemptCount==2,"F2B_O3_PG_LEASE_EXPIRY_RECOVERY");

            var crashed=await NewUser(users,"pg-worker-crash@example.invalid",false);
            await queue.SendConfirmationLinkAsync(crashed,crashed.Email!,"discarded");
            var crashedId=await IntentId(db,crashed.Id);
            fake.BlockNext();
            using(var canceled=new CancellationTokenSource())
            {
                var dispatch=worker.ProcessBatchOnceAsync(canceled.Token);
                await fake.WaitBlockedAsync();
                canceled.Cancel();
                var observed=false;
                try { await dispatch; } catch(OperationCanceledException) { observed=true; }
                Check(observed && (await Message(db,crashedId)).Status==IdentityEmailOutboxStatus.Leased,
                    "F2B_O3_PG_CRASH_LEASE_PRESERVED");
            }
            fake.Release();
            clock.Advance(TimeSpan.FromSeconds(31));
            await NewWorker(runtimeProvider,clock,config).ProcessBatchOnceAsync();
            Check((await Message(db,crashedId)).Status==IdentityEmailOutboxStatus.Sent &&
                (await Message(db,crashedId)).AttemptCount==2 &&
                fake.Deliveries.Count(x=>x.Email==crashed.Email)==1,
                "F2B_O3_PG_CRASH_RESTART_RECOVERY");

            var poison=await NewUser(users,"pg-worker-poison@example.invalid",false);
            await queue.SendConfirmationLinkAsync(poison,poison.Email!,"discarded");
            var poisonId=await IntentId(db,poison.Id);
            fake.FailAlways=true;
            await worker.ProcessBatchOnceAsync();
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            clock.Advance(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(1)));
            await worker.ProcessBatchOnceAsync();
            fake.FailAlways=false;
            Check((await Message(db,poisonId)).Status==IdentityEmailOutboxStatus.DeadLetter &&
                (await Message(db,poisonId)).AttemptCount==3,"F2B_O3_PG_DEADLETTER");
            var replay=await IdentityEmailOutboxAdminEndpoints.ReplayDeadLetterAsync(
                db,poisonId,clock,CancellationToken.None);
            await worker.ProcessBatchOnceAsync();
            Check(replay==IdentityEmailOutboxReplayResult.Replayed &&
                (await Message(db,poisonId)).Status==IdentityEmailOutboxStatus.Sent,
                "F2B_O3_PG_OPERATOR_REPLAY");

            var now=clock.GetUtcNow().UtcDateTime;
            var obsolete=new[]{
                new IdentityEmailOutboxMessage {
                    UserId=user.Id,Purpose=IdentityEmailPurpose.ConfirmEmail,
                    Status=IdentityEmailOutboxStatus.Sent,QueuedAtUtc=now.AddDays(-40),
                    AvailableAtUtc=now.AddDays(-40),ExpiresAtUtc=now.AddDays(-39),
                    SentAtUtc=now.AddDays(-31)
                },
                new IdentityEmailOutboxMessage {
                    UserId=user.Id,Purpose=IdentityEmailPurpose.ResetPassword,
                    Status=IdentityEmailOutboxStatus.Suppressed,QueuedAtUtc=now.AddDays(-40),
                    AvailableAtUtc=now.AddDays(-40),ExpiresAtUtc=now.AddDays(-31)
                },
                new IdentityEmailOutboxMessage {
                    UserId=user.Id,Purpose=IdentityEmailPurpose.ConfirmEmail,
                    Status=IdentityEmailOutboxStatus.DeadLetter,QueuedAtUtc=now.AddDays(-100),
                    AvailableAtUtc=now.AddDays(-100),ExpiresAtUtc=now.AddDays(-99),
                    DeadLetterAtUtc=now.AddDays(-91)
                }
            };
            db.IdentityEmailOutbox.AddRange(obsolete);
            await db.SaveChangesAsync();
            var obsoleteIds=obsolete.Select(x=>x.Id).ToArray();
            await worker.ProcessBatchOnceAsync();
            Check(!await db.IdentityEmailOutbox.AnyAsync(x=>obsoleteIds.Contains(x.Id)),
                "F2B_O3_PG_RETENTION_PURGE");

            var forbidden=typeof(IdentityEmailOutboxMessage).GetProperties().Select(x=>x.Name)
                .Any(x=>x.Contains("Token",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Email",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Recipient",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Body",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Secret",StringComparison.OrdinalIgnoreCase));
            Check(!forbidden && (await db.IdentityEmailOutbox.AsNoTracking().ToArrayAsync())
                .All(x=>x.UserId!=user.Email),"F2B_O3_PG_NO_TOKEN_OR_RECIPIENT_AT_REST");

            // H05c: verify the actual operational projection against persisted PG17.
            // Expose only count and UTC time fields (no identity/user/message data).

            var staleSentUser=await NewUser(users,"pg-worker-stale-sent@example.invalid",false);
            await queue.SendConfirmationLinkAsync(staleSentUser,staleSentUser.Email!,"discarded");
            var staleSentId=await IntentId(db,staleSentUser.Id);
            fake.BlockNext();
            var staleSentTask=worker.ProcessBatchOnceAsync();
            await fake.WaitBlockedAsync();
            clock.Advance(TimeSpan.FromSeconds(31));
            await NewWorker(runtimeProvider,clock,config).ProcessBatchOnceAsync();
            var currentOwnerState=await Message(db,staleSentId);
            Check(currentOwnerState.Status==IdentityEmailOutboxStatus.Sent &&
                currentOwnerState.AttemptCount==2,
                "F2B_H05E_NEW_LEASE_OWNER_FINALIZED");
            fake.Release();
            await staleSentTask;
            var afterStaleSent=await Message(db,staleSentId);
            Check(afterStaleSent.Status==currentOwnerState.Status &&
                afterStaleSent.Revision==currentOwnerState.Revision &&
                afterStaleSent.SentAtUtc==currentOwnerState.SentAtUtc &&
                fake.Deliveries.Count(x=>x.Email==staleSentUser.Email)==2,
                "F2B_H05E_STALE_LEASE_OWNER_CANNOT_FINALIZE");

            var staleRetryUser=await NewUser(users,"pg-worker-stale-retry@example.invalid",false);
            await queue.SendConfirmationLinkAsync(staleRetryUser,staleRetryUser.Email!,"discarded");
            var staleRetryId=await IntentId(db,staleRetryUser.Id);
            fake.BlockNextThenFailBeforeAccept();
            var staleRetryTask=worker.ProcessBatchOnceAsync();
            await fake.WaitBlockedAsync();
            clock.Advance(TimeSpan.FromSeconds(31));
            await NewWorker(runtimeProvider,clock,config).ProcessBatchOnceAsync();
            var retryOwnerState=await Message(db,staleRetryId);
            fake.Release();
            await staleRetryTask;
            var afterStaleRetry=await Message(db,staleRetryId);
            Check(retryOwnerState.Status==IdentityEmailOutboxStatus.Sent &&
                afterStaleRetry.Status==retryOwnerState.Status &&
                afterStaleRetry.Revision==retryOwnerState.Revision &&
                afterStaleRetry.AttemptCount==retryOwnerState.AttemptCount &&
                fake.Deliveries.Count(x=>x.Email==staleRetryUser.Email)==1,
                "F2B_H05E_STALE_LEASE_OWNER_CANNOT_RETRY");

            var finalizationUser=await NewUser(users,"pg-worker-finalization@example.invalid",false);
            await queue.SendConfirmationLinkAsync(finalizationUser,finalizationUser.Email!,"discarded");
            var finalizationId=await IntentId(db,finalizationUser.Id);
            await InstallSentFinalizationFailureAsync(migratorCs,finalizationId);
            var finalizationFailureObserved=false;
            try
            {
                await worker.ProcessBatchOnceAsync();
            }
            catch(Exception exception) when(HasSqlState(exception,"P0001"))
            {
                finalizationFailureObserved=true;
            }
            finally
            {
                await RemoveSentFinalizationFailureAsync(migratorCs);
            }

            var afterFinalizationFailure=await Message(db,finalizationId);
            Check(finalizationFailureObserved &&
                afterFinalizationFailure.Status==IdentityEmailOutboxStatus.Leased &&
                afterFinalizationFailure.AttemptCount==1 &&
                afterFinalizationFailure.SentAtUtc is null &&
                afterFinalizationFailure.LastFailureCode is null &&
                fake.Deliveries.Count(x=>x.Email==finalizationUser.Email)==1,
                "F2B_H05E_SENT_FINALIZATION_DB_FAILURE_NOT_FALSE_SENT");
            var failureSnapshot=runtimeProvider.GetRequiredService<IdentityEmailOutboxWorkerTelemetry>().Snapshot();
            Check(failureSnapshot.LastCycleFailureCategory=="database" &&
                failureSnapshot.ConsecutiveFailures==1,
                "F2B_H05E_DATABASE_FAILURE_CLASSIFIED");
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            var afterFinalizationRecovery=await Message(db,finalizationId);
            Check(afterFinalizationRecovery.Status==IdentityEmailOutboxStatus.Sent &&
                afterFinalizationRecovery.AttemptCount==2 &&
                fake.Deliveries.Count(x=>x.Email==finalizationUser.Email)==2,
                "F2B_H05E_SENT_FINALIZATION_AT_LEAST_ONCE_RECOVERY");

            var summary = await IdentityEmailOutboxAdminEndpoints.ReadSummaryAsync(
                db, clock, CancellationToken.None);
            var expectedSent = await db.IdentityEmailOutbox.AsNoTracking()
                .LongCountAsync(x => x.Status == IdentityEmailOutboxStatus.Sent);
            var expectedPending = await db.IdentityEmailOutbox.AsNoTracking()
                .LongCountAsync(x => x.Status == IdentityEmailOutboxStatus.Pending);
            var expectedDeadLetter = await db.IdentityEmailOutbox.AsNoTracking()
                .LongCountAsync(x => x.Status == IdentityEmailOutboxStatus.DeadLetter);
            Check(summary.Sent == expectedSent &&
                summary.Pending == expectedPending &&
                summary.DeadLetter == expectedDeadLetter &&
                summary.Due <= summary.Pending &&
                summary.RetryPending <= summary.Pending &&
                summary.ObservedAtUtc == clock.GetUtcNow().UtcDateTime,
                "F2B_H05C_OUTBOX_SUMMARY_PERSISTED_COUNTS");

            var summaryJson = System.Text.Json.JsonSerializer.Serialize(summary);
            Check(!summaryJson.Contains("@", StringComparison.Ordinal) &&
                !summaryJson.Contains("UserId", StringComparison.OrdinalIgnoreCase) &&
                !summaryJson.Contains("Email", StringComparison.OrdinalIgnoreCase) &&
                !summaryJson.Contains("Token", StringComparison.OrdinalIgnoreCase) &&
                !summaryJson.Contains("Payload", StringComparison.OrdinalIgnoreCase) &&
                !summaryJson.Contains("Secret", StringComparison.OrdinalIgnoreCase),
                "F2B_H05C_OUTBOX_SUMMARY_NO_PII");

            metricListener.RecordObservableInstruments();
            var telemetrySnapshot=runtimeProvider
                .GetRequiredService<IdentityEmailOutboxWorkerTelemetry>().Snapshot();
            Check(telemetrySnapshot.TotalCycles>0 &&
                telemetrySnapshot.SuccessfulCycles>0 &&
                telemetrySnapshot.FailedCycles>0 &&
                telemetrySnapshot.LastCycleSucceededAtUtc is not null &&
                telemetrySnapshot.ConsecutiveFailures==0 &&
                telemetrySnapshot.TotalSent>0 &&
                telemetrySnapshot.TotalRetried>0 &&
                telemetrySnapshot.TotalLeaseLost>0 &&
                telemetrySnapshot.Pending>=0 && telemetrySnapshot.DeadLetter>=0,
                "F2B_H05E_WORKER_OBSERVABILITY_SNAPSHOT");
            Check(metricNames.Contains("yangspace.identity_email_outbox.worker.cycles") &&
                metricNames.Contains("yangspace.identity_email_outbox.messages") &&
                metricNames.Contains("yangspace.identity_email_outbox.pending") &&
                metricNames.Contains("yangspace.identity_email_outbox.dead_letter") &&
                metricTagKeys.Count>0 &&
                metricTagKeys.All(key=>key is "outcome" or "failure_category"),
                "F2B_H05E_METRICS_LOW_CARDINALITY");
            var workerLogs=logCapture.Messages;
            var identityValues=await db.Users.AsNoTracking()
                .Select(x=>new { x.Id,x.Email }).ToArrayAsync();
            var sensitiveValues=identityValues
                .SelectMany(x=>new[]{x.Id,x.Email})
                .Concat(fake.Deliveries.SelectMany(x=>new[]{x.Email,x.Payload}))
                .Concat(new[]{migratorCs,runtimeCs,scopeToken})
                .OfType<string>()
                .Where(value=>value.Length>0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var forbiddenPropertyNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "Email","UserId","Token","Payload","Secret","Recipient","LeaseId"
            };
            Check(workerLogs.Any(entry=>entry.Message.Contains("IDENTITY_EMAIL_OUTBOX_TRANSPORT_FAILED",StringComparison.Ordinal)) &&
                workerLogs.Any(entry=>entry.Message.Contains("IDENTITY_EMAIL_OUTBOX_WORKER_CYCLE_FAILED",StringComparison.Ordinal)) &&
                workerLogs.All(entry=>
                    !entry.Message.Contains("@",StringComparison.Ordinal) &&
                    !entry.Message.Contains("UserId=",StringComparison.OrdinalIgnoreCase) &&
                    !entry.Message.Contains("Email=",StringComparison.OrdinalIgnoreCase) &&
                    !entry.Message.Contains("Token=",StringComparison.OrdinalIgnoreCase) &&
                    !entry.Message.Contains("Payload=",StringComparison.OrdinalIgnoreCase) &&
                    !entry.Message.Contains("Secret=",StringComparison.OrdinalIgnoreCase) &&
                    !entry.Message.Contains("LeaseId=",StringComparison.OrdinalIgnoreCase) &&
                    !entry.Properties.Keys.Any(forbiddenPropertyNames.Contains) &&
                    !sensitiveValues.Any(value=>
                        entry.Message.Contains(value,StringComparison.Ordinal) ||
                        (entry.Exception?.Contains(value,StringComparison.Ordinal) ?? false) ||
                        entry.Properties.Values.Any(propertyValue=>
                            propertyValue?.Contains(value,StringComparison.Ordinal) ?? false)) &&
                    !(entry.Exception?.Contains("@",StringComparison.Ordinal) ?? false)),
                "F2B_H05E_WORKER_LOGS_NO_PII");

            Console.WriteLine("F2B_O3_WORKER_NPGSQL_PROVIDER=PASS");
            Console.WriteLine("F2B_O3_WORKER_POSTGRESQL_17=PASS");
            Console.WriteLine("F2B_O3_WORKER_TLS_VERIFYFULL=PASS");
            Console.WriteLine("F2B_O3_WORKER_RUNTIME_LEAST_PRIVILEGE=PASS");
            Console.WriteLine("F2B_O3_WORKER_FAKE_TRANSPORT_ONLY=PASS");
            Console.WriteLine("F2B_O3_WORKER_DISPOSABLE_ONLY=PASS");
            Console.WriteLine("F2B_O3_REAL_SMTP=NOT_CONNECTED");
        }
        finally { if(Directory.Exists(keys)) Directory.Delete(keys,true); }
    }

    private static string Required(string name)=>Environment.GetEnvironmentVariable(name) is { Length:>0 } v
        ? v : throw new InvalidOperationException("F2B_O3_PG_ENV_MISSING="+name);

    private static void ValidateConnection(string value,string role)
    {
        var cs=new NpgsqlConnectionStringBuilder(value);
        if(cs.Host!="localhost" || cs.Port!=5432 || cs.Database!="yangspace_f2b_o3_worker_disposable" ||
            cs.Username!=role || cs.SslMode!=SslMode.VerifyFull || cs.Pooling || cs.IncludeErrorDetail ||
            !cs.ConnectionString.Contains("Root Certificate=/run/pgcerts/ca.crt",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("F2B_O3_PG_CONNECTION_REFUSED="+role);
    }

    private static ServiceCollection CreateIdentityServices(string cs,string keys)
    {
        var services=new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IdentityEmailOutboxWorkerTelemetry>();
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys)).SetApplicationName("YangSpace-F2b-O3-PG");
        services.AddDbContext<DirectoryDb>(o=>o.UseNpgsql(cs,n=>n.MigrationsAssembly("YangSpace.PostgreSql.Migrations")));
        services.AddIdentityCore<IdentityUser>(o=>{
            o.User.RequireUniqueEmail=true;
            o.Password.RequiredLength=8;
            o.Password.RequireDigit=false;
            o.Password.RequireUppercase=false;
            o.Password.RequireNonAlphanumeric=false;
        })
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<DirectoryDb>().AddDefaultTokenProviders();
        return services;
    }

    private static async Task InstallSentFinalizationFailureAsync(string connection, Guid id)
    {
        await using var c=new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd=c.CreateCommand();
        cmd.CommandText=$"""
            CREATE OR REPLACE FUNCTION public.h05e_fail_sent_finalization() RETURNS trigger
            LANGUAGE plpgsql AS $h05e$
            BEGIN
                IF NEW."Id" = '{id:D}'::uuid AND NEW."Status" = 'Sent' THEN
                    RAISE EXCEPTION 'H05E_SYNTHETIC_SENT_FINALIZATION_FAILURE'
                        USING ERRCODE = 'P0001';
                END IF;
                RETURN NEW;
            END;
            $h05e$;
            DROP TRIGGER IF EXISTS h05e_fail_sent_finalization
                ON public."IdentityEmailOutbox";
            CREATE TRIGGER h05e_fail_sent_finalization
                BEFORE UPDATE OF "Status" ON public."IdentityEmailOutbox"
                FOR EACH ROW EXECUTE FUNCTION public.h05e_fail_sent_finalization();
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RemoveSentFinalizationFailureAsync(string connection)
    {
        await using var c=new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd=c.CreateCommand();
        cmd.CommandText="""
            DROP TRIGGER IF EXISTS h05e_fail_sent_finalization
                ON public."IdentityEmailOutbox";
            DROP FUNCTION IF EXISTS public.h05e_fail_sent_finalization();
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool HasSqlState(Exception exception,string sqlState)
    {
        for(Exception? current=exception;current is not null;current=current.InnerException)
            if(current is PostgresException postgres && postgres.SqlState==sqlState)
                return true;
        return false;
    }

    private static async Task<bool> VerifyScope(string cs,string token,string role)
    {
        await using var c=new NpgsqlConnection(cs); await c.OpenAsync();
        await using var cmd=c.CreateCommand();
        cmd.CommandText="""
            SELECT current_database()='yangspace_f2b_o3_worker_disposable'
             AND current_user=@role
             AND current_setting('server_version_num')::integer BETWEEN 170000 AND 179999
             AND (SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid())
             AND (SELECT scope_token FROM public.yangspace_f2b_o3_qa_guard LIMIT 1)=@token
             AND NOT (SELECT rolsuper FROM pg_roles WHERE rolname=current_user)

            """;
        cmd.Parameters.AddWithValue("role",role); cmd.Parameters.AddWithValue("token",token);
        return (bool)(await cmd.ExecuteScalarAsync()??false);
    }

    private static async Task GrantRuntime(string cs)
    {
        await using var c=new NpgsqlConnection(cs); await c.OpenAsync();
        await using var cmd=c.CreateCommand();
        cmd.CommandText="""
            GRANT SELECT ("Id","Email","EmailConfirmed","SecurityStamp") ON TABLE public."AspNetUsers" TO ys_o3_runtime;
            GRANT SELECT ("UserId","RoleId") ON TABLE public."AspNetUserRoles" TO ys_o3_runtime;
            GRANT SELECT ("Id","Name") ON TABLE public."AspNetRoles" TO ys_o3_runtime;
            GRANT SELECT,INSERT,UPDATE,DELETE ON TABLE public."IdentityEmailOutbox" TO ys_o3_runtime;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<bool> VerifyRuntimePrivileges(string cs,string token)
    {
        await using var c=new NpgsqlConnection(cs); await c.OpenAsync();
        await using var cmd=c.CreateCommand();
        cmd.CommandText="""
            SELECT current_user='ys_o3_runtime'
             AND (SELECT scope_token FROM public.yangspace_f2b_o3_qa_guard LIMIT 1)=@token
             AND has_schema_privilege(current_user,'public','USAGE')
             AND NOT has_schema_privilege(current_user,'public','CREATE')
             AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','SELECT')
             AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','INSERT')
             AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','UPDATE')
             AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','DELETE')
             AND NOT has_table_privilege(current_user,'public."AspNetUsers"','SELECT')
             AND has_column_privilege(current_user,'public."AspNetUsers"','Id','SELECT')
             AND has_column_privilege(current_user,'public."AspNetUsers"','Email','SELECT')
             AND has_column_privilege(current_user,'public."AspNetUsers"','EmailConfirmed','SELECT')
             AND has_column_privilege(current_user,'public."AspNetUsers"','SecurityStamp','SELECT')
             AND NOT has_column_privilege(current_user,'public."AspNetUsers"','PasswordHash','SELECT')
             AND NOT has_column_privilege(current_user,'public."AspNetUsers"','ConcurrencyStamp','SELECT')
             AND NOT has_table_privilege(current_user,'public."Organizations"','SELECT')
            """;
        cmd.Parameters.AddWithValue("token",token);
        return (bool)(await cmd.ExecuteScalarAsync()??false);
    }

    private static async Task<bool> DeniedOperations(string cs)
    {
        foreach(var sql in new[]{
            "CREATE TABLE public.o3_runtime_denied(id integer)",
            "SELECT \"PasswordHash\" FROM public.\"AspNetUsers\" LIMIT 0",
            "UPDATE public.\"AspNetUsers\" SET \"EmailConfirmed\"=\"EmailConfirmed\" WHERE false",
            "TRUNCATE TABLE public.\"IdentityEmailOutbox\"",
            "SELECT * FROM public.\"Organizations\" LIMIT 0"
        }){
            await using var c=new NpgsqlConnection(cs); await c.OpenAsync();
            await using var cmd=c.CreateCommand(); cmd.CommandText=sql;
            try{await cmd.ExecuteNonQueryAsync();return false;}
            catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.InsufficientPrivilege){}
        }
        return true;
    }

    private static IdentityEmailOutboxWorker NewWorker(IServiceProvider p,TimeProvider clock,IConfiguration config)=>
        new(p.GetRequiredService<IServiceScopeFactory>(),p.GetRequiredService<ILogger<IdentityEmailOutboxWorker>>(),
            p.GetRequiredService<IdentityEmailOutboxWorkerTelemetry>(),
            p.GetRequiredService<IOptions<IdentityEmailOutboxOptions>>(),clock,config,
            p.GetRequiredService<IHostEnvironment>());

    private static async Task<IdentityUser> NewUser(UserManager<IdentityUser> users,string email,bool confirmed)
    {
        var user=new IdentityUser{UserName=email,Email=email,EmailConfirmed=confirmed};
        var result=await users.CreateAsync(user,"StrongPassword123");
        if(!result.Succeeded)throw new InvalidOperationException("F2B_O3_PG_FIXTURE_USER_CREATE_FAILED");
        return user;
    }
    private static Task<Guid> IntentId(DirectoryDb db,string userId)=>
        db.IdentityEmailOutbox.AsNoTracking().Where(x=>x.UserId==userId&&x.Status==IdentityEmailOutboxStatus.Pending)
            .Select(x=>x.Id).FirstAsync();
    private static Task<IdentityEmailOutboxMessage> Message(DirectoryDb db,Guid id)=>
        db.IdentityEmailOutbox.AsNoTracking().SingleAsync(x=>x.Id==id);

    private sealed class MutableClock(DateTimeOffset initial):TimeProvider
    {
        private DateTimeOffset _now=initial;
        public override DateTimeOffset GetUtcNow()=>_now;
        internal void Advance(TimeSpan span)=>_now=_now.Add(span);
    }
    internal sealed record CapturedLog(
        string Message,
        IReadOnlyDictionary<string,string?> Properties,
        string? Exception);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<CapturedLog> _messages=new();
        internal IReadOnlyCollection<CapturedLog> Messages=>_messages.ToArray();

        public ILogger CreateLogger(string categoryName) =>
            categoryName.EndsWith(nameof(IdentityEmailOutboxWorker),StringComparison.Ordinal)
                ? new CapturingLogger(_messages)
                : new NullLogger();

        public void Dispose() { }

        private sealed class CapturingLogger(ConcurrentQueue<CapturedLog> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState:notnull => null;
            public bool IsEnabled(LogLevel logLevel)=>logLevel>=LogLevel.Debug;
            public void Log<TState>(LogLevel logLevel,EventId eventId,TState state,
                Exception? exception,Func<TState,Exception?,string> formatter)
            {
                if(!IsEnabled(logLevel)) return;
                var properties=state is IEnumerable<KeyValuePair<string,object?>> values
                    ? values.Where(value=>value.Key!="{OriginalFormat}")
                        .GroupBy(value=>value.Key,StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group=>group.Key,group=>group.Last().Value?.ToString(),
                            StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
                messages.Enqueue(new CapturedLog(
                    formatter(state,exception),properties,exception?.ToString()));
            }
        }

        private sealed class NullLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState:notnull => null;
            public bool IsEnabled(LogLevel logLevel)=>false;
            public void Log<TState>(LogLevel logLevel,EventId eventId,TState state,
                Exception? exception,Func<TState,Exception?,string> formatter) { }
        }
    }

    private sealed record Delivery(string Email,string Payload);
    private sealed class FakeTransport:IIdentityEmailOutboxTransport
    {
        private readonly ConcurrentQueue<Delivery> _items=new();
        private int _failBefore,_failAfter,_block,_failAlways;
        private TaskCompletionSource<bool> _blocked=Signal(),_release=Signal();
        internal IReadOnlyCollection<Delivery> Deliveries=>_items.ToArray();
        internal void FailBeforeOnce()=>Interlocked.Exchange(ref _failBefore,1);
        internal void FailAfterOnce()=>Interlocked.Exchange(ref _failAfter,1);
        internal bool FailAlways {
            get=>Volatile.Read(ref _failAlways)==1;
            set=>Volatile.Write(ref _failAlways,value?1:0);
        }
        private int _failBlockedBefore;
        internal void BlockNext()
        {
            _blocked=Signal();_release=Signal();
            Interlocked.Exchange(ref _failBlockedBefore,0);
            Interlocked.Exchange(ref _block,1);
        }
        internal void BlockNextThenFailBeforeAccept()
        {
            _blocked=Signal();_release=Signal();
            Interlocked.Exchange(ref _failBlockedBefore,1);
            Interlocked.Exchange(ref _block,1);
        }
        internal Task WaitBlockedAsync()=>_blocked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        internal void Release()=>_release.TrySetResult(true);
        public Task SendConfirmationAsync(IdentityUser user,string email,string link,CancellationToken ct)=>Send(email,link,ct);
        public Task SendPasswordResetAsync(IdentityUser user,string email,string code,CancellationToken ct)=>Send(email,code,ct);
        private async Task Send(string email,string payload,CancellationToken ct)
        {
            if(Interlocked.Exchange(ref _block,0)==1)
            {
                _blocked.TrySetResult(true);
                await _release.Task.WaitAsync(ct);
                if(Interlocked.Exchange(ref _failBlockedBefore,0)==1)
                    throw new IOException("synthetic stale-owner transport failure");
            }
            if(FailAlways || Interlocked.Exchange(ref _failBefore,0)==1)
                throw new IOException("synthetic failure");
            _items.Enqueue(new Delivery(email,payload));
            if(Interlocked.Exchange(ref _failAfter,0)==1)
                throw new IOException("synthetic uncertain acknowledgement");
        }
        private static TaskCompletionSource<bool> Signal()=>new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
