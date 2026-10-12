# Design

## Context

See proposal.md for the motivation. The current state that shapes the approach:

- **Roles.**
  - `Role` is seeded with `Owner` (id 1) and `Member` (id 2). `UserRole` holds global roles and `ProjectUserRole` holds project roles.
  - `RoleService` manages global roles with no caller and no checks.
  - `ProjectAccess` and `RoleNames` (in `Application/Common`) already centralize per-project rules. Services take the caller's id explicitly, and endpoints read it with `principal.GetUserId()`. `ServiceStatus.Forbidden` maps to 403.
- **Tokens.** The JWT carries only the user id and email, and is valid for 1 day.
- **Persistence and tests.**
  - The backend uses PostgreSQL through EF Core and applies migrations at startup.
  - Tests run services against in-memory SQLite (`SqliteTestDb`).
  - `DependencyRuleTests` keep EF providers and infrastructure packages out of Application.
- **Deployment.** One `api` container in `docker-compose.yml` serves the API. Production runs on Proxmox.
- **Frontend.**
  - Angular with signals. `AccountService` knows only whether a token exists. There are no route guards yet.
  - `Menu` renders a flat `links` array. `/users` lists users and can register a new one through `/api/account/register`.
  - `GET /api/account/users` is also used by project owners to search for people to add, so it must stay available to non-admins.

## Goals / Non-Goals

**Goals:**
- **Testable ingestion.** The email-to-task logic lives in the Application layer behind an `IMailboxClient` interface, so it can be tested with a fake mailbox and in-memory SQLite, without a real IMAP server.
- **Immediate revocation.** Admin status is checked from the database on every request, so revoking it takes effect right away.
- **No new containers or volumes.** The feature fits the existing compose stack. Files go to Cloudinary, not to the container's disk.

**Non-Goals:**
- OAuth2 IMAP login, for example for Gmail and Microsoft 365. Only username and app password are supported.
- Several mailboxes, or several target projects per mailbox.
- Replies, threading, and mapping senders to assignees.
- Uploading or deleting attachments by hand, private or signed attachment links, and removing files from Cloudinary when a task or project goes away.
- Running more than one `api` replica. It stays correct, but both replicas poll; see Risks.
- Letting admins see every project in the Projects page. Admins see all projects only in the project picker of the mail settings.

## Decisions

### 1. `Admin` is a seeded global role, checked from the database per request
- Seed `Role { Id = 3, Name = "Admin" }` with `HasData`, and add `RoleNames.Admin`.
- Add a helper `AdminAccess.IsAdminAsync(context, userId)` next to `ProjectAccess`. It checks for a `UserRole` row with the `Admin` role.
- Admin-only service methods take `callerId` and return `Forbidden` when the caller is not an admin. This is the same pattern as the owner checks.
- *Alternative: put a role claim in the JWT and use an ASP.NET policy.* Rejected because a revoked admin would keep access for up to 24 hours, and the `admin-access` spec requires the check at request time.

### 2. First admin from `AdminEmail`, applied at startup and at registration
- An `AdminOptions { Email }` class in Application is bound from the configuration key `AdminEmail`. Compose maps the `.env` value `ADMIN_EMAIL` to it.
- **At startup.** After `ApplyMigrations()`, `app.Services.EnsureConfiguredAdminAsync()` gives `Admin` to the user with that email, if they exist and do not already hold it. Emails are compared case-insensitively, by lower-casing both sides in the query.
- **At registration.** `AccountService.RegisterAsync` adds the `UserRole` in the same `SaveChangesAsync` when the email matches.
- An empty or missing setting does nothing.
- *Alternative: the first registered user becomes admin.* Rejected by the user, because someone else could register first.

### 3. Global role endpoints become admin-only, and the last admin is protected
- `IRoleService.GetUserRolesAsync`, `AssignToUserAsync` and `RemoveFromUserAsync` gain `callerId`, and answer `Forbidden` for non-admins.
- `RemoveFromUserAsync` refuses to remove `Admin` when no other user holds it, and returns `Invalid("At least one admin must remain.")`.
- `GET /api/roles` stays open, because the project UI uses it.
- `ProjectMembershipService.AddRoleAsync` returns `Invalid("Admin is not a project role.")` for the `Admin` role. The project members UI lists only `Owner` and `Member`.

