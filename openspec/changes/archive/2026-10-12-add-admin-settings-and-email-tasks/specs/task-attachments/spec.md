# Spec Delta

## Purpose

Files attached to tasks: where they are stored, who can see them, and how they appear in the web app. In this change, attachments come only from emails that become tasks.

## ADDED Requirements

### Requirement: Tasks expose their attachments
Every task response SHALL include the task's attachments, each with its file name, content type, size in bytes and download link. A task without attachments SHALL return an empty list. Attachments SHALL be visible exactly to the people who can see the task, which means members of the task's project.

#### Scenario: Member reads a task with attachments
- **WHEN** a member of project P reads a task of P that has two attachments
- **THEN** the response lists both attachments, with name, type, size and link

#### Scenario: Non-member
- **WHEN** a user who is not a member of P requests a task of P
- **THEN** the system answers not found, and no attachment data is returned

### Requirement: Attachment links are public and permanent
An attachment's download link SHALL open the file without signing in and SHALL NOT expire. The application SHALL show links only in responses to people who can see the task. Anyone who has a link can still open the file.

#### Scenario: Open a link without a session
- **WHEN** someone opens an attachment link in a browser that is not signed in to the app
- **THEN** the file is served

### Requirement: Task details list the attachments
The task details in the web app SHALL list the task's attachments with file name and size. Each entry SHALL be a link that opens or downloads the file in a new tab. When a task has no attachments, the attachments section SHALL NOT be shown.

#### Scenario: Task with attachments
- **WHEN** a member opens a task that has an attachment `report.pdf` of 120 KB
- **THEN** the details show "report.pdf" with its size, as a link that opens the file

#### Scenario: Task without attachments
- **WHEN** a member opens a task without attachments
- **THEN** no attachments section is shown

### Requirement: Storage credentials stay in configuration
The system SHALL read the file storage account (cloud name, API key and API secret) only from its configuration. It MUST NOT store them in the database or return them in any response, and MUST NOT write the API secret to logs. When the configuration is missing or incomplete, storage SHALL be reported as not configured and no upload SHALL be attempted.

#### Scenario: Not configured
- **WHEN** the system runs without storage credentials
- **THEN** storage is reported as not configured, and the rest of the application works normally
