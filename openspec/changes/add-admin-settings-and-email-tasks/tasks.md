# Tasks

## 1. Data model and migration

- [x] 1.1 Add `RoleNames.Admin` and seed `Role { Id = 3, Name = "Admin" }` in `AppDb`.
  - Add `MailSettings` and `InboundEmail` entities to Domain (design decision 5): the `MailSecurity` enum, `AllowedSenders` as `List<string>`, and a nullable `ProjectId`.
  - Add the `TaskAttachment` entity (design decision 10), with cascade on task delete.
  - Add their `DbSet`s to `IAppDbContext` and `AppDb`, with a unique index on (`MailboxKey`, `MessageKey`).

  Verify with `dotnet build backend/PJMSolution.slnx`.
- [x] 1.2 Add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` to Infrastructure and make `AppDb` implement `IDataProtectionKeyContext` (design decision 6). Generate the migration `AdminAndMailIngestion`. Verify that its `Up` inserts role 3 and creates `MailSettings`, `InboundEmails`, `DataProtectionKeys` and `TaskAttachments` and changes nothing else, and that `docker compose up -d --build` applies it on the existing database.

## 2. Admin role, bootstrap and admin-only global roles

- [x] 2.1 Add `AdminAccess.IsAdminAsync(userId)` in `Application/Common`, and `AdminOptions { Email }` bound from `AdminEmail` (design decisions 1 and 2). Implement `EnsureConfiguredAdminAsync`, called after `ApplyMigrations()` in `Program.cs`, and grant `Admin` inside `AccountService.RegisterAsync` when the email matches, ignoring case. Add `AdminBootstrapTests` covering:
  - an existing user is promoted at startup;
  - a later registration is promoted;
  - case is ignored;
  - an empty setting grants nobody;
  - a second run does not duplicate the role.

  Verify with `dotnet test backend/PJMSolution.slnx`.
- [x] 2.2 Give `IRoleService` / `RoleService` a `callerId` on `GetUserRolesAsync`, `AssignToUserAsync` and `RemoveFromUserAsync`:
  - answer `Forbidden` for non-admins;
  - answer `Invalid("At least one admin must remain.")` when removing the last `Admin`;
  - in `RoleEndpoints`, pass `principal.GetUserId()` and answer `401` when it is missing.

  Add `RoleServiceTests` for the `role-management` and `admin-access` scenarios: admin assigns and removes, non-admin is `Forbidden`, duplicate is `Conflict`, an unknown user or role is `NotFound`, and the last-admin and two-admin cases. Verify with `dotnet test`.
- [x] 2.3 Add `IsAdmin` to `AccountDto`, filled in by `GetMeAsync` from the database. Make `ProjectMembershipService.AddRoleAsync` reject `Admin` with `Invalid("Admin is not a project role.")`. Extend `AccountServiceTests` (`me` reflects a role revoked after sign-in) and `ProjectMembershipServiceTests` (`Admin` rejected, nothing recorded; an admin with no project role does not see the project). Verify with `dotnet test`.
- [x] 2.4 Extend `DependencyRuleTests` so Application must not reference `MailKit`, `MimeKit`, `CloudinaryDotNet` or `Microsoft.AspNetCore.DataProtection`. Verify with `dotnet test`.

## 3. Mail settings API

- [x] 3.1 Add `ISecretProtector` in Application and its Data Protection implementation in Infrastructure, registered with `AddDataProtection().PersistKeysToDbContext<AppDb>()`. Verify with an Infrastructure-level test that `Protect` and then `Unprotect` round-trip, and that the protected value differs from the input.
- [x] 3.2 Add `MailSettingsDto` (no password, plus `HasPassword`, the last-poll status fields, and `AttachmentStorageConfigured` from `IFileStorage.IsConfigured`) and `SaveMailSettingsRequest` (optional `Password`). Add `IMailSettingsService` / `MailSettingsService` with:
  - `GetAsync(callerId)`;
  - `SaveAsync(callerId, request)`: validates per the `email-task-ingestion` spec only when enabled, keeps the stored password when none is sent, and resets the cursor when host, username or folder change;
  - `TestAsync(callerId, request)`: uses `IMailboxClient.TestAsync`, falling back to the stored password;
  - `GetUsersAsync(callerId)` and `GetProjectsAsync(callerId)`, for the settings pages.

  Add `MailSettingsServiceTests` with a fake `IMailboxClient` and a fake protector, covering:
  - a non-admin is `Forbidden`;
  - the password is never in the DTO and is kept when omitted;
  - each validation error, including a deleted project and the 30–3600 interval bounds;
  - a disabled draft saves;
  - a test failure leaves the settings unchanged.

  Verify with `dotnet test`.
- [x] 3.3 Add `SettingsEndpoints` (`/api/settings/users`, `/api/settings/projects`, `GET/PUT /api/settings/mail`, `POST /api/settings/mail/test`), using `principal.GetUserId()`, `401` when the id is missing and `ToHttpResult()`, with `Produces<…>` metadata. Map them in `Program.cs`. Verify that `dotnet build` succeeds and that `/openapi/v1.json` in Development lists the routes with `MailSettingsDto`.

## 4. IMAP client and email-to-task ingestion

- [x] 4.1 Add MailKit to Infrastructure and implement `MailKitMailboxClient : IMailboxClient` (design decision 7):
  - 30-second timeout, `SecureSocketOptions` from `MailSecurity`;
  - `Inspect`/read-only folder open;
  - fetch by UID range, with `Message-ID`, From, Subject, Date and a text body built with `HtmlToText` when there is no plain-text part;
  - read the BODYSTRUCTURE and download only attachment parts, meaning parts marked as attachments and inline parts with a file name, of at most 10 MB each and at most 10 per message. Report the rest with their size and reason.

  Verify that `dotnet build` succeeds. The behavior is covered by the e2e check in 6.1.
- [x] 4.2 Implement `EmailIngestionService.PollOnceAsync()` (design decision 8):
  - baseline on first enable and on a mailbox or `UidValidity` change;
  - allowlist match ignoring case;
  - task mapping: subject trimmed to 200 characters or `(no subject)`, From/Received header lines, body capped at 10,000 characters, `Medium`, no due date and no assignee;
  - `Created` / `Skipped` rows saved together with the task and the cursor;
  - unique-key violations treated as already processed;
  - the cursor is held while the project is missing;
  - the last-poll status is recorded, without credentials;
  - attachments are uploaded through `IFileStorage` before the save, stored files become `TaskAttachment` rows, and the rest go into the `Not attached:` list.

  Add `EmailIngestionServiceTests` with a fake mailbox covering every `email-task-ingestion` polling scenario:
  - allowed sender, case, no subject, HTML-only, unknown sender skipped;
  - a second poll and a "restart" (new service instance) create no duplicates;
  - the full inbox at enable time is not imported;
  - the project is deleted and then fixed;
  - a client exception is recorded and the next poll recovers;
  - disabled means no client call;
  - with a fake `IFileStorage`: an attachment is stored and linked; a too-large file and an 11th file are listed as not attached; storage not configured lists the files without calling upload; an upload exception lists the file, the task is still created, and the next poll creates no duplicate.

  Verify with `dotnet test`.
- [x] 4.3 Add `MailPollingWorker : BackgroundService` in Infrastructure: a scope per poll, a delay equal to the interval when enabled and 30 seconds when disabled, and a loop that never throws, with errors logged. Register it with `AddHostedService`. Verify that the API starts and that the logs show one poll attempt per interval with the service disabled and no IMAP connection.

- [x] 4.4 Add `IFileStorage` in Application and `CloudinaryFileStorage` in Infrastructure, with the `CloudinaryDotNet` package (design decision 10):
  - `CloudinaryOptions` bound from `CloudinarySettings`;
  - `IsConfigured` only when CloudName, ApiKey and ApiSecret are all set;
  - `AutoUploadParams` with delivery type `upload`, folder `pjm/tasks`, a unique public id, and the result's `secure_url`;
  - the secret is never logged.

  Verify with a test that missing or partial options give `IsConfigured == false`. Real uploads are covered in 6.1.
- [x] 4.5 Add `TaskAttachmentDto` and `TaskItemDto.Attachments`, filled in by `TaskService.Query`. Extend `TaskServiceTests`:
  - a member reading a task gets its attachments;
  - a task without attachments has an empty list;
  - a non-member gets `null`, so no attachment data.

  Verify with `dotnet test`.

## 5. Web app: admin flag, Settings area, pages

- [x] 5.1 Add an `isAdmin` field to the `Account` model. In `AccountService`, keep a `me` signal (loaded after sign-in and at start when a token exists, and cleared on logout) with `isAdmin` computed from it. Add an `adminGuard` (`CanActivateFn`) that sends signed-out people to `/login?returnUrl=…` and non-admins to `/home`. Verify with spec tests for the guard: admin allowed, non-admin redirected home, signed-out redirected to login with `returnUrl`. Run `npx ng test --watch=false --build-target pjm:build:dev` in `frontend/`.
- [x] 5.2 Add the routes `settings` (redirecting to `settings/users`), `settings/users` and `settings/mail`, guarded by `adminGuard`, and redirect `/users` to `/settings/users`. In `Menu`, remove the Users link and show a Settings group with "Users" and "Email → Project" only when `isAdmin()`. Verify with `menu.spec.ts` cases (admin sees Settings and both children, non-admin sees neither) and with `npm run build`.
- [x] 5.3 Move the `users/` component to `settings/users/`. Load `/api/settings/users`, keep "Add user", and add a per-row Admin toggle that calls the role endpoints with role id 3. Show the server message on failure, for example the last admin, and then reload. Verify with spec cases for grant, revoke, last-admin message and reload. Run the tests.
- [x] 5.4 Add the `settings/mail` page:
  - a form for enabled, host, port, security, username, password (empty, with a "password set" hint), folder, allowed senders (a chips input with email validation), project select from `/api/settings/projects`, and interval;
  - Save, showing validation messages and keeping the input;
  - Test connection, showing its result;
  - a last-poll status panel;
  - a notice when attachment storage is not configured;
  - a note that only mail arriving after enabling is imported.

  Verify with spec cases: load fills the form without a password, the storage notice is shown when `attachmentStorageConfigured` is false, saving sends no password when the field is empty, a 400 message is shown and the input kept, test success and failure, and the status error is shown. Run the tests.
- [x] 5.5 In `ProjectMembers`, limit the role options and the add-role select to `Owner` and `Member`. Verify with a `project-members.spec.ts` case where `/roles` returns `Admin` and it is not offered. Run the tests.
- [x] 5.6 Add `attachments` to the `TaskItem` model, and an Attachments section in `TaskDetails`:
  - shown only when the list is not empty;
  - each entry shows the name and a readable size;
  - each entry is a link with `target="_blank"` and `rel="noopener"`.

  Verify with `task-details.spec.ts` cases: a task with one attachment renders a link to its URL with name and size, and a task without attachments renders no section. Run the tests.

## 6. Configuration and end-to-end check

- [ ] 6.1 Configure the environment:
  - add `ADMIN_EMAIL=`, `CLOUDINARY_CLOUD_NAME=`, `CLOUDINARY_API_KEY=` and `CLOUDINARY_API_SECRET=` to `.env.example`, with empty values;
  - map them on the `api` service in `docker-compose.yml`, as `AdminEmail` and `CloudinarySettings__CloudName`, `__ApiKey` and `__ApiSecret`;
  - set the real values only in the local `.env`, which is not committed. Use a regenerated Cloudinary secret. Run `docker compose up -d --build` and check:
  - the configured user is `Admin` (`GET /api/account/me` has `isAdmin: true`) and sees Settings, while another user does not and gets `403` on `/api/settings/mail`;
  - a non-admin gets `403` granting themselves role 3, and removing the last admin returns `400`;
  - with a test mailbox, for example a GreenMail container started only for this check or a real app-password account, Test connection succeeds;
  - after enabling, one email from an allowed sender becomes one task on the chosen project, visible to its members, and an email from another sender creates nothing;
  - an email from an allowed sender with a small PDF and a file over 10 MB creates a task whose details link to the PDF on Cloudinary (the link opens without signing in), and whose description lists the large file as not attached;
  - the mail settings page shows attachment storage as configured;
  - after `docker compose restart api`, no duplicate is created.

  Record the outcome in this change, and remove any test data, including the uploaded test files in the Cloudinary `pjm/tasks` folder.

  **Outcome (2026-10-12, `docker compose` stack plus a temporary GreenMail container, checked with a scripted run against `http://localhost:8080/api`):** 19 of 20 checks passed.
  - `user@email.com` (`ADMIN_EMAIL`) was made Admin at startup.
  - The e2e admin (granted in the database, since the owner's password is unknown) had `isAdmin: true`. The member had `isAdmin: false` and got `403` on `/api/settings/mail` and on granting themselves role 3.
  - Removing the last admin returned `400` with "At least one admin must remain." (with `user@email.com`'s role removed for the check and restored afterwards).
  - Test connection failed with "Authentication failed…" for a wrong password, and succeeded for the right one.
  - Saving returned `hasPassword` without the password, and `attachmentStorageConfigured: true`.
  - After the baseline poll, an email from the allowed sender became one task on the project, with the sender and body, and visible to the member. The `huge.bin` file (11 MB) was listed under "Not attached: (larger than 10 MB)". The email from another sender created nothing, and there was no duplicate after `docker compose restart api`.
  - `report.pdf` was uploaded to Cloudinary (`raw/upload/…/pjm/tasks/…`) and linked to the task.
  - **Failed:** opening the PDF link without signing in returned `401` (`x-cld-error: deny or ACL failure`). A `.txt` file uploaded the same way opened with `200`, so the cause is the Cloudinary account setting that blocks delivery of PDF and ZIP files (Settings → Security → "Allow delivery of PDF and ZIP files"), not the code. Re-run the PDF check once that setting is enabled.
  - Found and fixed during the check:
    - GreenMail answers MailKit's `BODY[n.MIME]` fetch incorrectly, so parts are now fetched with plain `BODY[n]` and decoded from the BODYSTRUCTURE.
    - Non-image files are uploaded as Cloudinary `raw`, so their bytes are kept and invalid PDFs are not rejected.
    - The upload error message is included in "Not attached".
    - Attachment content types are lower-cased.
  - Removed afterwards: the e2e users, project, task, mail settings and inbound-email rows; the uploaded files in Cloudinary under `pjm/` (0 left); and the GreenMail container.

## Workflow follow-up

- Run `/opsx:archive` after implementation and verification, so that `admin-access`, `email-task-ingestion`, `settings-ui` and `task-attachments` are created and `role-management` and `project-membership` are updated.
