# HomePlatform Security Roadmap

Status: **Authoritative security plan**  
Last reviewed: **2026-09-02**
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
| ProblemDetails/central exception middleware configured | PARTIAL | Api registers `AddProblemDetails` and `UseExceptionHandler`; expected invalid Household input maps to 400. Stable error codes/trace assertions and unexpected-provider non-disclosure tests are absent. |
| Development OpenAPI restricted by environment | PARTIAL | OpenAPI is mapped only in Development, but the Household route is mapped only in Testing, so no HTTP-exposed document contains it and no document-generation test proves its declared responses. |
| PostgreSQL readiness without credential disclosure | PASS | `/ready` reports ready/unavailable only. |
| Known NuGet vulnerability scan | HISTORICAL PASS / NOT REVERIFIED | the 2026-08-27 audit reported no known vulnerable direct/transitive packages; connected CI must refresh advisory data. |
| Authentication/account lifecycle | FAIL | not implemented. |
| Authorization/resource checks | FAIL | not implemented. |
| Trusted creator identity | PASS for Testing slice | command carries Name only; `ICurrentAccount` derives the actor from authenticated claims; anonymous/malformed and caller-supplied AccountId tests prove zero impersonation. |
| Rate limiting/lockout policy | FAIL | not implemented. |
| HTTPS/reverse-proxy production policy | FAIL | not implemented; local launch is HTTP. |
| CORS policy | NOT VERIFIED / not required yet | no frontend/API cross-origin contract exists. Absence is safer than `AllowAnyOrigin`; configure exact origins only when Expo web exists. |
| Secrets management | PARTIAL | disposable development credentials are tracked in example/development files; no production secret store/configuration exists. |
| Logging redaction policy | FAIL | default logging exists; no explicit sensitive-data rules/tests. |
| Security integration tests | PARTIAL | fake-auth CreateHousehold tests cover 401, actor trust, zero rows, and Production 404; real Identity, IDOR, token, redaction, and unexpected-error disclosure tests are absent. |
| Database least privilege/backups/restore | NOT VERIFIED | no deployed database or production roles exist. |

Production currently maps only health/readiness; the product route exists only
in the Testing environment. The security stop-gate is to keep it absent from
Production until real Identity and trusted caller handling are ready.

## Trust model

Untrusted input includes every path, query, header, body field, mobile state, cached permission, and Guid supplied by a client. Random UUIDs are not authorization.

Trusted inputs are created or validated on the server:

- authenticated subject/AccountId from ASP.NET Core authentication;
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
requests and commands never select their actor, and health endpoints are
explicitly anonymous when a fallback policy exists.

## Authentication architecture decision

### Recommended six-month default

Use ASP.NET Core Identity in Infrastructure with PostgreSQL EF stores:

- `ApplicationUser : IdentityUser<Guid>` belongs in Infrastructure/Identity.
- the existing `HomePlatformDbContext` becomes roleless `IdentityUserContext<ApplicationUser, Guid>` unless a real global-role need emerges; call `base.OnModelCreating(builder)` before applying product configurations so one database and migration chain remain.
- use Identity's `UserManager`, `SignInManager`, password hasher, security stamps, confirmation/reset tokens, lockout, and API endpoints.
- Domain does not reference Identity and never owns password/security/token fields.
- do not persist the current Domain `User` as a second credential source.
  Retain/rename it to a Profile only when concrete behavior and ownership are
  approved; do not silently equate Profile, Account, and Membership identity.

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
- document that an already issued bearer access token may remain valid until its configured expiration. Test the maximum window.

Before public beta, make an explicit acceptance decision: if the product requires immediate per-device server revocation, device-session inventory, refresh-token reuse detection, social federation, third-party clients, standard OAuth/OIDC/JWT interoperability, or SSO, Identity's simple bearer mode may not suffice. Select an established OAuth/OIDC server/provider and Authorization Code + PKCE rather than extending a custom token server.

### Browser client branch

If Expo web/browser becomes a first-class client, prefer same-site `HttpOnly`, `Secure`, appropriately `SameSite` cookies and antiforgery protection. Configure exact CORS origins and credential behavior. Never combine wildcard origins and credentials. [ASP.NET Core CORS guidance](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0)

### Data Protection keys

Persist ASP.NET Core Data Protection keys in an environment-scoped Azure Blob Storage key-ring repository under a stable application name, protect the key material with a versionless environment-scoped Key Vault key identifier, retain old wrapping-key versions for as long as old protected payloads may need decryption, and use least-privilege managed identity for both. Key Vault alone is not the shared key-ring repository. Explicitly selecting external persistence disables default at-rest protection, so both persistence and protection are mandatory. Test authentication plus confirmation/reset-token continuity across replica restart, revision replacement, and wrapping-key rotation. [Data Protection key storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0), [Key Vault rotation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)

## Required security classification

