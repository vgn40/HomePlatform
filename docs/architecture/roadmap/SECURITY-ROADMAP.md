# HomePlatform Security Roadmap

Status: **Authoritative security plan**  
Last reviewed: **2026-09-14**
Scope: authentication, authorization, API/data protection, secure operations,
and blocking verification gates for the six-month plan  
Rule: authentication proves Account identity; authorization decides what that
Account's current Membership may do to a specific Household resource.

Target terminology follows accepted
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md).
The current durable schema implements the first Account/Membership identity
slice; linking, last-Owner concurrency, and resource authorization remain gated
follow-up work.

## Current security posture

| Check | Status | Current evidence |
|---|---|---|
| ProblemDetails/central exception middleware configured | PASS for Phase 1 | Api registers `AddProblemDetails` and `UseExceptionHandler`; expected invalid Household input maps to 400, and an unexpected repository exception produces a generic Problem Details 500 without internal/provider disclosure. Stable product error codes remain part of the later production contract. |
| OpenAPI restricted by environment | PASS for Phase 1 | OpenAPI is mapped in Development and Testing; generated Testing OpenAPI proves the Testing-only Household route's 201/400/401 contract, while Production does not expose the route. |
| PostgreSQL readiness without credential disclosure | PASS | `/ready` reports ready/unavailable only. |
| Known NuGet vulnerability scan | PASS (2026-09-02) | connected NuGet audit reported no known vulnerable direct or transitive packages in any solution project; the workflow reruns the time-sensitive check. |
| Authentication/account lifecycle | PARTIAL; release gate OPEN | roleless Identity persistence and implemented/verified registration exist; bearer sign-in and anonymous Refresh are implemented; refresh validates expiry/security stamp and issues new access/refresh tokens; confirmation/recovery/logout/revocation and broader release gates remain incomplete. |
| Authorization/resource checks | FAIL | not implemented. |
| Trusted creator identity | PASS for Testing slice | command carries Name only; `ICurrentAccount` derives the actor from authenticated claims; anonymous/malformed and caller-supplied AccountId tests prove zero impersonation. |
| Rate limiting/lockout policy | INCOMPLETE | sign-in uses Identity lockout enforcement with lockoutOnFailure enabled; endpoint rate limiting and the broader release policy remain incomplete. |
| HTTPS/reverse-proxy production policy | FAIL | not implemented; local launch is HTTP. |
| CORS policy | NOT VERIFIED / not required yet | no frontend/API cross-origin contract exists. Absence is safer than `AllowAnyOrigin`; configure exact origins only when Expo web exists. |
| Secrets management | PARTIAL | disposable development credentials are tracked in example/development files; no production secret store/configuration exists. |
| Logging redaction policy | FAIL | default logging exists; no explicit sensitive-data rules/tests. |
| Security integration tests | PARTIAL | fake-auth CreateHousehold tests cover 401, actor trust, zero rows, Production 404, and unexpected-error non-disclosure; registration tests now exercise UserManager and PostgreSQL; bearer sign-in tests cover token issuance, authenticated follow-up requests, wrong-password failed counts, and locked-account rejection; real-bearer Refresh tests cover renewal, protected AccountId persistence, required/invalid/tampered/expired/stamp-invalidated tokens, public error non-disclosure, one JSON response, and anonymous fallback-policy behavior; IDOR, client/revocation lifecycle, and broader log redaction proof remain incomplete. |
| Database least privilege/backups/restore | NOT VERIFIED | no deployed database or production roles exist. |

