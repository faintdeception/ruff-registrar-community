# Bulk member import (name + email CSV → Keycloak + AccountHolder)

Status: implemented (backend + frontend + E2E tests)
Owner: core (ruff-registrar-community)
Related repo note: `/memories/repo/keycloak-security.md`, `/memories/repo/validation.md`

## Problem

Hosts migrating from an older system want to bulk-upload a list of members
(name + email) and have them created as real accounts, without triggering
any assumption of an email service being configured for the host.

## Requirements

1. CSV upload of `firstName,lastName,email` (header row), admin-only.
2. For each row: create a Keycloak user with a temporary password and
   `UPDATE_PASSWORD` required action (must-change-password-on-first-login).
   No `VERIFY_EMAIL` action, no email sent — some hosts have no email
   service configured at all.
3. Rows become `AccountHolder` records (the generic "Member" role) using the
   existing single-create path — not a parallel implementation.
4. Admin-only feature end to end (UI + API).
5. The temporary/initial password used for invited users
   (`InitialInvitePassword`) must be admin-configurable per tenant, so it is
   not a "well known" shared default across every ruff-registrar host.
6. Hosts are **forced** to set a non-blank, sufficiently secure
   `InitialInvitePassword`. If it's blank, or fails the realm's password
   policy, password generation must throw a clear exception — never
   silently fall back to an insecure default.
7. This must hold even if Keycloak's policy can't be fetched (e.g. Keycloak
   admin API temporarily unreachable): a conservative baseline complexity
   check still applies, so something like `password` is always rejected.

## Design decisions (confirmed with product owner)

- Settings storage: **new `TenantSettings` table**, one row per tenant,
  rather than growing the `Tenant` entity further. This is a clean home for
  `InitialInvitePassword` and future admin-configurable settings, avoiding
  continued pollution of `Tenant`.
- Scope: CSV rows become `AccountHolder` (Member role) only — no Educator
  import in this pass.
- CSV format: simple `firstName,lastName,email` with header row.

## Data model

New `TenantSettings` entity (`core/src/StudentRegistrar.Models/TenantSettings.cs`):

- `Id` (Guid)
- `TenantId` (Guid, FK → Tenant, unique — 1:1)
- `InitialInvitePasswordEncrypted` (string?, protected via Data Protection,
  never returned in any read DTO)
- audit columns (`CreatedAtUtc`, `UpdatedAtUtc`) for consistency with other
  entities

New EF Core migration adds the table. Existing `Tenant` is untouched.

## Password policy enforcement

- `IKeycloakService.GetRealmPasswordPolicyAsync()` — calls
  `GET /admin/realms/{realm}` and returns the raw `passwordPolicy` string
  (Keycloak's `and`-joined token format, e.g.
  `length(12) and upperCase(1) and specialChars(1)`).
- New `IPasswordPolicyValidator` (single implementation, reused by both the
  settings-save path and the bulk-import path — see "duplication" below):
  - Parses the Keycloak policy string into rules and validates a candidate
    password against them.
  - If the policy can't be fetched (exception/timeout/403), falls back to a
    conservative baseline (length ≥ 12, upper/lower/digit/special) instead
    of skipping validation.
  - Returns a structured result (pass/fail + list of failed rule
    descriptions) so callers can build a clear error message.
- New `InsecureInitialInvitePasswordException : InvalidOperationException`
  — thrown (not logged-and-swallowed) whenever:
  - an admin tries to save a blank/failing `InitialInvitePassword`, or
  - the bulk-import path is about to use a stored value that fails
    validation (defense in depth against direct DB edits / stale data from
    an older version).

## Bulk import flow

- `POST /api/accountholders/bulk-import` (multipart CSV upload),
  `[Authorize(Roles = "Administrator")]`.
- Parses rows, validates `InitialInvitePassword` once up front (fail fast
  before touching Keycloak for row 1).
- For each row, reuses the **existing** `AccountHolderService`/
  `KeycloakService.CreateUserAsync` single-create path with
  `RequirePasswordChange = true`, `RequireEmailVerification = false`.
- Returns a per-row result report (created / skipped-duplicate / failed +
  reason) instead of all-or-nothing, so admins can fix and re-upload just
  the bad rows.

## Admin UI

- Settings page gets an `Initial invite password` field (masked input,
  save-time validation against the policy validator, clear inline error on
  failure).
- Members/admin area gets a "Bulk import" action: file picker → upload →
  per-row result table.

## Duplication / deslop watch-out

- Reuse the existing single AccountHolder-create path per CSV row instead
  of writing new Keycloak-call logic for bulk import.
- Password-policy parsing/validation logic lives in exactly one place
  (`IPasswordPolicyValidator`) and is called from both the settings-save
  check and the bulk-import defensive check.
