# Application Security Assessment & VAPT Readiness Report

**Application:** Automated Onboarding (AOB) — ASP.NET Core MVC, .NET 10, EF Core 10 / SQL Server
**Solution layout:** `AOB.WEB` (MVC), `AOB.BAL` (entities/DTOs/rules), `AOB.DAL` (EF Core, services), `AOB.Tests`
**Branch / date reviewed:** `feature/users-csv-templete`, 2026-10-29 (working tree incl. uncommitted Health/IP-tracking changes)
**Method:** manual white-box source review plus `dotnet list package --vulnerable --include-transitive`. Nothing was run dynamically; no infrastructure was inspected.

> Verification convention: **Verified** = confirmed in code/config I read. **Not Verified** = could not be confirmed from the repository; it is listed in section 6 and is neither assumed secure nor assumed vulnerable.
> Secret values are deliberately not reproduced in this report; they are referenced by file and location.

---

## 1. Executive summary

The application has a sound security *skeleton*: deny-by-default authorization, global CSRF validation, EF Core parameterised queries, hashed one-time tokens, a lockout policy, safe file-storage paths, and download hardening. Ownership scoping (partner → customer → project) is implemented consistently in the code I read.

It is **not ready for an independent VAPT** until the items in section 5 (Phase 0 and Phase 1) are done. The main problems are secrets management and session lifecycle, not classic injection bugs:

| # | Headline issue | Severity |
|---|---|---|
| F-01 | Microsoft Graph client secret (can send mail as `software@inovedia.com`) is committed to git | **Critical** |
| F-02 | PCI Pal service credentials hard-coded in source, committed, and copied in plaintext into every project row | **High** |
| F-03 | Deactivated / disabled accounts keep working until their cookie dies (no server-side session revalidation) | **High** |
| F-04 | No rate limiting anywhere; OTP attempt limit is resettable; email-bombing and enumeration are open | **Medium-High** |
| F-05 | No CSP / anti-clickjacking / hardening headers; auth cookie not forced `Secure` | **Medium** |
| F-06 | `/health` leaks host, OS, DB server/name/user and raw exceptions, gated only by a spoofable-by-topology IP check | **Medium** |

Counts: 1 Critical, 2 High, 9 Medium, 10 Low/Informational (section 3).

---

## 2. Security controls — what is implemented correctly (Verified)

| Area | Evidence |
|---|---|
| Deny-by-default authorization | Global `AuthorizeFilter` in `Program.cs:7-11`; only `AuthController`, `HealthController`, `HomeController.Error` are `[AllowAnonymous]`. |
| Role-based access | Controllers carry `[Authorize(Roles=…)]`; sensitive actions are narrowed further (`InternalUser`-only create/approve, `SuperAdmin`-only internal users and IP admin). |
| Object-level authorization (IDOR) | Customer id always comes from the claim (`CustomerDocumentsController.GetCustomerId`). Partners must pass `PartnerOwnsCustomerAsync` (`CustomersController.ResolvePartnerIdAsync`, `ProjectsController.OnActionExecutionAsync`). Staff are forced read-only except a named allowlist. Services re-scope by `GetForCustomerAsync`. IDs are GUIDs. |
| CSRF | `AutoValidateAntiforgeryTokenAttribute` applied globally (`Program.cs:10`); the one cross-origin form (PCI Pal iframe) correctly sets `asp-antiforgery="false"`. |
| SQL injection | No `FromSqlRaw`, `ExecuteSqlRaw` or string-built SQL anywhere in `AOB.WEB/BAL/DAL`; all data access is EF Core LINQ. |
| XSS | Razor auto-encoding throughout. Only `Html.Raw` uses are JSON-serialised constants (`TechnicalEnvironment.cshtml:171`, `_ValidationScriptsPartial.cshtml:6`) and a GUID-built attribute (`_ProjectDocumentsPanel.cshtml:89`). No `innerHTML` with user data in `wwwroot/js`. Email template values are `HtmlEncode`d (`EmailService.cs:80`). |
| Password storage | PBKDF2-HMAC-SHA256 with 16-byte random salt, constant-time compare (`PasswordHasher.cs`). |
| Token / OTP handling | CSPRNG generation; only SHA-256 hashes stored; single-use; expiry; per-OTP attempt cap; older tokens invalidated on re-issue (`OtpService`, `SecurityTokenHelper`). |
| Account lockout | 5 failures → 15 min lockout, applied to login and change-password. |
| Enumeration resistance | Uniform message on login failure; uniform response on forgot-password and resend-verification. |
| Open redirect | `Url.IsLocalUrl` check on `returnUrl` (`AuthController.cs:58`). |
| Temporary-password flow | A correct temporary password does **not** create a session; it only mints a one-time grant (`AuthService.LoginAsync`). Temp passwords are 16 chars from a CSPRNG and never logged. |
| File storage | Stored name is a generated GUID; folder from an enum; `ResolvePath` blocks traversal; store is outside `wwwroot` (`LocalFileStorage`). Extension allowlist and size limit enforced server-side. Downloads served as attachment with `X-Content-Type-Options: nosniff`. |
| Sensitive page caching | `Cache-Control: no-store` on the flow-launch page that carries live tokens. |
| Email links | Built from configured `AppSettings:BaseUrl`, not the `Host` header, so host-header poisoning of verification links is not possible. |
| Secrets in repo hygiene (partial) | `.gitignore` covers `.env`, `*.pfx`, `App_Data/`. |
| Dependencies | `dotnet list package --vulnerable --include-transitive` on `AOB.WEB`: **no vulnerable NuGet packages** reported. jQuery 3.7.1 (current). |
| HTTPS | `UseHttpsRedirection` and `UseHsts` (non-Development) are present. |
| Input constraints | `StringLength`/`EmailAddress`/`PasswordPolicy` on registration; CSV import capped (2 MB, 500 rows, strict validation). |

