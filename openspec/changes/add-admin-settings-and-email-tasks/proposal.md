# Proposal

## Why

Teams want work that arrives by email, such as client requests or alerts, to land on the board without anyone copying it over by hand. Configuring that mailbox is an administrative task that ordinary users must not be able to see or change, and the app has no notion of an administrator.

The current role setup also has a hole. The global user-role endpoints (`/api/account/users/{id}/roles`) are open to every signed-in user. Once a privileged role exists, anyone could grant it to themselves.

## What Changes

- **Admin role:**
  - Add a global `Admin` role to the role catalog.
  - The user whose email matches the configured `ADMIN_EMAIL` is made `Admin` at startup, and also at registration if they sign up later.
  - Admins can grant or revoke `Admin` for other users.
  - The system refuses to remove the last `Admin`.
- **BREAKING** Listing, granting and removing a user's global roles becomes admin-only. These endpoints were open to every signed-in user.
- **Project roles:** `Admin` cannot be assigned as a project role. Projects keep using `Owner` and `Member` only.
- **Current account:** the current-account response (`GET /api/account/me`) says whether the user is an admin, so the web app can show or hide admin features.
- **Inbound email settings,** stored on the server and editable only by admins:
  - IMAP host, port, TLS mode, username and password. The password is stored encrypted and is never returned by the API.
  - The folder to watch, which defaults to `INBOX`.
  - The list of allowed sender addresses.
  - The target project.
  - The poll interval.
  - An enabled switch.
  - A "test connection" action.
- **Email-to-task service:**
  - A background service polls the configured mailbox over IMAP.
  - Each new message from an allowed sender becomes an unassigned task in the configured project: the subject becomes the title, and the plain-text body becomes the description, together with the sender and the received date.
  - Messages from other senders are skipped.
  - Each message is processed at most once.
  - When the service is first enabled, it starts with mail that arrives from then on and does not import the existing mailbox.
  - The status of the last poll (time and error, if any) is shown in the settings.
- **File storage (Cloudinary) for email attachments:**
  - Attachments of emails from allowed senders are uploaded to Cloudinary and linked to the created task.
  - Project members see the attachments in the task details and can download them. The links are public and permanent.
  - Limits: 10 MB per file and 10 files per email. Files that exceed a limit, or whose upload fails, are listed in the task description as not attached, with the reason.
  - The Cloudinary credentials come only from configuration (`CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`). When they are missing, tasks are still created, without attachments.
- **Settings in the web app:**
  - A new **Settings** menu node, visible only to admins, with two sub-nodes:
    - **Users**: the existing users page, moved under Settings, which also lets an admin grant or revoke `Admin`.
    - **Email → Project**: the mailbox, sender allowlist and target project configuration, with connection test, last-poll status, and whether attachment storage is configured.
  - The old `/users` route redirects to the new page.
  - Non-admins who open a settings URL are sent back to the home page.

## Capabilities

### New Capabilities
- `admin-access`: the global `Admin` role. Covers the first admin set from configuration, admin-only management of global roles, protection of the last admin, and exposing admin status to the client.
- `email-task-ingestion`: admin-managed inbound mailbox settings, and the background conversion of allowed incoming emails into tasks on the configured project.
- `settings-ui`: the admin-only Settings area in the web app, with its Users and Email → Project pages.
- `task-attachments`: files attached to tasks, stored in Cloudinary, and shown to the people who can see the task.

### Modified Capabilities
- `role-management`: the catalog gains `Admin`, and listing, assigning and removing a user's global roles become admin-only.
- `project-membership`: the `Admin` role is rejected as a project role.

## Impact

- **Backend, Application layer:**
  - admin check helper;
  - `AccountDto.IsAdmin`;
  - `RoleService` gains a caller and admin checks;
  - new `MailSettingsService`;
  - `IFileStorage` interface, and `TaskItemDto.Attachments`;
  - `ProjectMembershipService` rejects `Admin`.
- **Backend, Infrastructure layer:**
  - the IMAP client, using the new **MailKit** dependency;
  - a `BackgroundService` that does the polling;
  - ASP.NET Data Protection for the mailbox password, with keys persisted in the database;
  - admin bootstrap at startup;
  - `CloudinaryFileStorage`, using the new **CloudinaryDotNet** dependency.
- **Database:** one migration that:
  - seeds the `Admin` role;
  - adds a single-row `MailSettings` table;
  - adds an `InboundEmails` table recording each processed message, for de-duplication and audit;
  - adds the Data Protection keys table;
  - adds a `TaskAttachments` table.
- **API:**
  - new admin-only `GET/PUT /api/settings/mail` and `POST /api/settings/mail/test`;
  - admin-only global role endpoints;
  - `isAdmin` on `/api/account/me`.
- **Configuration:** new `ADMIN_EMAIL` and `CLOUDINARY_*` variables in `.env`, passed to the `api` container in `docker-compose.yml`. Secrets stay out of the repository. The container also needs outbound network access to the IMAP server and to Cloudinary.
- **Frontend:**
  - Settings menu node;
  - `settings/users` and `settings/mail` routes;
  - an admin route guard;
  - the mail settings page;
  - Admin toggles on the users page;
  - project-member role choices limited to `Owner` and `Member`;
  - an attachments list in the task details.
- **Out of scope:**
  - sending email;
  - uploading or deleting attachments by hand;
  - private or signed attachment links;
  - deleting files from Cloudinary when tasks or projects are deleted;
  - one mailbox for several projects;
  - mapping senders to assignees;
  - replying to or threading emails.