- Check the deslop extension's duplication report during implementation and
  refactor immediately if new code pushes clone percentage up, rather than
  deferring cleanup.

## Testing

- Unit: password-policy parser against representative Keycloak policy
  strings; baseline-fallback rejection of weak passwords; exception thrown
  for blank/insecure settings.
- API: bulk-import endpoint — 403 for non-admin, partial-failure reporting,
  no `VERIFY_EMAIL`/email side effects, `UPDATE_PASSWORD` required action
  set on created users.
- Run via existing core lane (`core/run-tests.ps1` / `core/run-tests.sh`)
  before calling this done.

## Rollout

- New EF migration for `TenantSettings` (backward compatible — no existing
  data affected).
- Existing hosts must set `InitialInvitePassword` in Admin Settings before
  bulk import is usable (settings-save validation makes this obvious; the
  defensive runtime check makes it non-bypassable).

## Implementation status (2026-09-05)

Backend done:
- `TenantSettings` model, EF migration (`AddTenantSettings`), `DbContext`
  wiring (tenant query filter + timestamp handling).
- `IKeycloakService.GetRealmPasswordPolicyAsync()`.
- `IPasswordPolicyValidator` / `PasswordPolicyValidator` (Keycloak-policy
  parsing + conservative baseline fallback).
- `InsecureInitialInvitePasswordException`.
- `ITenantSettingsService` / `TenantSettingsService` (encrypts the password
  via Data Protection, validates on save and again on read/use) +
  `GET /api/tenant-settings` and `PUT /api/tenant-settings/initial-invite-password`
  (`TenantSettingsController`, admin-only).
- `KeycloakService.CreateUserAsync` now honors a caller-supplied password
  (used for the shared initial invite password) instead of always
  generating a random one.
- `AccountHoldersController.CreateAccountHolderWithKeycloakUserAsync` —
  extracted single-create helper reused by both the existing single-create
  endpoint and the new `POST /api/accountholders/bulk-import` endpoint
  (admin-only, CSV `firstName,lastName,email`, per-row result reporting,
  `RequireEmailVerification = false`).
- `MemberImportCsvParser` — small hand-rolled CSV parser (no new
  dependency, kept scoped to this one format).
- Unit tests: `PasswordPolicyValidatorTests`, `MemberImportCsvParserTests`,
  `TenantSettingsServiceTests`; controller tests:
  `TenantSettingsControllerTests`, new bulk-import cases in
  `AccountHoldersControllerTests`; existing `AccountHoldersControllerTests`
  updated for the new constructor dependency. Full
  `core/tests/StudentRegistrar.Api.Tests` suite passing (348/348).

Frontend done:
- `core/frontend/src/lib/api-client.ts` — added `postForm()` and FormData
  support (skips the forced JSON `Content-Type` so the browser sets the
  multipart boundary).
- `core/frontend/src/pages/settings/system.tsx` — new "Initial Invite
  Password" card: fetches `/api/tenant-settings`, shows configured/missing
  status, and saves via `PUT /api/tenant-settings/initial-invite-password`
  with inline validation error/reason display.
- `core/frontend/src/pages/members.tsx` — new "Bulk Import" button/panel:
  CSV file picker, uploads via `POST /api/accountholders/bulk-import`, and
  renders a per-row result table (created/failed + reason).
- Verified with `npx tsc --noEmit` (no errors).

Follow-up (not yet done):
- `PasswordPolicyValidationResult.UsedFallbackBaseline` is intentionally
  log-only for now (not surfaced in the Admin Settings UI) \u2014 confirmed
  as acceptable scope for this pass.

E2E coverage added (2026-09-05):
- `core/tests/StudentRegistrar.E2E.Tests/Tests/RoleBasedTests/InitialInvitePasswordSettingsTests.cs`
  \u2014 admin configures a compliant password successfully; admin gets a
  clear, actionable error (not a silent accept) for an insecure one like
  `password`.
- `core/tests/StudentRegistrar.E2E.Tests/Tests/RoleBasedTests/BulkImportMembersTests.cs`
  \u2014 end-to-end CSV upload creates a member and it shows up in the
  members list; a mixed-validity CSV reports partial success per row
  instead of failing the whole batch.
- Added a shared `EnsureInitialInvitePasswordConfigured()` helper on
  `BaseRoleNavigationTest` so both new test classes (and any future ones)
  stay order-independent without duplicating the settings-page setup flow.
- Added `data-testid`/`id` hooks needed for these tests to
  `system.tsx`/`members.tsx` (invite-password status spans, success
  banner, bulk-import submit button).
- Compile-checked via `dotnet build core/tests/StudentRegistrar.E2E.Tests`;
  not yet run against a live stack (requires the full local Aspire/Keycloak
  environment per `core/scripts/testing/run-e2e-tests.ps1`).