---

## 3. Findings

Severity follows likelihood × impact for this application (a portal that holds customer onboarding data, PCI-adjacent integration credentials, and a trusted mail sender).

### Critical

#### F-01 Microsoft Graph client secret committed to source control
- **Where:** `AOB.WEB/appsettings.json` (`MicrosoftGraph:ClientSecret`, plus tenant and client IDs); introduced in commit `31bb1da` and present in `HEAD`. Remote is Azure DevOps.
- **Impact:** The secret authenticates an app-only Graph client that sends mail as `software@inovedia.com`. Anyone with repo read access (or any clone/backup/fork) can send phishing mail from a trusted internal address. The secret remains in git history after removal.
- **OWASP:** A02 Cryptographic Failures / A05 Security Misconfiguration (CWE-798, CWE-540).
- **Fix:** Rotate the secret in Entra ID now. Move to environment variable, user-secrets (dev) or Key Vault (prod). Scope the app registration with an Exchange *Application Access Policy* so it can only send as that one mailbox. Treat the old value as burned; purge history only after rotation.

### High

#### F-02 Hard-coded PCI Pal service credentials, stored plaintext per project
- **Where:** `AOB.DAL/Implementations/Services/ProvisioningSteps.cs:49-51` (username, client id, client secret literals); persisted to `ProjectProvisioningDetail.ClientSecret` (`AOB.BAL/Entities/ProjectProvisioningDetail.cs:35`) with no encryption; used in `FlowLaunchService`. Committed in several commits.
- **Impact:** Credential disclosure via repo; every project shares one identity, so compromise or DB read exposes the PCI Pal tenant for all customers. A SQL-level read (backup, SQLi elsewhere, DBA) yields usable secrets.
- **Fix:** Rotate. Load from secret store; do not copy into per-project rows (store a reference/tenant id only), or encrypt the column with ASP.NET Data Protection / Always Encrypted. Once real provisioning replaces these stubs, issue per-tenant least-privilege credentials.

#### F-03 No server-side validation of the auth cookie (disable/role change/password change do not end sessions)
- **Where:** `Program.cs:26-35` — cookie options set, but no `OnValidatePrincipal`, no security stamp. UI text claims "deactivated and can no longer sign in" (`PartnersController.cs:78`), but only *new* sign-ins are refused (`AuthService.LoginAsync`).
- **Impact:** A deactivated partner/internal user/customer, or a user whose password was reset after compromise, keeps full access for up to 8 h and indefinitely while active (`SlidingExpiration = true`). Claims (role, partner id, customer id) are frozen at login.
- **OWASP:** A07 Authentication Failures, A01 Broken Access Control (CWE-613).
- **Fix:** Add a security stamp (or `UpdatedAt`/`IsActive`/`PasswordHash` version) to the claims and compare in `OnValidatePrincipal` (a cheap indexed lookup, optionally cached ~1 min); reject if `IsActive=false`, customer `Disabled`, or stamp changed. Rotate stamp on password change/reset/disable. Sign out on mismatch.

### Medium

