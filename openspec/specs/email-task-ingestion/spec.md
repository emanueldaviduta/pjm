# email-task-ingestion Specification

## Purpose

Turns emails that allowed senders send to a configured mailbox into tasks on a configured project. Admins manage the mailbox settings, and the server polls the mailbox in the background.

## Requirements

### Requirement: Admins manage the inbound mailbox settings
The system SHALL keep one set of inbound mailbox settings, which only admins may read or change:
- an enabled flag;
- IMAP host, port and TLS mode;
- username and password;
- the folder to watch, defaulting to `INBOX`;
- the allowed sender addresses;
- the target project;
- the poll interval in seconds, defaulting to 60.

#### Scenario: Admin saves settings
- **WHEN** an admin saves valid settings
- **THEN** reading the settings returns the saved values

#### Scenario: Non-admin reads settings
- **WHEN** a user without `Admin` requests the mailbox settings
- **THEN** the system answers forbidden and returns no settings

### Requirement: The mailbox password is never disclosed
The system SHALL store the mailbox password encrypted, and MUST NOT return it in any response. Responses SHALL only say whether a password is set. When an admin saves the settings without a password, the stored password SHALL stay unchanged.

#### Scenario: Read settings with a password set
- **WHEN** an admin reads settings that have a stored password
- **THEN** the response shows that a password is set and does not contain the password

#### Scenario: Save without retyping the password
- **WHEN** an admin changes only the poll interval and leaves the password empty
- **THEN** the stored password is unchanged and polling keeps authenticating

### Requirement: Settings are validated before they are saved
When the settings are saved with the enabled flag on, the system SHALL require:
- a host;
- a port between 1 and 65535;
- a username;
- a password, either sent with the request or already stored;
- at least one well-formed sender address;
- an existing, non-deleted target project;
- a poll interval between 30 and 3600 seconds.

Invalid settings MUST be rejected with a message naming the problem, and the previously stored settings MUST stay unchanged. Disabled settings MAY be incomplete.

#### Scenario: Enable without a project
- **WHEN** an admin enables the service without choosing a project
- **THEN** the save is rejected with a message that a project is required, and the stored settings are unchanged

#### Scenario: Malformed sender
- **WHEN** an admin saves an allowed sender `not-an-email` with the service enabled
- **THEN** the save is rejected with a message naming the invalid address

#### Scenario: Save disabled draft
- **WHEN** an admin saves settings with the service disabled and no host
- **THEN** the settings are saved

### Requirement: Admins can test the mailbox connection
The system SHALL let an admin test a connection using the submitted settings, falling back to the stored password when none is submitted. The test connects, authenticates and opens the folder. It SHALL report success, or the failure reason, within 30 seconds. A test MUST NOT create tasks, change the stored settings or mark messages as processed.

#### Scenario: Successful test
- **WHEN** an admin tests settings that reach a working mailbox and folder
- **THEN** the system reports success

#### Scenario: Wrong password
- **WHEN** an admin tests settings with a wrong password
- **THEN** the system reports that authentication failed, and nothing else changes

### Requirement: Emails from allowed senders become tasks
While enabled, the system SHALL check the configured folder at the configured interval. Each new message whose From address matches an allowed sender becomes one task in the target project. The match is exact and ignores case.

#### Scenario: Allowed sender
- **WHEN** an email from an allowed sender arrives
- **THEN** within one poll interval, one task for it appears in the target project

#### Scenario: Sender address case
- **WHEN** an allowed sender is `alerts@example.com` and an email arrives from `Alerts@Example.com`
- **THEN** a task is created

#### Scenario: Visible to project members
- **WHEN** a task is created from an email
- **THEN** members of the target project see it like any other task, and non-members do not

### Requirement: Tasks created from email carry the message content
A task created from an email SHALL have:
- the subject as title, trimmed to 200 characters, or `(no subject)` when there is none;
- a description holding the sender, the received date and the plain-text body (converted from HTML when no plain-text part exists);
- status `Created`, priority `Medium`, no due date and no assignee.

#### Scenario: Subject and body
- **WHEN** an allowed sender sends "Server down" with the body "Disk full on db-1"
- **THEN** the task is titled "Server down", and its description contains the sender's address, the received date and "Disk full on db-1"