The category is the latest acceptable gate. Work can start earlier.

| Topic | Classification | Phase | Blocking acceptance condition |
|---|---|---:|---|
| ASP.NET Core Identity | BEFORE MULTI-USER FEATURES | 2 | Identity EF store/migrations exist; Domain remains Identity-free. |
| Password hashing | BEFORE MULTI-USER FEATURES | 2 | passwords pass only through Identity/UserManager; no reversible/custom storage. |
| Account registration | BEFORE MULTI-USER FEATURES | 2 | `RequireUniqueEmail`, named unique filtered PostgreSQL index on NormalizedEmail, recognized `23505` mapping, and concurrent-registration proof. |
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
| Export/deletion/shared-record fate | BEFORE PUBLIC BETA | 6 | authorized export, deletion/retention rules, and shared-data outcomes are documented and tested. |
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

Domain invariants still apply even to Owner: the last Owner cannot be removed/demoted/leave without a transfer.

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

Create Household A/B resources and test every read/list/create/update/delete/role/invite/assignment operation:

- anonymous -> 401.
- allowed actor -> success.
- known member with disallowed role -> 403, zero mutation.
- nonmember substitutes HouseholdId in path/query/body -> consistent 404, no detail.
- caller pairs Household A route with Task/Item/Event owned by Household B -> denied.
- random and known foreign UUIDs have no data leak.
- list/search/count/pagination returns only accessible rows.
- bulk operation authorizes every child.
- removed member loses access even while authentication remains valid.
- request-supplied creator/owner/Account ID cannot override authenticated identity.
- self-promotion, unauthorized role escalation, and last-owner removal fail.
- every membership mutation changes a Household version concurrency token; barrier-based concurrent demote/remove/leave/transfer attempts preserve at least one Owner and a stale writer receives 409.
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
- Blob-persisted, Key-Vault-protected Data Protection keys preserve authentication and confirmation/reset tokens across replica restart, revision replacement, and versionless wrapping-key rotation; old Key Vault key versions remain retained.

### Recovery

- forgot-password returns generic response for known/unknown email.
- reset token expires/cannot be reused and is bound to the user/purpose.
- password reset changes security stamp and old credentials/session behavior matches the documented decision.

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
- configure trusted forwarded headers for Azure's proxy before auth redirects/scheme-sensitive behavior. Avoid redirect loops and do not trust arbitrary forwarded headers.
- let the hosting edge enforce HTTPS/HSTS where appropriate, and make application behavior match it. [ASP.NET Core proxy guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- CORS is not authentication and does not affect native clients; allow exact web origins only.

### Secrets/configuration

- source: names/placeholders/disposable local defaults only.
- developer machine: environment/user-secrets, never committed `.env` secrets.
- CI: protected environment secrets or workload identity.
- Azure: managed identity to Key Vault/other managed services where supported.
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
- **OPEN:** generated OpenAPI contract, stable ProblemDetails code/trace proof,
  unexpected-error non-disclosure, connected dependency audit, green formatting,
  and discoverable CI.

### Phase 2 gate

- Identity lifecycle, hashing, trusted user, lockout, secure token/cookie mode, TLS for non-loopback, and auth tests green.
- no multi-user invitation endpoint before this gate.

### Phase 3 gate

- permission matrix and complete Household IDOR suite green.
- Account linking is explicitly authorized, preserves Membership identity, and
  rejects duplicate scoped links under concurrency.
- Household version concurrency plus barrier-based PostgreSQL tests preserve the last-owner invariant.
- invitation acceptance commits Invitation and Household once or rolls both back under injected failure/concurrency.
- conditional invitation consume binds the verified target and permits exactly one winner across duplicate/different-actor attempts.

### Phase 4–5 gate

- every child resource is authorized through its actual Household relationship.
- mass-assignment DTOs cannot change actor/household/owner fields.
- list/projection queries cannot leak cross-household data.

### Public beta gate

- real verification/reset email, rate limits, CORS policy if web, restricted
  production hosts/proxies, safe errors/configuration, isolated environments,
  least privilege, Blob-persisted and Key-Vault-protected keys with retained
  rotation versions, additive migration/rollback proof, dependency/container
  scans, backup/restore, export/deletion/shared-record-fate behavior, redaction,
  and session/revocation decisions proven in staging.

### Production gate

- post-beta operating controls—key/secret rotation rehearsal, incident/alert/runbook ownership, retention/privacy policy, recovery evidence, and final threat review—are green.

## Things security does not justify yet

- global Identity roles for household permissions.
- custom JWT/password/token cryptography.
- OAuth/OIDC server before its decision trigger.
- security by UUID secrecy.
- API gateway, Web Application Firewall, service mesh, broker, or microservices as substitutes for code-level resource authorization.
- logging every payload “for audit”.
- a blanket 200 response that hides all auth errors and destroys observability.