#### F-04 No rate limiting / abuse controls on anonymous endpoints
- **Where:** none configured (no `AddRateLimiter`). Affects `Auth/Login`, `ForgotPassword`, `VerifyOtp`, `ResendVerification`, `Register`, `IsEmailAvailable`.
- **Issues:**
  1. `ForgotPassword` issues a fresh OTP each call and `IssueAsync` invalidates the old one, which **resets the 5-attempt budget** (`OtpService.cs`). An attacker with a victim's email gets unlimited 5-guess rounds (and floods the victim's inbox). Practical brute force is slow but not bounded.
  2. Email bombing through Graph (forgot/resend/register) and consumption of the sender's mail quota/reputation.
  3. `Auth/IsEmailAvailable` is an unauthenticated, unthrottled email-existence oracle; `Register` also discloses existence ("email in use").
  4. Lockout is per account only: anyone can lock out any known email (including the super admin) indefinitely by repeating bad logins; no per-IP throttle, no CAPTCHA.
- **Fix:** `Microsoft.AspNetCore.RateLimiting` policies per IP and per email (e.g. 5/min login, 3/hour forgot-password per email); cap OTP issues per account per hour and keep the failure counter across re-issues; replace `IsEmailAvailable` with submit-time check only or throttle hard; consider CAPTCHA on register/forgot.

#### F-05 Missing browser-side hardening: security headers and cookie flags
- **Where:** `Program.cs` (no header middleware); `_Layout.cshtml` loads Google Fonts and inline scripts/styles. `X-Content-Type-Options` is set only on file downloads.
- **Missing:** `Content-Security-Policy`, clickjacking protection (`frame-ancestors`/`X-Frame-Options`), `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-*`. Auth cookie has `HttpOnly` but no explicit `Secure` (`SecurePolicy` defaults to *SameAsRequest*), no explicit `SameSite`, default cookie name, and `IsPersistent` "remember me" with 8 h sliding window. HSTS uses the 30-day default without `includeSubDomains`/preload.
- **Impact:** Any XSS has no second line of defence; the app can be framed (clickjacking on approve/delete actions); cookie can be sent over HTTP if an HTTP listener is exposed.
- **Fix:** Add a header middleware (CSP with nonces; `frame-ancestors 'none'`; `Referrer-Policy: strict-origin-when-cross-origin`). Set `Cookie.SecurePolicy = Always`, `SameSite = Strict` (or Lax with intent), `Cookie.Name = "__Host-aob"`. Raise HSTS to 1 year. Self-host fonts or allow-list them in CSP. Add `sandbox`/`allow` attributes to the PCI Pal iframe as far as the vendor flow permits.

#### F-06 `/health` discloses infrastructure detail and relies on IP trust
- **Where:** `HealthController.cs`, `HealthIpFilterAttribute.cs`, `appsettings.json` (`HealthCheck:AllowedIps` includes a public IP and `::1`).
- **Leaks:** machine name, OS, runtime, uptime, base URL, DB server/name/user (connection string with only the password masked), DB server version, pending migration names, absolute upload path, raw exception messages (`ex.Message`).
- **Risks:** (a) `ForwardedHeaders` is not configured, so behind IIS/ARR, nginx or a load balancer `RemoteIpAddress` is the *proxy*. If the proxy is local, `::1` is in the allow-list and **everyone** is allowed; if remote, the allowed IP is wrong and the filter is meaningless. (b) Allow-listing a single public IP is weak authentication.
- **Fix:** Require authentication (SuperAdmin or an API key) *in addition to* IP checks; return only status codes for anonymous probes; drop machine/OS/connection string/exception text from output; configure `UseForwardedHeaders` with `KnownProxies`/`KnownNetworks`; remove `::1` from production config.

#### F-07 Seed credentials and default environment
- **Where:** `AOB.WEB/appsettings.Development.json` (tracked) holds the plaintext password for a **SuperAdmin** and the PCIPAL partner (same password for both); `DbSeeder` creates them if configured, with `EmailConfirmed = true` and no forced change. `launchSettings.json` defaults to `Development`.
- **Impact:** Known super-admin credential in the repo; if the Development config/DB is ever reachable from the internet or reused for staging, it is a full takeover. A runtime started without `ASPNETCORE_ENVIRONMENT` set will load these.
- **Fix:** Remove passwords from tracked files; seed via secret/env var, generate random, and set `MustChangePassword = true`. Rotate the existing passwords. Disable seeding outside first-run.