#### Scenario: Email without subject
- **WHEN** an allowed sender sends an email with an empty subject
- **THEN** the task is titled `(no subject)`

#### Scenario: HTML-only email
- **WHEN** an allowed sender sends an email that has only an HTML body
- **THEN** the task description contains the text of that body without HTML tags

### Requirement: Email attachments are stored with the task
When an email from an allowed sender becomes a task, the system SHALL store its attachments with the task. Attachments are the parts marked as attachments, plus inline parts that have a file name. Each file may be up to 10 MB, and at most 10 files are stored per email, in the order they appear.

#### Scenario: Email with a PDF
- **WHEN** an allowed sender sends an email with `report.pdf` attached
- **THEN** the created task has one attachment `report.pdf`, whose link serves the same file

#### Scenario: Inline image with a file name
- **WHEN** an email contains an inline image named `screenshot.png`
- **THEN** `screenshot.png` is stored as an attachment of the task

### Requirement: Attachments that cannot be stored are reported in the task
When an attachment is larger than 10 MB, exceeds the count of 10, fails to upload, or file storage is not configured, the system SHALL still create the task. The attachment is not stored, and the task description SHALL list it as not attached, with its name and the reason. Failed uploads MUST NOT be retried, and the email MUST still count as processed.

#### Scenario: File too large
- **WHEN** an allowed email has a 25 MB attachment and a 1 MB attachment
- **THEN** the task has the 1 MB file attached, and its description lists the 25 MB file as not attached because it is too large

#### Scenario: Storage not configured
- **WHEN** file storage is not configured and an allowed email has an attachment
- **THEN** the task is created, and its description lists the attachment as not attached because storage is not configured

#### Scenario: Upload fails
- **WHEN** the storage service rejects an upload
- **THEN** the task is created without that file, the description names it as not attached, and the next poll does not create the task again

### Requirement: Emails from other senders are skipped
The system SHALL NOT create a task for a message whose From address is not on the allowed list. It SHALL record that message as skipped, so it is not examined again.

#### Scenario: Unknown sender
- **WHEN** an email from `stranger@example.com` arrives and that address is not allowed
- **THEN** no task is created

### Requirement: Each email is processed at most once
The system SHALL create at most one task per message, even across restarts and repeated polls, and regardless of whether the message is later read, flagged or moved by another mail client. The system MUST NOT delete or alter messages in the mailbox.

#### Scenario: Restart after processing
- **WHEN** a message has produced a task and the server restarts
- **THEN** the next poll does not create a second task for it

#### Scenario: Message read elsewhere
- **WHEN** a message is marked as read in another mail client before the next poll
- **THEN** it is still processed exactly once

### Requirement: Only new mail is imported
When the service is first enabled, and whenever the host, username or folder changes, the system SHALL import only messages that arrive after that point. Messages already in the folder MUST NOT become tasks.

#### Scenario: Enable on a full inbox
- **WHEN** the service is enabled on a folder that already holds 500 messages from allowed senders
- **THEN** no tasks are created for those 500, and the next message that arrives does become a task

### Requirement: Polling problems are reported and retried
The system SHALL record the time and outcome of the last poll: the number of tasks created, or the error message. Admins see this with the settings. A failed poll, whether from an unreachable server, rejected credentials or a missing or deleted target project, MUST NOT create tasks, stop the service or affect other requests. The system SHALL try again at the next interval.

#### Scenario: Server unreachable
- **WHEN** the IMAP server cannot be reached during a poll
- **THEN** the settings show the error and the time of that poll, and the next interval tries again

#### Scenario: Target project deleted
- **WHEN** the target project is deleted while the service is enabled
- **THEN** no tasks are created, the status says the project is missing, and messages that arrive meanwhile are imported once a valid project is configured

#### Scenario: Recovery
- **WHEN** a poll succeeds after earlier failures
- **THEN** the status shows the successful poll and no error

### Requirement: Disabled service does not connect
While the service is disabled, the system MUST NOT connect to the mailbox. Polls SHALL resume within one interval after it is enabled again, and SHALL stop within one interval after it is disabled.

#### Scenario: Disable
- **WHEN** an admin disables the service
- **THEN** no further connections to the mailbox are made and no tasks are created
