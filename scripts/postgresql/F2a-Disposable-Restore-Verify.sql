-- Validate a second database restored from a custom-format synthetic pg_dump.
DO $restore$
DECLARE n int;
BEGIN
  SELECT count(*) INTO n FROM information_schema.tables
    WHERE table_schema='public' AND table_type='BASE TABLE';
  IF n<>16 THEN RAISE EXCEPTION 'Restored tables wrong: %',n; END IF;
  SELECT count(*) INTO n FROM pg_constraint
    WHERE connamespace='public'::regnamespace AND contype='f';
  IF n<>17 THEN RAISE EXCEPTION 'Restored FKs wrong: %',n; END IF;
  SELECT count(*) INTO n FROM "__EFMigrationsHistory"
    WHERE "MigrationId"='20261111203833_InitialPostgreSqlSchema';
  IF n<>1 THEN RAISE EXCEPTION 'Restored EF history missing'; END IF;
  SELECT count(*) INTO n FROM "AspNetUsers";
  IF n<>2 THEN RAISE EXCEPTION 'Restored Identity users wrong'; END IF;
  SELECT count(*) INTO n FROM "Organizations";
  IF n<>2 THEN RAISE EXCEPTION 'Restored organizations wrong'; END IF;
  SELECT count(*) INTO n FROM "Locations"
    WHERE "Slug"='qa-location' AND "OrganizationId" IS NULL AND "Revision"=1;
  IF n<>1 THEN RAISE EXCEPTION 'Restored location/revision wrong'; END IF;
  SELECT count(*) INTO n FROM "BusinessClaims" WHERE "Status"='Pending';
  IF n<>2 THEN RAISE EXCEPTION 'Restored Pending claims wrong'; END IF;
  SELECT count(*) INTO n FROM "BusinessClaims" WHERE "Status"='Approved';
  IF n<>1 THEN RAISE EXCEPTION 'Restored Approved claim wrong'; END IF;
  SELECT count(*) INTO n FROM "BusinessClaims" WHERE "Status"='Withdrawn';
  IF n<>1 THEN RAISE EXCEPTION 'Restored Withdrawn claim wrong'; END IF;
  SELECT count(*) INTO n FROM "ContentAuditEvents";
  IF n<>1 THEN RAISE EXCEPTION 'Restored audit events wrong'; END IF;
  IF NOT EXISTS(SELECT 1 FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid
    WHERE c.relname='UX_BusinessClaims_ApprovedLocation' AND i.indisunique AND i.indisvalid)
  THEN RAISE EXCEPTION 'Restored Approved unique index missing'; END IF;
  IF NOT EXISTS(SELECT 1 FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid
    WHERE c.relname='UX_BusinessClaims_PendingLocationOrganization' AND i.indisunique AND i.indisvalid)
  THEN RAISE EXCEPTION 'Restored Pending unique index missing'; END IF;
END
$restore$;
SELECT 'F2A_PG_RESTORE=PASS';