#### F-08 File uploads validated by extension only
- **Where:** `CustomerDocumentService.UploadAsync`, `ProjectDocumentService.UploadAsync`, `ProjectInformationService`.
- **Gaps:** no content/magic-number check, no malware scanning, client-supplied `Content-Type` stored and later echoed on download, legacy macro-capable formats (`.doc`, `.xls`) allowed, files stored unencrypted on local disk. Mitigated by attachment disposition, `nosniff`, GUID names.
- **Impact:** Malware or macro documents uploaded by an external party are later opened by internal staff (the highest-value target); polyglot files evade the extension check.
- **Fix:** Verify signatures, drop `.doc/.xls` or scan with AV (Defender/ClamAV) before availability, derive `Content-Type` server-side from the validated extension, quarantine until scanned, restrict ACLs on `App_Data/Uploads`, consider encryption at rest.

#### F-09 No audit trail for authentication and privileged events
- **Where:** `AuthService` records only customer registration in `ActivityLog`. Login success/failure, lockout, password change/reset, email-verification, account (de)activation, IP block/unblock, and role-sensitive actions are not auditable; default `ILogger` only.
- **Impact:** Incident response and the evidence a PCI-adjacent assessor expects (PCI DSS 10.x / ISO A.8.15) are unavailable.
- **Fix:** Append-only security event log (who, what, source IP, UA, outcome) with alerts on repeated failures; ship to central logging.

#### F-10 Sensitive data written to logs when mail is not configured
- **Where:** `EmailService.SendAsync` logs the **full HTML body**, which includes password-reset OTPs and verification links, when Graph settings are incomplete (only `InternalUserCredentials` is exempt). Graph error bodies are also embedded in exception messages.
- **Fix:** Never log bodies for any auth template (log template name only); gate the dev fallback on `IsDevelopment()`.

#### F-11 Database transport and privilege
- **Where:** connection string `TrustServerCertificate=True` (certificate validation off), and `DbSeeder.InitializeAsync` runs `MigrateAsync()` at every startup, so the app login needs DDL rights. 
- **Fix:** Use a trusted certificate and `TrustServerCertificate=False`/`Encrypt=True`; apply migrations from a deployment step with a separate privileged identity; run the app as a least-privileged login (read/write on its schema only).

#### F-12 Unbounded work from anonymous traffic in the new IP tracker
- **Where:** `Program.cs:56-77`, `IpAccessTracker` (uncommitted).
- **Issues:** every distinct remote address creates an in-memory `Pending` entry that is never evicted, then a `VisitorIps` row, a `Task.Run`, and an outbound geolocation call to a third-party (`ipwho.is`). An IPv6 /64 gives an attacker unlimited addresses (memory and table growth, DB write amplification, outbound call amplification). Blocking is by IP only and also depends on the forwarded-headers issue in F-06. Visitor IPs are sent to a third party (privacy / data-processing agreement).
- **Fix:** Normalise IPv6 to /64, cap/evict `_pending`, sample or skip unauthenticated 4xx noise, rate-limit geolocation, make lookup opt-in, document the data transfer.

### Low / Informational

| ID | Finding | Where | Recommendation |
|---|---|---|---|
| F-13 | PBKDF2 iteration count 100 000; OWASP currently recommends ≥ 600 000 for HMAC-SHA256. No rehash-on-login, no versioned upgrade path (format has iterations only). `FromBase64String` throws on a malformed stored hash. | `PasswordHasher.cs` | Raise to ≥ 600 000 or move to Argon2id/`PasswordHasher<T>`; rehash on successful login; catch `FormatException`. |
| F-14 | Password policy caps at 30 chars, has no breached-password/common-password check, and uses composition rules (NIST 800-63B advises length over composition). | `PasswordPolicyAttribute.cs` | Allow ≥ 64 chars, add a deny-list/HIBP k-anonymity check. |
| F-15 | Login timing differs for unknown email (no hash computed) vs known; locked-out message exists only for real accounts. | `AuthService.LoginAsync` | Verify against a dummy hash; use the same message for lockout. |
| F-16 | OTP/token hashes are unsalted SHA-256; a 6-digit OTP is reversible instantly if the table leaks (10-min lifetime limits exposure). | `SecurityTokenHelper.Hash` | HMAC with a server-side key. |
| F-17 | Email verification is a state-changing `GET` with the token in the query string; mail scanners (Safe Links) can consume it, and for internal users it triggers sending a temporary password. | `AuthController.VerifyEmail` | GET shows a confirm page, POST performs the action. |
| F-18 | PCI Pal access/refresh tokens are rendered into hidden fields of the page (vendor flow requires POST of tokens). `no-store` is set; iframe has no `sandbox`. | `Views/Projects/FlowLaunch.cshtml` | Keep `no-store`; restrict iframe permissions; short-lived tokens only; confirm with the vendor. |
| F-19 | `AllowedHosts: "*"`. Not directly exploitable here (email URLs use config) but should be locked in production. | `appsettings.json` | Set the real host names. |
| F-20 | Data Protection keys are not configured (default per-machine key ring). Cookies and antiforgery tokens break on restart/scale-out, and key storage is unprotected by default. | `Program.cs` | Persist keys (file share/Blob/DB) and protect them (DPAPI/cert/Key Vault). |
| F-21 | Internal users set the initial partner password and can change a partner's login email, which is by design but is a high-trust path with no audit event and no forced change. | `PartnerService` | Send an invite/temp-password flow like internal users; audit it. |
| F-22 | Automated test coverage is 28 tests (build rules, CSV parser, token helper); none cover authorization, IDOR, lockout or file validation. | `AOB.Tests` | Add integration tests for role/ownership matrix before and after fixes. |

