# project-membership Specification

## Purpose

Defines how users are made members of a project, each with one or more roles in that project, independently of the roles they hold elsewhere.

## Requirements

### Requirement: Add a user to a project with a role
The system SHALL allow an `Owner` of a project to give a user a catalog role in that project. A project MAY have any number of members, and one user MAY hold several different roles in the same project. A member without `Owner` MUST be refused as forbidden. A caller who holds no role in the project MUST be told the project was not found. A refused request MUST NOT change any data.

#### Scenario: Add several users to one project
- **WHEN** an `Owner` of a project gives the `Member` role in that project to two different users
- **THEN** both users are members of that project

#### Scenario: Several roles for one user in one project
- **WHEN** an `Owner` also gives the `Owner` role to a user who is already `Member` of the project
- **THEN** the user holds both `Member` and `Owner` in that project

#### Scenario: Same user, different projects
- **WHEN** a user is `Owner` in one project and `Member` in another
- **THEN** each project reports only the role the user holds in that project

#### Scenario: Member without Owner tries to add
- **WHEN** a user who holds only `Member` in a project tries to add a member to it
- **THEN** the system answers forbidden and records nothing

#### Scenario: Non-member tries to add
- **WHEN** a user who holds no role in a project tries to add a member to it
- **THEN** the system answers that the project was not found and records nothing

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
The system SHALL let any member of a project list its members. For each member, the listing shows the user's id, name and email and all roles that user holds in that project. Roles held in other projects MUST NOT appear, and the listing MUST NOT expose password data. A caller who holds no role in the project MUST be told the project was not found.

#### Scenario: Members grouped per user
- **WHEN** a project has user A as `Owner` and `Member`, and user B as `Member`, and user B lists the members
- **THEN** the listing has two entries: A with both roles and B with `Member`

#### Scenario: Project without members
- **WHEN** the members of a project that has no members are requested, which is only possible for a project created before owners were recorded
- **THEN** no caller is a member of it, so the system answers that the project was not found

#### Scenario: Unknown project
- **WHEN** the members of a project that does not exist are requested
- **THEN** the system answers that the project was not found

#### Scenario: Non-member lists members
- **WHEN** a user who holds no role in a project requests its members
- **THEN** the system answers that the project was not found

### Requirement: Remove roles and members from a project
The system SHALL let an `Owner` of a project remove one role of a user from that project, or remove a user from the project altogether, which drops all of that user's roles in it. Removing a membership MUST NOT delete the user, the role or the project, and MUST NOT affect the user's memberships in other projects. A member without `Owner` MUST be refused as forbidden, and a caller who holds no role in the project MUST be told the project was not found.

#### Scenario: Remove one role
- **WHEN** an `Owner` removes the `Owner` role from a user who holds `Member` and `Owner`, while another user is still `Owner` of the project
- **THEN** the user remains a member of the project with only `Member`

#### Scenario: Remove a member entirely
- **WHEN** an `Owner` removes a user from a project
- **THEN** the user holds no role in that project, still holds roles in other projects, and the user account still exists

#### Scenario: Remove something that is not there
- **WHEN** an `Owner` removes a role or member that the project does not have
- **THEN** the system answers that it was not found and changes nothing

#### Scenario: Member without Owner tries to remove
- **WHEN** a user who holds only `Member` in a project tries to remove a role or a member
- **THEN** the system answers forbidden and changes nothing

### Requirement: Membership access depends on project roles
The system SHALL reject every membership request that lacks a valid token. For a request with a valid token, what the caller may do depends on the roles the caller holds in the project. Any member may list the members, and only an `Owner` may change them.

#### Scenario: Unauthenticated request
- **WHEN** a request without a valid token tries to list or change a project's members
- **THEN** the system rejects it as unauthorized and changes nothing

#### Scenario: Authenticated non-member cannot manage membership
- **WHEN** an authenticated user who holds no role in a project adds a member to it
- **THEN** the system answers that the project was not found and records nothing

### Requirement: A project keeps at least one Owner
The system SHALL refuse any membership change that would leave a non-deleted project without a user holding `Owner`. This covers removing the `Owner` role from the last owner and removing the last owner from the project. The refusal MUST be reported as invalid and MUST NOT change any data.

#### Scenario: Remove the last Owner role
- **WHEN** a project has exactly one `Owner`, who also holds `Member`, and that `Owner` role is removed
- **THEN** the system rejects the request and the user still holds `Owner`

#### Scenario: Remove the last owner as a member
- **WHEN** the only `Owner` of a project is removed from the project
- **THEN** the system rejects the request and the user keeps all of their roles in it

#### Scenario: Another Owner remains
- **WHEN** a project has two owners and one of them loses the `Owner` role
- **THEN** the change is recorded and the other user is still `Owner`

### Requirement: Admin is not a project role
The system SHALL refuse to give a user the `Admin` role in a project. The refusal MUST be reported as invalid, and nothing is recorded. Holding `Admin` globally SHALL NOT make a user a member of any project or give them any project role.

#### Scenario: Owner tries to assign Admin in a project
- **WHEN** an `Owner` of a project gives a user the `Admin` role in that project
- **THEN** the system rejects the request as invalid and records nothing

#### Scenario: Admin is not automatically a project member
- **WHEN** a user who holds `Admin` but no role in project P lists projects
- **THEN** P is not in the list
