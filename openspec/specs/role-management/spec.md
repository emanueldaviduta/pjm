# role-management Specification

## Purpose

Defines the catalog of roles the system knows about and how a user is given or stripped of roles, so that later features can decide what a person may do.

## Requirements

### Requirement: Role catalog with default roles
The system SHALL keep a catalog of roles, each identified by a unique name. The catalog SHALL contain the roles `Owner` and `Member` from the moment the feature is available, including on databases that already hold data.

#### Scenario: Default roles exist on a new database
- **WHEN** the system starts on an empty database
- **THEN** the role catalog contains `Owner` and `Member`

#### Scenario: Default roles are added to an existing database
- **WHEN** the system is upgraded on a database that already has users, projects and tasks
- **THEN** the role catalog contains `Owner` and `Member` and the existing users, projects and tasks are unchanged

### Requirement: List roles
The system SHALL let an authenticated user retrieve every role in the catalog, each with its id and name.

#### Scenario: Authenticated user lists roles
- **WHEN** an authenticated user requests the list of roles
- **THEN** the response is successful and includes `Owner` and `Member`

#### Scenario: Unauthenticated request
- **WHEN** a request without a valid token asks for the list of roles
- **THEN** the system rejects it as unauthorized and returns no roles

### Requirement: Assign multiple roles to a user
The system SHALL allow an authenticated user to assign any number of catalog roles to a user, and a user SHALL be able to hold several roles at the same time. Assigning a role the user already holds MUST NOT create a duplicate.

#### Scenario: Assign a second role
- **WHEN** a user who already holds `Member` is assigned `Owner`
- **THEN** the user holds both `Member` and `Owner`

#### Scenario: Assign a role twice
- **WHEN** a role is assigned to a user who already holds it
- **THEN** the system rejects the request with a conflict and the user still holds the role exactly once

#### Scenario: Unknown user or role
- **WHEN** a role assignment names a user or a role that does not exist
- **THEN** the system answers that the user or role was not found and changes nothing

### Requirement: List and remove a user's roles
The system SHALL let an authenticated user list the roles held by a user and remove a single role from that user without affecting the user's other roles.

#### Scenario: List a user's roles
- **WHEN** an authenticated user requests the roles of a user who holds `Member` and `Owner`
- **THEN** the response lists exactly `Member` and `Owner`

#### Scenario: Remove one role
- **WHEN** the `Owner` role is removed from a user who holds `Member` and `Owner`
- **THEN** the user holds only `Member`

#### Scenario: Remove a role the user does not hold
- **WHEN** a role is removed from a user who does not hold it
- **THEN** the system answers that the assignment was not found and changes nothing