---

## 4. OWASP Top 10 (2021) mapping

| OWASP | Status | Findings |
|---|---|---|
| A01 Broken Access Control | Mostly sound; session revalidation gap | F-03, F-06 (design reviewed OK otherwise) |
| A02 Cryptographic Failures | Needs work | F-01, F-02, F-13, F-16, F-20 |
| A03 Injection | No issues found (SQL/XSS) | — |
| A04 Insecure Design | Rate limiting, enumeration, OTP reissue | F-04, F-17 |
| A05 Security Misconfiguration | Needs work | F-05, F-06, F-07, F-11, F-19 |
| A06 Vulnerable Components | No NuGet advisories; client libs partly Not Verified | §6 |
| A07 Identification & Authentication Failures | Needs work | F-03, F-04, F-14, F-15 |
| A08 Software & Data Integrity | Upload handling, secrets in repo | F-08, F-02 |
| A09 Logging & Monitoring Failures | Needs work | F-09, F-10 |
| A10 SSRF | Outbound hosts are fixed/stubbed today; becomes relevant once `Platform`/`Region` come from user input | F-02 note |

---

## 5. Remediation roadmap before VAPT

**Phase 0 — immediate (do today)**
1. Rotate the Graph client secret (F-01) and the PCI Pal client secret (F-02); rotate seed passwords (F-07).
2. Move all secrets to env/Key Vault/user-secrets; remove them from tracked files.

**Phase 1 — required before testing starts**
3. Cookie revalidation + security stamp (F-03).
4. Rate limiting and OTP re-issue cap (F-04).
5. Security headers, cookie `Secure`/`SameSite`, HSTS (F-05).
6. Lock down `/health` and configure forwarded headers (F-06, F-12).
7. Upload content validation and AV scanning (F-08).
8. Security event logging and removal of body logging (F-09, F-10).
9. DB TLS and least-privilege runtime identity (F-11).

**Phase 2 — hardening**
10. Password hashing parameters and policy (F-13, F-14), timing/enumeration cleanup (F-15), token hashing (F-16), GET→POST verification (F-17).
11. Data Protection key ring (F-20), `AllowedHosts` (F-19), authorization integration tests (F-22).

**Pen-test scope hints** (give the testers): role/ownership matrix (Customer, Partner, InternalUser, SuperAdmin), cross-partner GUID access on `Customers/{id}/Projects/*`, upload endpoints (customer docs, API documentation, information requests, users CSV), auth flows (temp password, forgot password, email verify), `/health`, `/IpAddresses`.

---

## 6. Not Verified (explicit)

I could not confirm the following from the repository; do not treat them as secure or insecure:

