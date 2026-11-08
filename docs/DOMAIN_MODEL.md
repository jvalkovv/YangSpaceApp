# Domain & Data Model — Draft

## Главни агрегати

**Organization**: Id, Name, LegalName(optional), Status, CreatedAt.
**OrganizationMember**: OrganizationId, UserId, Role, InvitedAt, RevokedAt.
**BusinessLocation**: Id, OrganizationId, PublicName, Slug, Description, Address, CityId, NeighborhoodId, GeoPoint, TimeZone, Contacts, Status, VerifiedAt, VerificationSource, LastUpdatedAt.
**OpeningHours**: LocationId, DayOfWeek, OpenTime, CloseTime, Closed; separate `OpeningHourException` for holidays or exceptional schedules.
**City / Neighborhood**: Id, ParentIds, Name, Slug, GeoBoundary(optional), DisplayOrder.
**Category / LocationCategory**: many-to-many, taxonomy versioning, optional parent category.
**BusinessClaim**: LocationId, UserId, EvidenceRef(optional), Status, ReviewerId, ReviewedAt, DecisionNote.
**Offer**: Id, LocationId, Title, Details, Terms, StartsAt, EndsAt, Status, PriceDisclosure(optional), CreatedBy, PublishedAt.
**Campaign**: Id, OrganizationId, Scope, Title, Start/End, Status; can group offers and brochures.
**Brochure**: Id, LocationId or CampaignId, MediaId, Title, ValidFrom, ValidUntil, RightsAttestedAt, Status.
**JobPost**: Id, LocationId, Title, Description, EmploymentType, Compensation(optional), ApplyUrlOrContact, ValidUntil, Status.
**MediaAsset**: Id, ObjectKey, MimeType, Size, Checksum, ScanStatus, OwnerScope, CreatedAt.
**Report / ModerationDecision / AuditEvent**: subjects, actors, action, reason, timestamp, retention category.
**InteractionEvent**: aggregated, consent-aware contact/campaign actions, no unnecessary personal identifiers.

## Rules

1. Organization може да има много BusinessLocations, но всеки BusinessLocation има точно един accountable organization owner.
2. User може да е член на няколко Organization с различни роли; няма глобална boolean стойност `IsServiceProvider` като authorization model.
3. Публикуването е отделно от създаването: `draft → pending_review → published → expired|archived`; `rejected` и `suspended` също са явни състояния.
4. Времевите валидности се нормализират до UTC; UI показва локалната зона (за България Europe/Sofia).
5. Съдържание с изтекъл срок не се показва като активно; worker и заявките трябва да дават еднакъв резултат.
6. Публично се показват само одобрени профили и публикувано съдържание, с изключение на контролиран owner/admin preview.
7. Управлението на обекти се проверява server-side във всяка read/write операция, не само чрез Angular guard.
8. MediaAsset не се доверява на оригиналното име на файла; access policy, download headers и content validation са задължителни.
9. Профилът различава `owner_verified` от `data_recently_checked`; това не са взаимозаменяеми твърдения.
10. Извличането на данни от трети страни трябва да има разрешен източник и да не нарушава договорни/лицензни условия.

## Разширяемост без преждевременен overengineering

- `Subscription`, `Plan`, `Entitlement`, `Invoice` са само бъдещи concept boundaries.
- Никъде в домейн модела не се фиксират 1–5 EUR като бизнес правило.
- Мулти-градският обхват се реализира чрез geo model + индекси, а не чрез отделни DB instances по квартал.
- Резервациите не са част от YangSpace 2.0 MVP; старите записи се запазват единствено като legacy data до решение за миграция.

## Индекси (планирани)

- Unique slug по град/обект при избрана URL политика.
- Spatial/geohash подход за геолокации след DB ADR.
- Index: (LocationId, Status, StartsAt, EndsAt) за активни оферти.
- Index: (NeighborhoodId, Status) за каталог и публикувани обекти.
- Search text index според избраната СУБД.
