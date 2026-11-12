-- Synthetic test data only. Requires a fresh network-isolated PostgreSQL 17 database.
BEGIN;
DO $verify$
DECLARE
  n int;
  constraint_name text;
BEGIN
  SELECT count(*) INTO n FROM information_schema.tables
    WHERE table_schema='public' AND table_type='BASE TABLE';
  IF n<>16 THEN RAISE EXCEPTION 'Expected 15 mapped tables plus EF history; found %',n; END IF;
  SELECT count(*) INTO n FROM pg_constraint
    WHERE connamespace='public'::regnamespace AND contype='f';
  IF n<>17 THEN RAISE EXCEPTION 'Expected 17 foreign keys; found %',n; END IF;
  IF NOT EXISTS(SELECT 1 FROM information_schema.columns
    WHERE table_schema='public' AND table_name='Locations' AND column_name='Revision'
    AND data_type='bigint' AND is_nullable='NO')
  THEN RAISE EXCEPTION 'Location revision column missing'; END IF;
  IF NOT EXISTS(SELECT 1 FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid
    WHERE c.relname='UX_BusinessClaims_ApprovedLocation' AND i.indisunique
    AND i.indisvalid AND i.indpred IS NOT NULL
    AND pg_get_expr(i.indpred,i.indrelid) LIKE '%Approved%')
  THEN RAISE EXCEPTION 'Missing approved partial unique index'; END IF;
  IF NOT EXISTS(SELECT 1 FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid
    WHERE c.relname='UX_BusinessClaims_PendingLocationOrganization' AND i.indisunique
    AND i.indisvalid AND i.indpred IS NOT NULL
    AND pg_get_expr(i.indpred,i.indrelid) LIKE '%Pending%')
  THEN RAISE EXCEPTION 'Missing pending partial unique index'; END IF;

  INSERT INTO "AspNetUsers"
    ("Id","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount")
    VALUES ('qa-a',false,false,false,false,0),('qa-b',false,false,false,false,0);
  INSERT INTO "Organizations" ("Id","Name","Status","CreatedAtUtc","Revision")
    VALUES ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','QA A','Active',now(),0),
           ('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb','QA B','Active',now(),0);
  INSERT INTO "Locations"
    ("Id","Slug","Name","City","Neighborhood","Category","Description",
     "Address","IsPublished","IsVerified","OrganizationId","Revision","UpdatedAtUtc")
    VALUES ('11111111-1111-4111-8111-111111111111','qa-location','QA','QA','QA',
     'QA','','QA',false,false,null,0,now());

  INSERT INTO "BusinessClaims"
    ("Id","LocationId","OrganizationId","ApplicantUserId","Status","SubmittedAtUtc","Revision")
    VALUES ('22222222-2222-4222-8222-222222222222',
            '11111111-1111-4111-8111-111111111111',
            'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','qa-a','Pending',now(),0),
           ('33333333-3333-4333-8333-333333333333',
            '11111111-1111-4111-8111-111111111111',
            'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb','qa-b','Pending',now(),0);
  SELECT count(*) INTO n FROM "BusinessClaims" WHERE "Status"='Pending';
  IF n<>2 THEN RAISE EXCEPTION 'Competing pending claims blocked'; END IF;

  BEGIN
    INSERT INTO "BusinessClaims"
      ("Id","LocationId","OrganizationId","ApplicantUserId","Status","SubmittedAtUtc","Revision")
      VALUES ('44444444-4444-4444-8444-444444444444',
              '11111111-1111-4111-8111-111111111111',
              'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','qa-b','Pending',now(),0);
    RAISE EXCEPTION 'Duplicate pending claim was accepted';
  EXCEPTION WHEN unique_violation THEN
    GET STACKED DIAGNOSTICS constraint_name=CONSTRAINT_NAME;
    IF constraint_name<>'UX_BusinessClaims_PendingLocationOrganization'
    THEN RAISE EXCEPTION 'Unexpected pending constraint %',constraint_name; END IF;
  END;

  INSERT INTO "BusinessClaims"
    ("Id","LocationId","OrganizationId","ApplicantUserId","Status","SubmittedAtUtc","Revision")
    VALUES ('55555555-5555-4555-8555-555555555555',
            '11111111-1111-4111-8111-111111111111',
            'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','qa-a','Approved',now(),0);
  BEGIN
    INSERT INTO "BusinessClaims"
      ("Id","LocationId","OrganizationId","ApplicantUserId","Status","SubmittedAtUtc","Revision")
      VALUES ('66666666-6666-4666-8666-666666666666',
              '11111111-1111-4111-8111-111111111111',
              'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb','qa-b','Approved',now(),0);
    RAISE EXCEPTION 'Second approved claim was accepted';
  EXCEPTION WHEN unique_violation THEN
    GET STACKED DIAGNOSTICS constraint_name=CONSTRAINT_NAME;
    IF constraint_name<>'UX_BusinessClaims_ApprovedLocation'
    THEN RAISE EXCEPTION 'Unexpected approved constraint %',constraint_name; END IF;
  END;

  BEGIN
    INSERT INTO "BusinessClaims"
      ("Id","LocationId","OrganizationId","ApplicantUserId","Status","SubmittedAtUtc","Revision")
      VALUES ('77777777-7777-4777-8777-777777777777',
              '11111111-1111-4111-8111-111111111111',
              'cccccccc-cccc-4ccc-8ccc-cccccccccccc','qa-a','Pending',now(),0);
    RAISE EXCEPTION 'Orphan claim FK accepted';
  EXCEPTION WHEN foreign_key_violation THEN
    GET STACKED DIAGNOSTICS constraint_name=CONSTRAINT_NAME;
    IF constraint_name<>'FK_BusinessClaims_Organizations_OrganizationId'
    THEN RAISE EXCEPTION 'Unexpected FK %',constraint_name; END IF;
  END;

  UPDATE "BusinessClaims" SET "Status"='Withdrawn',"Revision"=1
    WHERE "Id"='22222222-2222-4222-8222-222222222222';
  INSERT INTO "BusinessClaims"
    ("Id","LocationId","OrganizationId","ApplicantUserId","Status","SubmittedAtUtc","Revision")
    VALUES ('88888888-8888-4888-8888-888888888888',
            '11111111-1111-4111-8111-111111111111',
            'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','qa-a','Pending',now(),0);
  SELECT count(*) INTO n FROM "BusinessClaims"
    WHERE "OrganizationId"='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' AND "Status"='Pending';
  IF n<>1 THEN RAISE EXCEPTION 'Reapplication after withdrawn failed'; END IF;

  UPDATE "Locations" SET "Revision"="Revision"+1
    WHERE "Id"='11111111-1111-4111-8111-111111111111' AND "Revision"=0;
  GET DIAGNOSTICS n=ROW_COUNT;
  IF n<>1 THEN RAISE EXCEPTION 'CAS initial update failed'; END IF;
  UPDATE "Locations" SET "Revision"="Revision"+1
    WHERE "Id"='11111111-1111-4111-8111-111111111111' AND "Revision"=0;
  GET DIAGNOSTICS n=ROW_COUNT;
  IF n<>0 THEN RAISE EXCEPTION 'CAS stale update accepted'; END IF;

  INSERT INTO "ContentAuditEvents"
    ("Id","OrganizationId","LocationId","EntityType","EntityId","Action",
     "ActorUserId","ActorRole","OccurredAtUtc","RevisionBefore",
     "RevisionAfter","CorrelationId","ChangedFieldsJson")
    VALUES ('99999999-9999-4999-8999-999999999999',
            'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            '11111111-1111-4111-8111-111111111111',
            'Location','11111111-1111-4111-8111-111111111111',
            'Update','qa-a','Owner',now(),0,1,'qa','{}');
  BEGIN
    UPDATE "Locations" SET "Revision"="Revision"+1
      WHERE "Id"='11111111-1111-4111-8111-111111111111';
    INSERT INTO "ContentAuditEvents"
      ("Id","OrganizationId","LocationId","EntityType","EntityId","Action",
       "ActorUserId","ActorRole","OccurredAtUtc","RevisionBefore",
       "RevisionAfter","CorrelationId","ChangedFieldsJson")
      VALUES ('aaaaaaaa-0000-4000-8000-000000000000',
              'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
              '11111111-1111-4111-8111-111111111111',
              'Location','11111111-1111-4111-8111-111111111111',
              'Update','qa-a','Owner',now(),0,1,'duplicate','{}');
    RAISE EXCEPTION 'Duplicate event accepted';
  EXCEPTION WHEN unique_violation THEN
    GET STACKED DIAGNOSTICS constraint_name=CONSTRAINT_NAME;
    IF constraint_name<>'IX_ContentAuditEvents_EntityType_EntityId_RevisionAfter'
    THEN RAISE EXCEPTION 'Unexpected audit constraint %',constraint_name; END IF;
  END;
  SELECT "Revision" INTO n FROM "Locations"
    WHERE "Id"='11111111-1111-4111-8111-111111111111';
  IF n<>1 THEN RAISE EXCEPTION 'Failed audit did not rollback location update'; END IF;
END
$verify$;
COMMIT;
SELECT 'F2A_PG_CONSTRAINTS=PASS';
SELECT 'F2A_PG_SEQUENTIAL_CAS=PASS';
SELECT 'F2A_PG_DB_AUDIT_ROLLBACK=PASS';
