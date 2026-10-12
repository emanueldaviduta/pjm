# admin-access Specification

## Purpose

Defines the global `Admin` role: who holds it, how the first admin is chosen, how admins grant or revoke it, and how the client learns whether the signed-in user is an admin. Admin-only features build on this role.

## Requirements

### Requirement: First admin comes from configuration
The system SHALL read an admin email address from its configuration. If a user with that email exists, the system SHALL give that user the `Admin` role when it starts. If no such user exists yet, the system SHALL give the role when a user registers with that email. A missing or empty setting MUST NOT grant `Admin` to anyone.

#### Scenario: Configured user already registered
- **WHEN** the system starts with the admin email set to `boss@example.com` and that user exists without `Admin`
- **THEN** the user holds `Admin` once startup completes

#### Scenario: Configured user registers later
- **WHEN** the admin email is `boss@example.com` and a user registers with that email
- **THEN** the new user holds `Admin`

#### Scenario: Email comparison ignores case
- **WHEN** the admin email is `Boss@Example.com` and the user's email is `boss@example.com`
- **THEN** the user holds `Admin`

#### Scenario: No admin email configured
- **WHEN** the admin email setting is missing or empty
- **THEN** startup and registration grant `Admin` to nobody

#### Scenario: Restart does not duplicate the role
- **WHEN** the system restarts and the configured user already holds `Admin`
- **THEN** the user still holds `Admin` exactly once

### Requirement: Admins grant and revoke Admin
The system SHALL let a user who holds `Admin` give `Admin` to another user, and remove `Admin` from a user. A caller without `Admin` MUST be refused as forbidden and nothing changes.

#### Scenario: Admin grants Admin
- **WHEN** an admin gives `Admin` to user B
- **THEN** user B holds `Admin`

#### Scenario: Non-admin tries to grant Admin to themselves
- **WHEN** a user without `Admin` tries to give `Admin` to themselves
- **THEN** the system answers forbidden and the user still does not hold `Admin`

### Requirement: At least one Admin remains
The system SHALL refuse to remove `Admin` from a user when no other user holds `Admin`. The refusal MUST be reported as invalid with an explanation and MUST NOT change any data.

#### Scenario: Remove the only admin
- **WHEN** the only admin tries to remove their own `Admin` role
- **THEN** the system rejects the request with the message that at least one admin must remain, and the user still holds `Admin`

#### Scenario: Another admin remains
- **WHEN** there are two admins and one removes `Admin` from the other
- **THEN** the change is recorded

### Requirement: Clients can tell whether the user is an admin
The current-account response SHALL say whether the signed-in user holds `Admin`. The value MUST reflect the stored roles at the time of the request, not the time the token was issued.

#### Scenario: Admin reads their account
- **WHEN** a user who holds `Admin` requests their account
- **THEN** the response says the user is an admin

#### Scenario: Role removed after sign-in
- **WHEN** a signed-in user's `Admin` role is removed and the user then requests their account with the same token
- **THEN** the response says the user is not an admin

### Requirement: Admin-only operations are checked on every request
Every admin-only operation SHALL check, at the time of the request, that the caller holds `Admin`. A caller without `Admin` MUST be refused as forbidden, and a request without a valid token MUST be refused as unauthorized. Admin-only operations include managing users' global roles and reading or changing the inbound email settings.

#### Scenario: Admin role revoked during a session
- **WHEN** a user loses `Admin` and then, with the same token, calls an admin-only operation
- **THEN** the system answers forbidden

#### Scenario: Unauthenticated request
- **WHEN** a request without a valid token calls an admin-only operation
- **THEN** the system answers unauthorized
