# project-membership Specification

## Purpose

Defines how users are made members of a project, each with one or more roles in that project, independently of the roles they hold elsewhere.

## Requirements

### Requirement: Add a user to a project with a role
The system SHALL allow an authenticated user to give a user a catalog role in a project. A project MAY have any number of members, and one user MAY hold several different roles in the same project.

#### Scenario: Add several users to one project
- **WHEN** two different users are each given the `Member` role in the same project
- **THEN** both users are members of that project

#### Scenario: Several roles for one user in one project
- **WHEN** a user who is `Member` of a project is also given the `Owner` role in that project
- **THEN** the user holds both `Member` and `Owner` in that project

#### Scenario: Same user, different projects
- **WHEN** a user is `Owner` in one project and `Member` in another
- **THEN** each project reports only the role the user holds in that project

### Requirement: Reject invalid membership assignments
The system SHALL refuse to record the same user and role twice in a project, and SHALL refuse assignments that name a user, a role or a project that does not exist or a project that has been deleted. A refused assignment MUST NOT change any data.

#### Scenario: Duplicate assignment
- **WHEN** a user is given a role in a project where the user already holds that role
- **THEN** the system rejects the request with a conflict and the user still holds the role exactly once

#### Scenario: Unknown user, role or project
- **WHEN** an assignment names a user, a role or a project that does not exist
- **THEN** the system answers that it was not found and records nothing

#### Scenario: Deleted project
- **WHEN** an assignment targets a project that has been deleted
- **THEN** the system answers that the project was not found and records nothing

### Requirement: List the members of a project
The system SHALL let an authenticated user list a project's members, showing for each member the user's id, name and email and all roles that user holds in that project. Roles held in other projects MUST NOT appear. The listing MUST NOT expose password data.

#### Scenario: Members grouped per user
- **WHEN** a project has user A as `Owner` and `Member`, and user B as `Member`
- **THEN** the listing has two entries: A with both roles and B with `Member`

#### Scenario: Project without members
- **WHEN** a project has no members
- **THEN** the listing is empty and the request succeeds

#### Scenario: Unknown project
- **WHEN** the members of a project that does not exist are requested
- **THEN** the system answers that the project was not found

### Requirement: Remove roles and members from a project
The system SHALL let an authenticated user remove one role of a user from a project, and remove a user from a project altogether, which drops all of that user's roles in it. Removing a membership MUST NOT delete the user, the role or the project, and MUST NOT affect the user's memberships in other projects.

#### Scenario: Remove one role
- **WHEN** the `Owner` role is removed from a user who holds `Member` and `Owner` in a project
- **THEN** the user remains a member of the project with only `Member`

#### Scenario: Remove a member entirely
- **WHEN** a user is removed from a project
- **THEN** the user holds no role in that project, still holds roles in other projects, and the user account still exists

#### Scenario: Remove something that is not there
- **WHEN** a role or member is removed that the project does not have
- **THEN** the system answers that it was not found and changes nothing

### Requirement: Membership endpoints require authentication
The system SHALL reject every membership request that lacks a valid token. In this change, holding or lacking a role MUST NOT restrict who may call these operations; authorization based on roles is a separate capability.

#### Scenario: Unauthenticated request
- **WHEN** a request without a valid token tries to list or change a project's members
- **THEN** the system rejects it as unauthorized and changes nothing

#### Scenario: Any authenticated user may manage membership
- **WHEN** an authenticated user who holds no role in a project adds a member to it
- **THEN** the assignment is recorded