The live Program maps anonymous `POST /api/accounts/register`,
`POST /api/accounts/sign-in`, and `POST /api/accounts/refresh` in Production;
only the Household route is Testing-only. This is source-level exposure, not
evidence of a public deployment. Registration input validation, recognized
UserNameIndex duplicate-race mapping, typed errors, and OpenAPI are implemented
and verified in `e2fca98` (historical 72/72 solution tests). Bearer sign-in is
implemented in `4180096`. Refresh is implemented with permanent PostgreSQL
coverage; [historical Refresh verification](NEXT-STEPS.md#refresh-verification--2026-09-13)
is 105/105 tests. The [Account-reference review](NEXT-STEPS.md#account-reference-integrity-verification--2026-09-14)
adds FK and upgrade evidence with the current full-suite result. Release gates,
confirmation/recovery/logout/revocation and the independent email lifecycle
policy remain open. Keep the
Household route absent from Production until current-Account validation,
resource authorization, and the remaining release gates are ready;
Account-reference integrity is verified locally.

### Verified Refresh behavior and remaining session evidence

`POST /api/accounts/refresh` requires no access token, including under a test
fallback policy requiring authentication. A missing/null/empty/whitespace token
returns 400 RefreshTokenRequired. Malformed, tampered, expired, and
security-stamp-invalidated tokens return the same 401 InvalidRefreshToken
ProblemDetails without token, Identity, decryption, or exception disclosure in
the tested responses. A shared test clock proves success immediately before
expiry and rejection at/after expiry on ASP.NET Core 10.0.11. Success writes one
framework AccessTokenResponse with new access/refresh tokens; the new access
token authorizes a Household write with the registered AccountId. OpenAPI
exposes only 200 AccessTokenResponse and 400/401 ProblemDetails.

This does not close client storage/replacement, prior-refresh-token reuse,
wrong-purpose-token coverage, password-change/reset flows, old-access-token
validity windows, logout/per-device revocation decisions, rate limiting,
sensitive-log redaction, or shared Data Protection key continuity gates. New
tokens alone do not prove one-time consumption or server-side replay detection.
The authenticated fallback policy is a test override, not production policy.

## Adopted deletion and ownership requirements

[DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) is canonical. Account-reference
integrity is IMPLEMENTED / VERIFIED locally: nullable FK, ClientNoAction /
NO ACTION and PostgreSQL integrity/upgrade proof. The protected-request
current-Account check, DeleteAccount, LeaveHousehold, TransferOwnership and
CloseHousehold are NOT YET IMPLEMENTED. Follow the
[current next-step order](NEXT-STEPS.md): Household ownership lifecycle starting
with TransferOwnership test-first, current-Account validity, then DeleteAccount.

**ADOPTED:** after DeleteAccount successfully commits, new protected product
requests from that Account must be denied even with an unexpired access token.
Current opaque access validation does not reload the Account; the existing
Refresh security-stamp check does not close that gap. Add focused current
Account existence/validity checking and current Household membership/role
authorization; no custom session/OAuth framework is required.

**ADOPTED:** a continuing Household has an Account-linked Owner. Last Owner
requires explicit TransferOwnership to a concrete eligible Account-linked
person or explicit CloseHousehold. Membership never causes automatic Owner
promotion. Null AccountId is not anonymization, and Owner does not own other
people's personal data. The implemented guarding nullable AccountId FK rejects
direct Identity deletion with unresolved links; no Account-to-HouseholdMember CASCADE
DELETE or automatic SET NULL.

**ADOPTED:** after ownership resolution, DeleteAccount explicitly deletes all
HouseholdMember memberships linked to the AccountId across all Households,
then Identity/ApplicationUser and Account-owned data. Never automatically
convert memberships to loginless; AccountId remains nullable for separate
loginless-member flows. The guarding FK rejects unresolved direct deletion;
it does not perform membership lifecycle on the application's behalf.

Destination acceptance, reauthentication UX, exact access lifetime,
concrete future feature lifecycles/attribution/history, child policy, retention/backups,
Article 6 bases, providers/regions, future CloseHousehold record handling and
precise lifecycle concurrency/transaction APIs remain OPEN. No grace period is
adopted.

## Trust model

Untrusted input includes every path, query, header, body field, mobile state, cached permission, and Guid supplied by a client. Random UUIDs are not authorization.

Trusted inputs are created or validated on the server:

- authenticated subject/AccountId from ASP.NET Core authentication, followed
  by current Account existence/validity checking for protected product requests;
- current Membership/role loaded from PostgreSQL for the requested Household;
- server timestamps and generated invitation/reset/session tokens.
- configuration from approved secret/configuration providers.

Canonical request flow:

```text
client request
   |
   v
authentication middleware -> trusted AccountId
   |
   v
current Account existence/validity check
   |
   v
Api DTO (client-editable fields only)
   |
   v
Application handler / authorization policy
   |
   v
resolve current Membership and load the resource from PostgreSQL
   |
   v
Domain invariant + permission decision
   |
   v
single transaction / safe response
```

In Phase 1, `CreateHouseholdCommand` contains Name only and injects a minimal
Application current-account port into the handler. A scoped API adapter uses
`IHttpContextAccessor`, parses exactly the configured subject/NameIdentifier
claim as Guid, and fails closed on missing/malformed identity through a distinct
unauthenticated outcome mapped to 401 with zero mutation. Map the endpoint only
in the `Testing` environment, whose integration host installs a real default
authenticate/challenge scheme; prove a Production host returns 404 because the
route is absent. Phase 2 activates the same adapter under Identity. HTTP
requests and commands never select their actor, and health endpoints must be
explicitly anonymous when a fallback policy is introduced. They currently have
no AllowAnonymous metadata and there is no production fallback policy.

## Authentication architecture decision

### Recommended six-month default

Use ASP.NET Core Identity in Infrastructure with PostgreSQL EF stores:

- `ApplicationUser : IdentityUser<Guid>` belongs in Infrastructure/Identity.
- the implemented `HomePlatformDbContext` is roleless `IdentityUserContext<ApplicationUser, Guid>` unless a real global-role need emerges; call `base.OnModelCreating(builder)` before applying product configurations so one database and migration chain remain.
- use Identity's `UserManager`, `SignInManager`, password hasher, security stamps, confirmation/reset tokens, lockout, and API endpoints.
- Domain does not reference Identity and never owns password/security/token fields.
- Domain User has been removed. Keep ApplicationUser/Identity as the credential
  authority; create a Profile only when concrete behavior and ownership justify it.
  Do not invent an Account aggregate or equate Account and Membership identity.

For the first-party React Native/Expo client, the default is ASP.NET Core Identity's built-in opaque bearer access/refresh-token mode. Select exactly one public authentication mode: do not expose an unchanged `MapIdentityApi` cookie switch, reject `useCookies=true` (or map a deliberately bearer-only Identity-backed boundary), and remove the Phase 1 fake scheme from the Phase 2 app/test host. If the ADR selects cookies instead, the Browser branch becomes Phase 2 blocking work. Microsoft documents that these tokens are proprietary, not JWTs, and intended for simple first-party clients that cannot use cookies—not as a general OAuth/OIDC token server. [Identity API guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)

Do not expose built-in login failure detail unchanged if the product promises non-enumeration. Use a thin Identity-backed boundary/filter that maps unknown user, wrong password, unconfirmed/not-allowed, and locked-out failures to the same public 401 ProblemDetails shape; preserve the internal reason only in protected metrics/logs.

Do not:

- write password hashing or encryption.
- mint a home-grown JWT/refresh-token protocol.
- embed a native client secret.
- parse Identity's opaque token in the client.
- store tokens in URLs, logs, analytics, crash reports, Redux persistence, or AsyncStorage.

### Native token handling

- access token in memory where practical.
- refresh token in `expo-secure-store`; Expo documents Android Keystore-backed encrypted storage and iOS Keychain storage. [Expo SecureStore](https://docs.expo.dev/versions/v54.0.0/sdk/securestore/)
- send access token only in `Authorization: Bearer ...` over HTTPS.
- configure a short access lifetime and a longer bounded refresh lifetime, record exact values, and integration-test expiry/refresh/security-stamp behavior. Do not copy arbitrary internet defaults without testing the selected .NET patch and product risk.
- serialize refresh so concurrent 401s do not create a refresh race.
- replace the client-held refresh token with the token returned by the framework endpoint, but do not claim one-time rotation, server-side consumed-token replay detection, or per-device revocation. Test and document whether the prior protected token remains reusable until expiry or a security-stamp change.
- local logout deletes device tokens. Password reset/change and “sign out everywhere” update the Identity security stamp.
- document that current access tickets can remain valid until expiration; the
  exact lifetime remains OPEN. The adopted DeleteAccount rule is stricter:
  after successful commit, a current-Account check must deny every new protected
  product request from that Account regardless of ticket expiry. Test this
  separately from logout/password-change validity windows.

Before public beta, make an explicit acceptance decision: if the product requires immediate per-device server revocation, device-session inventory, refresh-token reuse detection, social federation, third-party clients, standard OAuth/OIDC/JWT interoperability, or SSO, Identity's simple bearer mode may not suffice. Select an established OAuth/OIDC server/provider and Authorization Code + PKCE rather than extending a custom token server.

### Browser client branch

If Expo web/browser becomes a first-class client, prefer same-site `HttpOnly`, `Secure`, appropriately `SameSite` cookies and antiforgery protection. Configure exact CORS origins and credential behavior. Never combine wildcard origins and credentials. [ASP.NET Core CORS guidance](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0)

### Data Protection keys

Hosting/provider/region choices are **OPEN**. Durable protected Data Protection
keys and continuity tests remain required; the Azure-specific design below is
**PROPOSED**, conditional on later provider selection, not deployment evidence.

If Azure is selected, persist ASP.NET Core Data Protection keys in an environment-scoped Azure Blob Storage key-ring repository under a stable application name, protect the key material with a versionless environment-scoped Key Vault key identifier, retain old wrapping-key versions for as long as old protected payloads may need decryption, and use least-privilege managed identity for both. Key Vault alone is not the shared key-ring repository. Explicitly selecting external persistence disables default at-rest protection, so both persistence and protection are mandatory. Test authentication plus confirmation/reset-token continuity across replica restart, revision replacement, and wrapping-key rotation. [Data Protection key storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0), [Key Vault rotation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)

## Required security classification

The category is the latest acceptable gate. Work can start earlier. Numeric
phases refer to the masterplan; letters A–G refer to the adopted deletion
dependencies. NEXT-STEPS owns the current implementation order.

| Topic | Classification | Phase | Blocking acceptance condition |
|---|---|---:|---|
| ASP.NET Core Identity | BEFORE MULTI-USER FEATURES | 2 | Identity EF store/migrations exist; Domain remains Identity-free. |
| Password hashing | BEFORE MULTI-USER FEATURES | 2 | passwords pass only through Identity/UserManager; no reversible/custom storage. |
| Account registration / email lifecycle | BEFORE MULTI-USER FEATURES | 2 | Current registration is verified through UserName=email and unique UserNameIndex, including controlled concurrent duplicates. The planned independent email policy (`RequireUniqueEmail` and a named unique filtered NormalizedEmail index) remains unimplemented; decide migration/existing-data and future email/username-change semantics before that lifecycle is exposed. |
| Login | BEFORE MULTI-USER FEATURES | 2 | identical public 401 shape for unknown/wrong/unconfirmed/locked failures, lockout enabled, one auth mode, and framework-issued credentials. |
| Email verification | BEFORE MULTI-USER FEATURES | 2 | expiring confirmation flow, resend throttle, and confirmed identity required before invitations/collaboration. |
| Password reset | BEFORE PUBLIC BETA | 2/6 | generic forgot-password response, expiring one-time Identity token, reset invalidates existing sessions within documented semantics. |
| Rate limiting | BEFORE PUBLIC BETA | 2/6 | partitioned policies for login/register/resend/forgot/reset/refresh/invite; 429 and retry behavior tested. |
| Account lockout | BEFORE MULTI-USER FEATURES | 2 | explicit Identity options plus integration tests; pair with IP/device throttles to avoid brute force and trivial lockout abuse. |
| Authorization | BEFORE MULTI-USER FEATURES | 2 | product routes authenticated by default; only health and required auth endpoints explicitly anonymous. |
| Resource-level authorization | BEFORE MULTI-USER FEATURES | 3 | every household/child resource lookup validates current membership and operation. |
| Account-to-Membership linking | BEFORE DURABLE MULTI-USER SCHEMA | 1/3 | ADR 0006 disposition, verified link authority, scoped uniqueness, and concurrency behavior are explicit. |
| Logout/revocation | BEFORE PUBLIC BETA | 2/6 | device logout, sign-out-everywhere/security stamp, password-reset invalidation, token expiry window documented/tested; per-device requirement decided. |
| Secure cookies/tokens | BEFORE MULTI-USER FEATURES | 2 | selected mode, storage, lifetimes, refresh, CSRF branch, key persistence, and no URL/log leakage proven. |
| HTTPS | BEFORE MULTI-USER FEATURES | 2/6 | every non-loopback environment handles credentials/tokens only over TLS; proxy/forwarded headers correct. |
| CORS | BEFORE PUBLIC BETA | 5/6 | native needs none; if web exists, exact environment origins/methods/headers and credentials policy tested. |
| Secrets management | NOW | 1/6 | Phase 1 now: only disposable examples in source, local user-secrets/environment, startup validation. Before public beta: managed deployment store/identity and missing-secret fail-closed proof. |
| Account-reference integrity and deletion access | BEFORE DELETEACCOUNT / REAL-USER RELEASE | A–E in deletion design | guarding nullable AccountId FK, explicit last-Owner resolution and current-Account checks implemented; atomic all-membership deletion and post-commit denial proven. |
| Export/deletion/shared-record fate | BEFORE REAL-USER RELEASE | F/G in deletion design | authorized export, approved retention and shared-data outcomes implemented/tested; unresolved legal/provider/backup decisions completed. |
| Production configuration | BEFORE PUBLIC BETA | 6 | restricted hosts, correct proxy/TLS, safe non-development logging/errors, no development OpenAPI/sample credentials. |
| Database least privilege | BEFORE PUBLIC BETA | 6 | runtime role cannot change schema; separate migration identity; TLS, backup, and restore proven. |
| Secure error handling | NOW | 1 onward | stable ProblemDetails; no stack, SQL/provider text, connection strings, tokens, or unauthorized existence leakage. |
| Logging hygiene | NOW | 1 onward | redact authorization/password/access/refresh/reset/confirmation/invitation values and household free text; automated review test. |
| Dependency vulnerability scanning | NOW | 1 onward | connected NuGet audit in CI; review before adding packages; container scanning before beta. |
| Security-focused integration tests | NOW | 1 onward | pattern starts in first slice; auth and full IDOR matrices become merge/release blockers as features arrive. |

## Authorization model

### Authentication is not authorization

`RequireAuthorization()` ensures a principal exists. It does not prove the caller belongs to the HouseholdId in a route. ASP.NET Core resource-based authorization requires an imperative decision once the resource/context is available. [Microsoft resource authorization guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)

Household roles are not global Identity roles:

- Owner in Household A may be a Guest/nonmember in Household B.
- Membership/role must come from current resource state, not a long-lived global claim.
- client UI capabilities are hints only; the server rechecks.

### Initial permission matrix

This is a starting product decision to validate in Phase 3. Unspecified actions deny.

| Operation | Owner | Member | Guest |
|---|:---:|:---:|:---:|
| View household/members | yes | yes | yes |
| Rename household | yes | no | no |
| Invite/remove/change role | yes | no | no |
| Transfer ownership | yes | no | no |
| Create/update/complete tasks | yes | yes | no |
| Manage routines | yes | yes | no |
| Add/check shopping items | yes | yes | no |
| Create/update events | yes | yes | no |
| View Today/content | yes | yes | yes, if product confirms read-only access |

Domain invariants still apply even to Owner: a continuing Household must retain
an Account-linked Owner. Last-Owner departure requires explicit TransferOwnership
or explicit CloseHousehold under the adopted deletion design. No Member/Guest
is automatically promoted, and a loginless HouseholdMember cannot be Owner.
Exact endpoint/permission details for the new lifecycle operations remain for
implementation review; the illustrative matrix does not replace these rules.

### 401/403/404 policy

- 401: no valid authentication.
- 403: authenticated known member lacks the requested operation and revealing the household relationship is acceptable.
- 404: nonmember/cross-household lookup where existence should not be disclosed, and genuine absence. Keep timing/body shape reasonably consistent.
- 409: recognized state/unique/concurrency conflict.

Do not reveal another household through counts, validation messages, invitation status, or child-resource errors.

## IDOR/BOLA integration-test matrix

OWASP states that every API function using a client-supplied object ID needs object-level authorization, regardless of whether IDs are UUIDs. [OWASP API1:2023 BOLA](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/)

Create test actors:

- anonymous.
- Household A Owner, Member, and Guest.
- Household B Owner who is a nonmember of A.
- removed/expired Household A member with a still-valid access token.
- deleted Account with a previously issued, unexpired access token.

Create Household A/B resources and test every read/list/create/update/delete/role/invite/assignment operation:

- anonymous -> 401.
- allowed actor -> success.
- known member with disallowed role -> 403, zero mutation.
- nonmember substitutes HouseholdId in path/query/body -> consistent 404, no detail.
- caller pairs Household A route with Task/Item/Event owned by Household B -> denied.
- random and known foreign UUIDs have no data leak.
- list/search/count/pagination returns only accessible rows.
- bulk operation authorizes every child.
- removed member loses Household access even while its Account remains valid.
- deleted Account cannot make new protected product requests after deletion commit.
- request-supplied creator/owner/Account ID cannot override authenticated identity.
- self-promotion, unauthorized role escalation, and last-owner removal fail.
- concurrent demote/remove/leave/transfer/close and Account deletion attempts
  preserve an Account-linked Owner in every continuing Household or complete an
  approved explicit closure. Prove rollback and recognized conflicts with real
  PostgreSQL; the precise concurrency strategy is OPEN, not a mandated version
  token or EF API.
- invitation acceptance uses one use-case-specific transaction over Invitation and Household; it binds the verified actor to the target and atomically consumes exactly one pending/unrevoked/unexpired row through a concurrency token or conditional update. A unique token-hash index, injected mid-transaction failure, duplicate requests, and two different authenticated acceptors prove single-use safety.
- failure produces no row, email, or later outbox message.
- response DTO exposes no Identity password hash/security stamp or unauthorized properties.

Blocking example names:

```text
Create_household_uses_authenticated_account_not_request_account_id
Get_household_as_non_member_returns_not_found
Rename_household_as_guest_returns_forbidden_without_mutation
List_households_returns_only_current_accounts_memberships
Update_task_rejects_task_from_another_household
Assign_task_rejects_assignee_from_another_household
Removed_member_cannot_access_with_still_valid_access_token
Concurrent_owner_transfers_preserve_at_least_one_owner
```

## Account lifecycle acceptance tests

### Registration/verification

- valid registration creates Identity user without leaking password.
- `IdentityOptions.User.RequireUniqueEmail = true`; a named unique filtered NormalizedEmail constraint is the race guard, and only its PostgreSQL `23505` is translated to the safe duplicate response.
- concurrent same-normalized-email registrations produce one account and controlled responses, never an unhandled 500.
- duplicate/unknown email responses do not create an enumeration oracle where avoidable.
- weak/compromised-policy password is rejected with safe validation.
- unconfirmed account follows the chosen access restriction.
- confirmation token is URL-safe, expiring, single-purpose, and redacted from logs.
- resend is rate-limited.

### Login/session

- use a separate real-Identity test factory with the Phase 1 fake scheme removed.
- valid bearer login/challenge succeeds; `useCookies=true` is rejected under the default ADR.
- invalid password, unknown user, unconfirmed/not-allowed user, and locked account share one public 401 body/status while internal telemetry distinguishes them.
- repeated failure triggers configured lockout and rate limit.
- malformed, expired, wrong-purpose, and tampered token is 401.
- refresh returns working new tokens and the client replaces its held token; test/document prior-token reuse until expiry/security-stamp change rather than asserting one-time consumption.
- security stamp/password change blocks refresh and bounds old access validity.
- device logout removes local credentials; sign-out-everywhere semantics are separately proven.
- the selected Data Protection persistence/protection design preserves authentication
  and confirmation/reset tokens across replica restart, revision replacement and
  key rotation. Blob/Key Vault is a PROPOSED option; provider/region and relevant
  retention decisions remain OPEN.

### Recovery

- forgot-password returns generic response for known/unknown email.
- reset token expires/cannot be reused and is bound to the user/purpose.
- password reset changes security stamp and old credentials/session behavior matches the documented decision.

### DeleteAccount and Household lifecycle — required future proof

- resolve ownership across all Households, then explicitly delete every
  Account-linked membership before Identity deletion; prove no loginless conversion;
- non-last-Owner departure preserves the Household and other people's roles;
- unresolved last Owner blocks deletion; explicit eligible transfer or closure
  is required, including when loginless children remain;
- no automatic promotion, no Owner with null AccountId, no ownerless continuation;
- direct Identity deletion cannot cascade/delete/null unresolved memberships;
- approved lifecycle plus Identity deletion commits atomically, with rollback
  and concurrency evidence; precise concurrency implementation remains OPEN;
- previously issued access/refresh tokens cannot restore product access after
  Account deletion; exact access lifetime remains an independent OPEN choice;
- test the adopted Account-linked membership deletion rule and later concrete
  feature/shared-data and reauthentication decisions; no grace period or
  retention “just in case”;
- each persisted feature follows the
  [lifecycle checklist](../../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist):
  remove unnecessary personal references from surviving shared records, prevent
  accidental reference disclosure, define missing-attribution UI and verify
  feature-specific deletion without treating nulling as proven anonymization.

## API and operational controls

### ProblemDetails/error hygiene

Use a stable contract such as:

```json
{
  "type": "https://homeplatform.example/problems/validation",
  "title": "The request was invalid.",
  "status": 400,
  "code": "household.name_invalid",
  "traceId": "...",
  "errors": { "name": ["..."] }
}
```

No stack trace, source path, SQL, Postgres error detail, connection string, secret/token, or another user's resource data. Map only recognized expected errors; unexpected failures stay generic 500 and are diagnosed through protected logs.

### Rate limiting

Use ASP.NET Core's built-in rate-limiting middleware and attach named policies to expensive/abusable endpoints. Partition login/reset/invite by a safe combination of IP and normalized account key, considering trusted-proxy forwarding. Return 429 and Retry-After where available; metrics/alerts must distinguish attacks from bad UX. [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)

### HTTPS/proxy/CORS

- loopback HTTP can remain for development.
- non-loopback auth traffic requires TLS.
- configure trusted forwarded headers for the selected hosting proxy before auth
  redirects/scheme-sensitive behavior; provider choice remains OPEN. Avoid
  redirect loops and do not trust arbitrary forwarded headers.
- let the hosting edge enforce HTTPS/HSTS where appropriate, and make application behavior match it. [ASP.NET Core proxy guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- CORS is not authentication and does not affect native clients; allow exact web origins only.

### Secrets/configuration

- source: names/placeholders/disposable local defaults only.
- developer machine: environment/user-secrets, never committed `.env` secrets.
- CI: protected environment secrets or workload identity.
- PROPOSED if Azure is selected: managed identity to Key Vault/other managed
  services where supported; no provider is adopted here.
- validate required options at startup and fail closed.
- rotate credentials/keys and test application behavior.

### Database

- separate schema migration and runtime roles.
- runtime role limited to required tables/operations; no create/drop/role administration.
- encrypted transport and restricted network access.
- automated point-in-time backups plus an isolated restore drill.
- do not put production credentials in a migration bundle or source.

### Logging/telemetry

Log event name, safe pseudonymous user/resource identifiers, request/trace ID, outcome, duration, deployment version, and failure category. Never log Authorization, cookie contents, passwords, tokens, connection strings, full emails, or household free text. Add tests/structured-log review for authentication and error paths.

## Phase security gates

### Phase 1 gate

- **PASS:** ADR 0006 is explicitly Accepted and the durable core identity schema
  aligns with it.
- **PASS:** endpoint request contains only Name.
- **PASS:** command contains no actor ID; handler injects current Account.
- **PASS:** route exists only in the authenticated Testing host; a Production
  host test proves 404/unmapped.
- **PASS:** missing/malformed Testing-host Guid subjects receive 401 with zero
  rows.
- **PASS:** generated Testing OpenAPI contract, unexpected-error non-disclosure,
  connected dependency audit, green formatting, and discoverable CI with a
  green local equivalent. Current hosted execution is NOT VERIFIED by this audit.

### Phase 2 gate

- Identity lifecycle, hashing, trusted user, lockout, secure token/cookie mode, TLS for non-loopback, and auth tests green.
- no multi-user invitation endpoint before this gate.

### Phase 3 gate

- permission matrix and complete Household IDOR suite green.
- Account linking is explicitly authorized, preserves Membership identity, and
  rejects duplicate scoped links under concurrency.
- PostgreSQL race tests preserve an Account-linked Owner in continuing Households
  and explicit closure semantics; precise concurrency strategy remains OPEN.
- invitation acceptance commits Invitation and Household once or rolls both back under injected failure/concurrency.
- conditional invitation consume binds the verified target and permits exactly one winner across duplicate/different-actor attempts.

### Phase 4–5 gate

- every child resource is authorized through its actual Household relationship.
- mass-assignment DTOs cannot change actor/household/owner fields.
- list/projection queries cannot leak cross-household data.

### Public beta gate

- real verification/reset email, rate limits, CORS policy if web, restricted
  production hosts/proxies, safe errors/configuration, isolated environments,
  least privilege, persistent protected Data Protection keys with tested
  rotation/continuity under the selected provider design, additive migration/rollback proof, dependency/container
  scans, backup/restore, export/deletion/shared-record-fate behavior, redaction,
  and session/revocation decisions proven in staging.

### Production / real-user privacy gate

- Before real-user release, resolve controller/legal-basis, hosting/provider/
  region, retention and backup decisions; verify the adopted deletion/access
  behavior, rights handling, privacy information, recovery, key/secret rotation,
  incident/alert/runbook ownership and final security review.
- The broader phase labels do not postpone these privacy/security requirements
  until after real users enter beta. The initial
  [privacy documents](../../README.md#privacy-and-lifecycle) record open facts,
  not proof of compliance or production readiness.

## Things security does not justify yet

- global Identity roles for household permissions.
- custom JWT/password/token cryptography.
- OAuth/OIDC server before its decision trigger.
- security by UUID secrecy.
- API gateway, Web Application Firewall, service mesh, broker, or microservices as substitutes for code-level resource authorization.
- logging every payload “for audit”.
- a blanket 200 response that hides all auth errors and destroys observability.