- **Infrastructure and hosting:** reverse proxy/WAF presence, TLS versions and ciphers, HTTP listener exposure, firewall rules, Windows/IIS hardening, OS patching, `ASPNETCORE_ENVIRONMENT` in production.
- **Production configuration:** actual production secrets, connection string, `AllowedIps`, whether `Seed:*` exists in production, Entra app-registration permissions (Mail.Send scope, any Application Access Policy).
- **Database:** SQL login permissions, column/at-rest encryption, backups and their access, row-level data exposure beyond EF scoping.
- **File storage:** NTFS ACLs on `App_Data/Uploads`, backup of uploads, AV on host.
- **Client libraries:** Bootstrap and SweetAlert2 versions were not checked against advisories (jQuery 3.7.1 and jQuery Validation not audited); no `npm audit`-style scan was possible.
- **Code areas read only partially:** `ProjectService` / `ProjectPurgeService` / `ProjectDeletionWorker` (delete/restore logic), `ProjectInformationService` ownership paths, `ProjectUsersService` beyond the CSV extension/size checks, EF Core query filters in repositories, and the remaining views. IDOR coverage was verified for customer documents, customer/partner controllers, and the `ProjectsController` pipeline; deeper per-service verification is recommended.
- **Dynamic behaviour:** nothing was executed (no DAST, no fuzzing, no session or timing measurement); race conditions in token consumption (`ConsumeEmailVerificationTokenAsync` is read-then-write without an explicit concurrency guard) were not exercised.
- **Compliance:** no assessment against PCI DSS, SOC 2 or ISO 27001 was performed; F-09 is flagged as likely relevant.

---

## 7. VAPT readiness verdict

**Not ready.** After Phase 0 and Phase 1 the application should be in a reasonable state for an external assessment; the design-level controls (authorization model, CSRF, injection resistance, token handling) are already a good base, so the remaining work is mostly configuration, secret handling and session lifecycle.

---

## 8. Remediation status (Medium findings, 2026-10-29)

F-04 to F-12 were addressed in code; F-01, F-02, F-03 and all Low items are untouched.

| ID | Status | Change |
|---|---|---|
| F-04 | Done | Per-IP rate limiting on all anonymous auth endpoints; max 5 reset codes / 5 verification links per account per hour. `IsEmailAvailable` is throttled but still an existence oracle. |
| F-05 | Done | CSP (still `'unsafe-inline'`; nonces are a follow-up), X-Frame-Options, Referrer-Policy, Permissions-Policy, nosniff; auth cookie named, `Secure` outside Development, `SameSite=Lax`; antiforgery cookie Strict/Secure; HSTS 1 year. |
| F-06 | Done | Host/OS/DB/path/exception detail shown only to a signed-in super admin; `::1` moved to Development config; opt-in `ForwardedHeaders:KnownProxies/KnownNetworks`. **Production must set these if behind a proxy.** |
| F-07 | Done | Seed passwords removed from tracked config (supply `Seed:*:Password` via user-secrets/env); seeded accounts must change password at first sign-in. Old passwords remain in git history; rotate them. |
| F-08 | Partly | Signature check, server-derived content type. `.doc`/`.xls` still allowed; AV scanning and storage ACLs are infrastructure. |
| F-09 | Partly | Structured `SecurityEvent` log entries (category `AOB.Security`) for sign-in, logout, reset, lockout, password change. Not a tamper-proof store; ship to central logging. Admin actions (activate/deactivate, IP block) not yet logged. |
| F-10 | Done | Email bodies logged only in Development; Graph error text truncated. |
| F-11 | Partly | `Database:MigrateOnStartup` toggle (default true). `TrustServerCertificate=True` is unchanged: fix with a trusted DB certificate. |
| F-12 | Done | IPv6 reduced to /64, tracked addresses capped at 20 000 with pruning, geolocation capped at 30/min. |

## 9. Remediation status (Low findings, 2026-10-29)

| ID | Status | Change |
|---|---|---|
| F-13 | Done | PBKDF2 raised to 600 000 iterations; old hashes verify and are upgraded at next sign-in; malformed hashes fail cleanly. |
| F-14 | Partly | Maximum length raised to 64. Breached/common-password check not added (needs a deny-list or an external lookup). |
| F-15 | Done | Unknown and locked accounts do the same hashing work and give the same message as a wrong password. |
| F-16 | Not done | Needs a server-side HMAC key (new secret) and invalidates tokens in flight; decide on key storage first. |
| F-17 | Done | Email link opens a confirm page; verification is a POST. |
| F-18 | Partly | Iframe `sandbox` added; test the flow with the vendor, as the vendor page may need more permissions. |
| F-19 | Not done | Needs the real production host names. |
| F-20 | Done (opt-in) | `DataProtection:KeyPath` persists keys (DPAPI-protected on Windows). Unset keeps the default. |
| F-21 | Done | Admin-created partner logins must change password at first sign-in; creation and login-email changes are logged. |
| F-22 | Partly | Hasher tests added (48 tests). Role/ownership integration tests still missing. |