### 4. Admin endpoints under `/api/settings`
`SettingsEndpoints` maps the following routes. All of them require a valid token, and their services check admin status.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/settings/users` | Users with an `isAdmin` flag, for the Users page. `/api/account/users` stays as it is for member search. |
| GET | `/api/settings/projects` | Id and name of every non-deleted project, for the target picker. |
| GET / PUT | `/api/settings/mail` | Read and save the mailbox settings. |
| POST | `/api/settings/mail/test` | Test a connection with the submitted settings. |

Granting and revoking `Admin` reuse `POST /api/account/users/{id}/roles` and `DELETE /api/account/users/{id}/roles/3`.

`AccountDto` gains `IsAdmin`, which `GetMeAsync` fills in.

### 5. Storage: a single-row `MailSettings` table and an `InboundEmails` table
- **`MailSettings`** (always `Id = 1`) holds:
  - `Enabled`, `Host`, `Port` and `Security`, an enum of `Auto`, `SslOnConnect`, `StartTls` or `None`, mapped to MailKit's `SecureSocketOptions`;
  - `Username`, `ProtectedPassword`, `Folder`, `AllowedSenders` (a `List<string>`, which EF stores as a primitive collection: `text[]` on Postgres, JSON on SQLite), `ProjectId` and `PollIntervalSeconds`;
  - the cursor `MailboxKey`, `UidValidity` and `LastUid`;
  - the status `LastPollAt`, `LastPollError` and `LastPollCreated`.
- **`InboundEmails`** holds `Id`, `MailboxKey`, `MessageKey`, `From`, `Subject`, `ReceivedAt`, `ProcessedAt`, `Outcome` (`Created` or `Skipped`) and `TaskItemId`.
  - There is a unique index on (`MailboxKey`, `MessageKey`).
  - `MessageKey` is the `Message-ID` header, or `uid:{UidValidity}:{Uid}` when that header is missing.
- *Alternative: key-value rows in a generic `Settings` table.* Rejected. Typed columns give validation, foreign keys and simpler queries, and there is only one settings group.

### 6. The password is encrypted with ASP.NET Data Protection, with keys stored in the database
- An Application interface `ISecretProtector { Protect, Unprotect }` is implemented in Infrastructure with `IDataProtector`, purpose `"MailSettings.Password"`.
- Keys are persisted with `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`: `AppDb` implements `IDataProtectionKeyContext`, which adds a `DataProtectionKeys` table. Keys therefore survive image rebuilds without a volume.
- **Saving.** `PUT` with an empty or absent password keeps `ProtectedPassword`.
- **Reading.** `GET` returns `hasPassword` and never the password.
- *Alternative: the password only in `.env`.* Rejected, because the user wants to configure it from the UI.
- *Alternative: a separate secret in `.env` as the key.* Possible later; see Risks.

### 7. IMAP access behind `IMailboxClient`, implemented with MailKit
- The Application layer defines:
  - `IMailboxClient.TestAsync(MailboxConnection)`;
  - `IMailboxClient.FetchNewAsync(MailboxConnection, uidValidity, lastUid)`, which returns the current `UidValidity`, the highest UID and the messages above `lastUid`, each with UID, `Message-ID`, From, Subject, Date, text body and attachments.
  - Each attachment carries its file name, content type and size. Its content is included only when the file is within the limits; otherwise it carries a reason (too large, or over the count).
- The Infrastructure implementation `MailKitMailboxClient`:
  - connects with a 30-second timeout and opens the folder read-only, so it never sets flags or deletes messages;
  - fetches envelopes and bodies by UID range, and fetches the highest UID for the baseline;
  - converts HTML to text with MimeKit's `HtmlToText`;
  - reads the BODYSTRUCTURE first, then downloads only attachment parts (parts marked as attachments, and inline parts with a file name) that are at most 10 MB, and at most 10 of them. Larger or extra parts are reported with their size and reason, but not downloaded.
- The new MailKit dependency is added only to Infrastructure.
- *Alternative: search only `UNSEEN` messages.* Rejected, because another mail client reading a message would hide it. Tracking UIDs, plus de-duplication by `MessageKey`, meets the "at most once, regardless of read state" requirement.

### 8. Ingestion runs as one Application use case, driven by a hosted worker
`EmailIngestionService.PollOnceAsync()` runs one poll:
1. **Load and check.** Load the settings. If the service is disabled, stop. If the target project is missing or deleted, record the error, leave the cursor where it is, and stop.
2. **Compute the mailbox key.** `MailboxKey` is a hash of host, username and folder. When it differs from the stored key, or the cursor is empty, establish a baseline: store the current `UidValidity` and highest UID, create no tasks, and record success.
3. **Fetch.** Fetch the messages above `LastUid`. If `UidValidity` changed on the server, establish a new baseline and import nothing, so the same mail is not imported twice.
4. **Process each message in UID order:**
   - if `InboundEmails` already has its `MessageKey`, advance the cursor;
   - otherwise, if the sender is allowed:
     - upload each downloaded attachment with `IFileStorage` (decision 10), unless storage is not configured;
     - collect the files that were not stored, with the reason: too large, over the count, upload failed, or storage not configured;
     - create the task (fields as below), one `TaskAttachment` per stored file, and an `InboundEmail` row with outcome `Created`;
   - otherwise, add an `InboundEmail` row with outcome `Skipped`.

   Uploads happen before the save. The task, its attachments, the row and the advanced `LastUid` are then saved in one `SaveChangesAsync`. A unique-index violation, for example from two replicas processing the same message, is treated as "already processed".
5. **Record the status.** Store `LastPollAt`, the number of tasks created, and the error, cleared on success.

Exceptions from the client are caught, stored in `LastPollError` (the message only, never the credentials) and logged.

`MailPollingWorker : BackgroundService` (Infrastructure) loops: create a DI scope, call `PollOnceAsync`, then wait. It waits for the configured interval when the service is enabled, and for 30 seconds when it is disabled, so enabling or disabling takes effect within one interval. The worker never throws out of its loop.

Task fields are set as follows:
- the title is the subject, trimmed to 200 characters, or `(no subject)`;
- the description starts with `From: …` and `Received: …` followed by the body, which is truncated to 10,000 characters;
- when some files were not stored, the description ends with a `Not attached:` list giving each file's name and reason;
- the status is `Created` and the priority is `Medium`;
- `DueDate = null` and `AssignedId = null`.

### 9. Frontend: an admin flag, a guard and the Settings area
- **Admin flag.** `AccountService` loads `me` after sign-in and at app start when a token exists, and exposes `isAdmin = computed(...)`.
- **Guard.** `adminGuard` (`CanActivateFn`):
  - waits for `me`;
  - sends a signed-out person to `/login?returnUrl=…`;
  - sends a non-admin to `/home`.
- **Routes.**

  | Route | Shows |
  | --- | --- |
  | `settings` | Redirects to `settings/users`. |
  | `settings/users` | The existing `Users` component, moved to `settings/users/`. It loads `/api/settings/users` and gets an Admin toggle per row. |
  | `settings/mail` | The new `MailSettings` page: a PrimeNG form, a sender chips input, a project select fed by `/api/settings/projects`, "Test connection", and the status panel. |

  The `/users` route redirects to `/settings/users`.
- **Menu.** `Menu` drops the top-level Users link. When `isAdmin()` is true, it renders a Settings group with the two children.
- **Project members UI.** The role options are filtered to `Owner` and `Member`.
- **Task details.** When `task.attachments` is not empty, `TaskDetails` shows an Attachments section with name and size. Each entry is a link with `target="_blank"` and `rel="noopener"`.
- **Mail settings page.** It shows whether attachment storage is configured, from the `attachmentStorageConfigured` field of `MailSettingsDto`.

### 10. File storage in Cloudinary, behind `IFileStorage`
- **Interface.** The Application layer defines `IFileStorage`:
  - `bool IsConfigured`;
  - `UploadAsync(Stream content, string fileName, string contentType, string folder)`, which returns a `StoredFile(PublicId, Url, Bytes)`.
- **Implementation.** `CloudinaryFileStorage` in Infrastructure uses the `CloudinaryDotNet` package:
  - `AutoUploadParams`, so images, PDFs and other files are all accepted;
  - delivery type `upload`, which gives public, permanent `secure_url` links, as the user chose;
  - folder `pjm/tasks`, and a unique public id that keeps the original file name for readability.
- **Settings.**
  - `CloudinaryOptions { CloudName, ApiKey, ApiSecret }` is bound from the configuration section `CloudinarySettings`, as in the snippet the user provided.
  - Compose maps it from `.env`: `CloudinarySettings__CloudName: ${CLOUDINARY_CLOUD_NAME:-}`, `CloudinarySettings__ApiKey: ${CLOUDINARY_API_KEY:-}` and `CloudinarySettings__ApiSecret: ${CLOUDINARY_API_SECRET:-}`.
  - `IsConfigured` is true only when all three values are set.
  - The secret appears only in `.env`, never in `appsettings*.json`, the repository, the database, responses or logs.
- **Data.** A new `TaskAttachment` entity has `Id`, `TaskItemId` (cascade on task delete), `FileName`, `ContentType`, `SizeBytes`, `Url`, `PublicId` and `CreatedAt`.
- **API.** `TaskItemDto.Attachments` is a list of `{ id, fileName, contentType, sizeBytes, url }`, filled in by the existing `TaskService.Query` projection. It is therefore filtered by project membership like the task itself.
- *Alternative: store files in Postgres or on a Docker volume.* Rejected. The user asked for Cloudinary, and that keeps the database small and the containers stateless.
- *Alternative: authenticated delivery with signed URLs.* Rejected by the user in favour of permanent public links. It can be switched later by changing the delivery type and generating URLs on read.

## Risks / Trade-offs

- **[Keys in the same database as the ciphertext]** Someone with a full database dump can decrypt the mailbox password. → Accepted. Encryption still keeps the password out of API responses, logs and casual queries. If needed later, protect the key ring with a certificate or `.env` secret through `ProtectKeysWith…`.
- **[Baseline skips mail]** Mail that arrives while the service is disabled, or before a mailbox change, is never imported. A `UIDVALIDITY` reset also skips the messages pending at that moment. → This is what the spec asks for ("only new mail"), and it is documented on the settings page.
- **[Several replicas poll twice]** If the API is scaled out, each replica polls. → The unique index on (`MailboxKey`, `MessageKey`) keeps tasks unique. Only extra IMAP connections result. A leader lock can come later if scaling becomes real.
- **[Providers that require OAuth]** Gmail and Microsoft 365 may refuse plain login. → Use an app password or a provider that allows it. OAuth is a non-goal and the connection test surfaces the error.
- **[Large or hostile emails]** Huge bodies or crafted HTML. → Fetch only the body parts, truncate to 10,000 characters, and store plain text only. Download only attachments within 10 MB each, and at most 10 per email.
- **[Public attachment links]** Anyone who obtains a link, for example a forwarded URL, can open the file forever. → This is the chosen trade-off. Public ids include a random part, so links cannot be guessed, and the app shows them only to project members. Signed links remain an option (decision 10).
- **[Orphaned or duplicate files]** If the save fails after uploads, or a poll is interrupted mid-message, the files stay in Cloudinary. A retried message uploads its files again, and only one task is ever created. → Accepted, since the space is small. A later cleanup could compare `TaskAttachments.PublicId` with the files in the Cloudinary folder.
- **[Cloudinary quota and file types]** The free plan limits storage, bandwidth and file size, and uploads can be rejected. → A rejection becomes a "Not attached" entry with the reason. The 10 MB limit matches the free-plan cap for raw and image files.
- **[Malicious attachments]** Executables or malware are served from Cloudinary to whoever opens them. → They are shown as plain download links, never rendered inline by the app. Virus scanning is out of scope.
- **[Exposed Cloudinary secret]** The API secret was shared in plain text while this change was being planned. → Regenerate it in the Cloudinary console before going live, and put the new value only in `.env`.
- **[Sender spoofing]** The From header can be forged, so an allowlist is not authentication. → Accepted for this feature and documented. DKIM/SPF checking is out of scope.
- **[Stale admin flag in the UI]** The menu reflects `me` from the last load. → The server enforces every rule, and the guard and menu refresh on sign-in and app start.

## Migration Plan

1. **Migration `AdminAndMailIngestion`:** seeds `Admin` (id 3) and creates `MailSettings`, `InboundEmails`, `DataProtectionKeys` and `TaskAttachments`. No existing rows change.
2. **Configuration:**
   - add `ADMIN_EMAIL=`, `CLOUDINARY_CLOUD_NAME=`, `CLOUDINARY_API_KEY=` and `CLOUDINARY_API_SECRET=` to `.env.example`, with empty values;
   - map them on the `api` service in `docker-compose.yml`, as `AdminEmail` and `CloudinarySettings__*`;
   - put the real values only in `.env`.
3. **Deploy:** set `ADMIN_EMAIL` in `.env` on the server, run `docker compose up -d --build`, sign in with that account, open Settings → Email → Project, test, enable.
4. **Rollback:** redeploy the previous images. The new tables are ignored by old code. To fully remove, run `dotnet ef database update <previous migration>`, which drops the tables and the `Admin` role.
