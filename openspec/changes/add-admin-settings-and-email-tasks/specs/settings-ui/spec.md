# Spec Delta

## Purpose

Defines the admin-only Settings area of the web app, where admins manage users and their Admin role and configure the mailbox whose emails become tasks on a project.

## ADDED Requirements

### Requirement: Settings menu node only for admins
The main menu SHALL show a **Settings** node with the sub-nodes **Users** and **Email → Project** only when the signed-in person is an admin. Other people SHALL NOT see the node. The top-level **Users** menu entry SHALL be removed.

#### Scenario: Admin sees Settings
- **WHEN** an admin is signed in
- **THEN** the menu shows Settings with Users and Email → Project beneath it

#### Scenario: Non-admin does not see Settings
- **WHEN** a person without `Admin` is signed in
- **THEN** the menu has no Settings node and no Users entry

#### Scenario: Admin role changes
- **WHEN** a person's `Admin` role is removed and they reload the app
- **THEN** the Settings node is no longer shown

### Requirement: Settings pages are guarded
Opening a settings page SHALL require the person to be an admin. A signed-in person who is not an admin SHALL be redirected to the home page. A signed-out person SHALL be sent to sign in, and then returned to the requested page. The former `/users` address SHALL redirect to the Users settings page.

#### Scenario: Non-admin opens a settings URL
- **WHEN** a non-admin opens `/settings/mail` directly
- **THEN** they land on the home page and the settings are not loaded

#### Scenario: Old users address
- **WHEN** an admin opens `/users`
- **THEN** the Users settings page is shown

### Requirement: Users page manages the Admin role
The Users settings page SHALL list all users with their name and email, keep the existing action to add a user, and show for each user whether they are an admin. An admin SHALL be able to grant or revoke `Admin` from a user's row. Failures, including the refusal to remove the last admin, SHALL be reported with the server's explanation, and the list SHALL then show the server's current state.

#### Scenario: Grant Admin
- **WHEN** an admin grants `Admin` to a user from the list
- **THEN** that user's row shows them as an admin

#### Scenario: Revoke the last admin
- **WHEN** the only admin tries to revoke their own `Admin`
- **THEN** the page shows that at least one admin must remain, and the row still shows them as an admin

### Requirement: Email → Project page configures the mailbox
The Email → Project page SHALL let an admin:
- view and edit every mailbox setting;
- pick the target project from all non-deleted projects;
- add and remove allowed sender addresses;
- turn the service on or off;
- save.

The password field SHALL never be pre-filled. It SHALL indicate when a password is already stored, and leaving it empty keeps the stored password. Validation errors from the server SHALL be shown next to the form, and the form SHALL keep what the admin typed.

#### Scenario: Configure and enable
- **WHEN** an admin fills in host, port, username, password, folder, one sender and a project, enables the service and saves
- **THEN** a success message is shown, and reloading the page shows the saved values with "password set" and an empty password field

#### Scenario: Server rejects the settings
- **WHEN** the server rejects a save because no project is chosen
- **THEN** the page shows that message and keeps the values that were entered

### Requirement: Test connection and poll status on the page
The Email → Project page SHALL offer a "Test connection" action that uses the values currently in the form. It SHALL show the result of the test. The page SHALL also show the time and outcome of the last poll, with the error message when the last poll failed.

#### Scenario: Test fails
- **WHEN** an admin tests a connection with a wrong password
- **THEN** the page shows that authentication failed, and the saved settings are unchanged

#### Scenario: Last poll failed
- **WHEN** the last poll could not reach the server
- **THEN** the page shows the time of that poll and its error message

### Requirement: Mail page shows whether attachment storage is configured
The Email → Project page SHALL state whether file storage for attachments is configured. When it is not, the page SHALL explain that tasks will be created without their attachments.

#### Scenario: Storage not configured
- **WHEN** an admin opens the page while file storage is not configured
- **THEN** the page says that attachments will not be stored
