using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YangSpace.Api;

internal static class IdentityEmailOutboxWorkerContract
{
    private static void Check(bool ok,string marker)
    {
        if (!ok) throw new InvalidOperationException("OUTBOX_WORKER_FAILED="+marker);
        Console.WriteLine(marker+"=PASS");
    }

    internal static async Task RunAsync()
    {
        var keys=Path.Combine(Path.GetTempPath(),"yangspace-o3-keys-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(keys);
        try
        {
            var cs=new SqliteConnectionStringBuilder {
                DataSource="yangspace-o3-"+Guid.NewGuid().ToString("N"),
                Mode=SqliteOpenMode.Memory, Cache=SqliteCacheMode.Shared,
                ForeignKeys=true, Pooling=false, DefaultTimeout=30
            }.ToString();
            await using var keeper=new SqliteConnection(cs);
            await keeper.OpenAsync();

            var clock=new MutableClock(DateTimeOffset.UtcNow);
            var fake=new FakeTransport();
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                ["PublicAccounts:CanonicalOrigin"]="https://yangspace.gndstore.eu"
            }).Build();
            var services=new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys))
                .SetApplicationName("YangSpace-F2b-O3-Contract");
            services.AddDbContext<DirectoryDb>(o=>o.UseSqlite(cs));
            services.AddIdentityCore<IdentityUser>(o=>{
                o.User.RequireUniqueEmail=true;
                o.Password.RequiredLength=8;
                o.Password.RequireDigit=false;
                o.Password.RequireUppercase=false;
                o.Password.RequireNonAlphanumeric=false;
            }).AddRoles<IdentityRole>().AddEntityFrameworkStores<DirectoryDb>().AddDefaultTokenProviders();
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IIdentityEmailOutboxTransport>(fake);
            services.AddSingleton<IConfiguration>(config);
            services.Configure<IdentityEmailOutboxOptions>(o=>{
                o.PollIntervalSeconds=1; o.LeaseSeconds=30; o.BatchSize=20; o.MaxAttempts=3;
                o.SentRetentionDays=30; o.SuppressedRetentionDays=30; o.DeadLetterRetentionDays=90;
            });
            using var provider=services.BuildServiceProvider(new ServiceProviderOptions {
                ValidateScopes=true, ValidateOnBuild=true
            });
            await using var scope=provider.CreateAsyncScope();
            var db=scope.ServiceProvider.GetRequiredService<DirectoryDb>();
            var users=scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await db.Database.EnsureCreatedAsync();
            var worker=NewWorker(provider,clock,config);
            var queue=new OutboxIdentityEmailSender(db);

            var user=await NewUser(users,"confirm@example.invalid",false);
            await queue.SendConfirmationLinkAsync(user,user.Email!,"discarded-placeholder");
            var id=await IntentId(db,user.Id,IdentityEmailPurpose.ConfirmEmail);
            await worker.ProcessBatchOnceAsync();
            var row=await Message(db,id);
            var delivered=fake.Deliveries.Single(x=>x.Email==user.Email && x.Purpose==IdentityEmailPurpose.ConfirmEmail);
            var uri=new Uri(delivered.Payload);
            var code=QueryHelpers.ParseQuery(uri.Query)["code"].ToString();
            var token=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var confirm=await users.ConfirmEmailAsync(user,token);
            Check(row.Status==IdentityEmailOutboxStatus.Sent && confirm.Succeeded &&
                delivered.Payload.StartsWith("https://yangspace.gndstore.eu/api/public-account/confirm?",StringComparison.Ordinal),
                "F2B_O3_WORKER_CONFIRMATION_DISPATCH");

            await queue.SendPasswordResetCodeAsync(user,user.Email!,"discarded-reset-placeholder");
            var resetId=await IntentId(db,user.Id,IdentityEmailPurpose.ResetPassword);
            await worker.ProcessBatchOnceAsync();
            var resetDelivery=fake.Deliveries.Last(x=>x.Email==user.Email && x.Purpose==IdentityEmailPurpose.ResetPassword);
            var resetToken=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetDelivery.Payload));
            var reset=await users.ResetPasswordAsync(user,resetToken,"ReplacementPassword123");
            Check((await Message(db,resetId)).Status==IdentityEmailOutboxStatus.Sent &&
                reset.Succeeded && resetDelivery.Payload!="discarded-reset-placeholder",
                "F2B_O3_WORKER_RESET_DISPATCH");

            var changed=await NewUser(users,"before@example.invalid",false);
            await queue.SendConfirmationLinkAsync(changed,changed.Email!,"discarded");
            var changedId=await IntentId(db,changed.Id,IdentityEmailPurpose.ConfirmEmail);
            await users.SetEmailAsync(changed,"after@example.invalid");
            await users.SetUserNameAsync(changed,"after@example.invalid");
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,changedId)).Status==IdentityEmailOutboxStatus.Sent &&
                fake.Deliveries.Any(x=>x.Email=="after@example.invalid"),
                "F2B_O3_WORKER_RECHECKS_CURRENT_EMAIL");

            var role=await roles.CreateAsync(new IdentityRole("Reviewer"));
            var staff=await NewUser(users,"staff@example.invalid",false);
            await users.AddToRoleAsync(staff,"Reviewer");
            await queue.SendConfirmationLinkAsync(staff,staff.Email!,"discarded");
            var staffId=await IntentId(db,staff.Id,IdentityEmailPurpose.ConfirmEmail);
            var confirmed=await NewUser(users,"already-confirmed@example.invalid",true);
            await queue.SendConfirmationLinkAsync(confirmed,confirmed.Email!,"discarded");
            var confirmedId=await IntentId(db,confirmed.Id,IdentityEmailPurpose.ConfirmEmail);
            var unconfirmed=await NewUser(users,"unconfirmed-reset@example.invalid",false);
            await queue.SendPasswordResetCodeAsync(unconfirmed,unconfirmed.Email!,"discarded");
            var unconfirmedId=await IntentId(db,unconfirmed.Id,IdentityEmailPurpose.ResetPassword);
            var expired=await NewUser(users,"expired@example.invalid",false);
            await queue.SendConfirmationLinkAsync(expired,expired.Email!,"discarded");
            var expiredId=await IntentId(db,expired.Id,IdentityEmailPurpose.ConfirmEmail);
            await db.IdentityEmailOutbox.Where(x=>x.Id==expiredId).ExecuteUpdateAsync(s=>s
                .SetProperty(x=>x.ExpiresAtUtc,clock.GetUtcNow().UtcDateTime.AddSeconds(-1)));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,expiredId)).Status==IdentityEmailOutboxStatus.Suppressed &&
                (await Message(db,expiredId)).LastFailureCode=="intent_expired",
                "F2B_O3_WORKER_EXPIRED_INTENT_SUPPRESSION");
            Check(role.Succeeded &&
                (await Message(db,staffId)).Status==IdentityEmailOutboxStatus.Suppressed &&
                (await Message(db,confirmedId)).Status==IdentityEmailOutboxStatus.Suppressed &&
                (await Message(db,unconfirmedId)).Status==IdentityEmailOutboxStatus.Suppressed,
                "F2B_O3_WORKER_RECHECKS_CONFIRMATION_AND_STAFF_STATE");

            var retry=await NewUser(users,"retry@example.invalid",false);
            await queue.SendConfirmationLinkAsync(retry,retry.Email!,"discarded");
            var retryId=await IntentId(db,retry.Id,IdentityEmailPurpose.ConfirmEmail);
            fake.FailBeforeAcceptOnce();
            await worker.ProcessBatchOnceAsync();
            var failed=await Message(db,retryId);
            Check(failed.Status==IdentityEmailOutboxStatus.Pending && failed.AttemptCount==1 &&
                failed.LastFailureCode=="dispatch_failed" && failed.AvailableAtUtc>clock.GetUtcNow().UtcDateTime,
                "F2B_O3_WORKER_RETRY_BACKOFF");
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,retryId)).Status==IdentityEmailOutboxStatus.Sent &&
                (await Message(db,retryId)).AttemptCount==2,"F2B_O3_WORKER_RETRY_RECOVERY");

            var uncertain=await NewUser(users,"uncertain@example.invalid",false);
            await queue.SendConfirmationLinkAsync(uncertain,uncertain.Email!,"discarded");
            var uncertainId=await IntentId(db,uncertain.Id,IdentityEmailPurpose.ConfirmEmail);
            fake.FailAfterAcceptOnce();
            await worker.ProcessBatchOnceAsync();
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,uncertainId)).Status==IdentityEmailOutboxStatus.Sent &&
                fake.Deliveries.Count(x=>x.Email==uncertain.Email && x.Purpose==IdentityEmailPurpose.ConfirmEmail)==2,
                "F2B_O3_WORKER_UNCERTAIN_ACK_AT_LEAST_ONCE");

            var concurrent=await NewUser(users,"concurrent@example.invalid",false);
            await queue.SendConfirmationLinkAsync(concurrent,concurrent.Email!,"discarded");
            var concurrentId=await IntentId(db,concurrent.Id,IdentityEmailPurpose.ConfirmEmail);
            fake.BlockNext();
            var first=worker.ProcessBatchOnceAsync();
            await fake.WaitBlockedAsync();
            var secondCount=await NewWorker(provider,clock,config).ProcessBatchOnceAsync();
            fake.Release();
            await first;
            Check(secondCount==0 && (await Message(db,concurrentId)).Status==IdentityEmailOutboxStatus.Sent &&
                fake.Deliveries.Count(x=>x.Email==concurrent.Email)==1,
                "F2B_O3_WORKER_CONCURRENT_CLAIM");

            var orphanLease=await NewUser(users,"orphan-lease@example.invalid",false);
            await queue.SendConfirmationLinkAsync(orphanLease,orphanLease.Email!,"discarded");
            var orphanId=await IntentId(db,orphanLease.Id,IdentityEmailPurpose.ConfirmEmail);
            await db.IdentityEmailOutbox.Where(x=>x.Id==orphanId).ExecuteUpdateAsync(s=>s
                .SetProperty(x=>x.Status,IdentityEmailOutboxStatus.Leased)
                .SetProperty(x=>x.LeaseId,(Guid?)null)
                .SetProperty(x=>x.LeasedUntilUtc,(DateTime?)null)
                .SetProperty(x=>x.AttemptCount,1)
                .SetProperty(x=>x.Revision,x=>x.Revision+1));
            await worker.ProcessBatchOnceAsync();
            Check((await Message(db,orphanId)).Status==IdentityEmailOutboxStatus.Sent &&
                (await Message(db,orphanId)).AttemptCount==2,
                "F2B_O3_WORKER_NULL_LEASE_RECOVERY");

            var restart=await NewUser(users,"restart@example.invalid",false);
            await queue.SendConfirmationLinkAsync(restart,restart.Email!,"discarded");
            var restartId=await IntentId(db,restart.Id,IdentityEmailPurpose.ConfirmEmail);
            fake.BlockNext();
            using(var cts=new CancellationTokenSource())
            {
                var interrupted=worker.ProcessBatchOnceAsync(cts.Token);
                await fake.WaitBlockedAsync();
                cts.Cancel();
                var canceled=false;
                try { await interrupted; } catch(OperationCanceledException) { canceled=true; }
                Check(canceled,"F2B_O3_WORKER_CANCELLATION_OBSERVED");
            }
            Check((await Message(db,restartId)).Status==IdentityEmailOutboxStatus.Leased,
                "F2B_O3_WORKER_CRASH_LEAVES_EXPIRING_LEASE");
            clock.Advance(TimeSpan.FromSeconds(31));
            await NewWorker(provider,clock,config).ProcessBatchOnceAsync();
            Check((await Message(db,restartId)).Status==IdentityEmailOutboxStatus.Sent &&
                (await Message(db,restartId)).AttemptCount==2 &&
                fake.Deliveries.Count(x=>x.Email==restart.Email)==1,
                "F2B_O3_WORKER_LEASE_EXPIRY_RECOVERY");

            var poison=await NewUser(users,"poison@example.invalid",false);
            await queue.SendConfirmationLinkAsync(poison,poison.Email!,"discarded");
            var poisonId=await IntentId(db,poison.Id,IdentityEmailPurpose.ConfirmEmail);
            fake.FailAlways=true;
            await worker.ProcessBatchOnceAsync();
            clock.Advance(TimeSpan.FromSeconds(31));
            await worker.ProcessBatchOnceAsync();
            clock.Advance(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(1)));
            await worker.ProcessBatchOnceAsync();
            fake.FailAlways=false;
            var dead=await Message(db,poisonId);
            Check(dead.Status==IdentityEmailOutboxStatus.DeadLetter && dead.AttemptCount==3,
                "F2B_O3_WORKER_DEADLETTER");
            var replay=await IdentityEmailOutboxAdminEndpoints.ReplayDeadLetterAsync(db,poisonId,clock,CancellationToken.None);
            await worker.ProcessBatchOnceAsync();
            Check(replay==IdentityEmailOutboxReplayResult.Replayed &&
                (await Message(db,poisonId)).Status==IdentityEmailOutboxStatus.Sent,
                "F2B_O3_WORKER_OPERATOR_REPLAY");

            var now=clock.GetUtcNow().UtcDateTime;
            var old=new[]{
                new IdentityEmailOutboxMessage { UserId=user.Id, Purpose=IdentityEmailPurpose.ConfirmEmail,
                    Status=IdentityEmailOutboxStatus.Sent, QueuedAtUtc=now.AddDays(-40),
                    AvailableAtUtc=now.AddDays(-40), ExpiresAtUtc=now.AddDays(-39), SentAtUtc=now.AddDays(-31) },
                new IdentityEmailOutboxMessage { UserId=user.Id, Purpose=IdentityEmailPurpose.ResetPassword,
                    Status=IdentityEmailOutboxStatus.Suppressed, QueuedAtUtc=now.AddDays(-40),
                    AvailableAtUtc=now.AddDays(-40), ExpiresAtUtc=now.AddDays(-31) },
                new IdentityEmailOutboxMessage { UserId=user.Id, Purpose=IdentityEmailPurpose.ConfirmEmail,
                    Status=IdentityEmailOutboxStatus.DeadLetter, QueuedAtUtc=now.AddDays(-100),
                    AvailableAtUtc=now.AddDays(-100), ExpiresAtUtc=now.AddDays(-99), DeadLetterAtUtc=now.AddDays(-91) }
            };
            db.IdentityEmailOutbox.AddRange(old);
            await db.SaveChangesAsync();
            var oldIds=old.Select(x=>x.Id).ToArray();
            await worker.ProcessBatchOnceAsync();
            Check(!await db.IdentityEmailOutbox.AnyAsync(x=>oldIds.Contains(x.Id)),
                "F2B_O3_WORKER_RETENTION_PURGE");

            var forbidden=typeof(IdentityEmailOutboxMessage).GetProperties().Select(x=>x.Name)
                .Any(x=>x.Contains("Token",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Email",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Recipient",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Body",StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("Secret",StringComparison.OrdinalIgnoreCase));
            Check(!forbidden && (await db.IdentityEmailOutbox.AsNoTracking().ToArrayAsync()).All(x=>x.UserId!=user.Email),
                "F2B_O3_NO_TOKEN_OR_RECIPIENT_AT_REST");

            Console.WriteLine("F2B_O3_WORKER_FAKE_TRANSPORT_ONLY=PASS");
            Console.WriteLine("F2B_O3_WORKER_SQLITE_SYNTHETIC_ONLY=PASS");
            Console.WriteLine("F2B_O3_POSTGRES_WORKER=NOT_RUN");
            Console.WriteLine("F2B_O3_REAL_SMTP=NOT_CONNECTED");
            Console.WriteLine("F2B_O3_IDENTITY_HTTP=NOT_TESTED");
        }
        finally { if(Directory.Exists(keys)) Directory.Delete(keys,true); }
    }

    private static IdentityEmailOutboxWorker NewWorker(IServiceProvider provider,TimeProvider clock,IConfiguration config) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<IdentityEmailOutboxWorker>>(),
            provider.GetRequiredService<IOptions<IdentityEmailOutboxOptions>>(),clock,config);

    private static async Task<IdentityUser> NewUser(UserManager<IdentityUser> users,string email,bool confirmed)
    {
        var user=new IdentityUser { UserName=email, Email=email, EmailConfirmed=confirmed };
        var result=await users.CreateAsync(user,"StrongPassword123");
        if(!result.Succeeded) throw new InvalidOperationException("OUTBOX_WORKER_TEST_USER_CREATE_FAILED");
        return user;
    }

    private static Task<Guid> IntentId(DirectoryDb db,string userId,IdentityEmailPurpose purpose) =>
        db.IdentityEmailOutbox.AsNoTracking().Where(x=>x.UserId==userId && x.Purpose==purpose &&
            x.Status==IdentityEmailOutboxStatus.Pending).Select(x=>x.Id).FirstAsync();

    private static Task<IdentityEmailOutboxMessage> Message(DirectoryDb db,Guid id) =>
        db.IdentityEmailOutbox.AsNoTracking().SingleAsync(x=>x.Id==id);

    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now=initial;
        public override DateTimeOffset GetUtcNow()=>_now;
        internal void Advance(TimeSpan span)=>_now=_now.Add(span);
    }

    private sealed class FakeTransport : IIdentityEmailOutboxTransport
    {
        private readonly ConcurrentQueue<Delivery> _deliveries=new();
        private int _failBefore,_failAfter,_blockNext,_failAlways;
        private TaskCompletionSource<bool> _blocked=Signal(),_release=Signal();
        internal IReadOnlyCollection<Delivery> Deliveries=>_deliveries.ToArray();
        internal bool FailAlways { get=>Volatile.Read(ref _failAlways)==1; set=>Volatile.Write(ref _failAlways,value?1:0); }
        internal void FailBeforeAcceptOnce()=>Interlocked.Exchange(ref _failBefore,1);
        internal void FailAfterAcceptOnce()=>Interlocked.Exchange(ref _failAfter,1);
        internal void BlockNext(){_blocked=Signal();_release=Signal();Interlocked.Exchange(ref _blockNext,1);}
        internal Task WaitBlockedAsync()=>_blocked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        internal void Release()=>_release.TrySetResult(true);
        public Task SendConfirmationAsync(IdentityUser user,string email,string link,CancellationToken ct)=>
            SendAsync(IdentityEmailPurpose.ConfirmEmail,email,link,ct);
        public Task SendPasswordResetAsync(IdentityUser user,string email,string code,CancellationToken ct)=>
            SendAsync(IdentityEmailPurpose.ResetPassword,email,code,ct);
        private async Task SendAsync(IdentityEmailPurpose purpose,string email,string payload,CancellationToken ct)
        {
            if(Interlocked.Exchange(ref _blockNext,0)==1){_blocked.TrySetResult(true);await _release.Task.WaitAsync(ct);}
            if(FailAlways || Interlocked.Exchange(ref _failBefore,0)==1) throw new IOException("synthetic failure");
            _deliveries.Enqueue(new Delivery(purpose,email,payload));
            if(Interlocked.Exchange(ref _failAfter,0)==1) throw new IOException("synthetic uncertain ACK");
        }
        private static TaskCompletionSource<bool> Signal()=>new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed record Delivery(IdentityEmailPurpose Purpose,string Email,string Payload);
}
