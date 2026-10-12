# Spec Delta

## Purpose

Decides who can see and act on a project and its tasks. Access is based on the roles the signed-in user holds in that project: members can see the project and work on its tasks, and owners can also delete it.

## ADDED Requirements

### Requirement: Projects are visible only to their members
The system SHALL list, for a signed-in user, only the non-deleted projects in which that user holds at least one role. A user who holds no role in a project MUST NOT see it in any listing.

#### Scenario: Member sees the project
- **WHEN** user A holds `Member` in project P and requests the list of projects
- **THEN** the list contains P

#### Scenario: Non-member does not see the project
- **WHEN** user B holds no role in project P and requests the list of projects
- **THEN** the list does not contain P

#### Scenario: User without any project
- **WHEN** a user who is a member of no project requests the list of projects
- **THEN** the list is empty and the request succeeds

#### Scenario: Project without members
- **WHEN** a project has no members at all
- **THEN** it appears in no user's list

### Requirement: Projects report the caller's roles
Each project the system returns to a signed-in user SHALL include the names of the roles that user holds in that project, and only those.

#### Scenario: Owner and member roles
- **WHEN** user A holds `Owner` and `Member` in project P and lists projects
- **THEN** P is returned with the roles `Owner` and `Member`

#### Scenario: Roles of other users are not shown
- **WHEN** user A holds `Member` in P and user B holds `Owner` in P, and user A lists projects
- **THEN** P is returned with only the role `Member`

### Requirement: The creator of a project becomes its Owner
When a signed-in user creates a project, the system SHALL record that user as `Owner` of the new project in the same operation. A project MUST NOT be stored without its creator as `Owner`.

#### Scenario: Create a project
- **WHEN** user A creates project P
- **THEN** user A holds `Owner` in P, and P appears in user A's list with the role `Owner`

#### Scenario: Other users do not see a new project
- **WHEN** user A creates project P
- **THEN** user B, who holds no role in P, does not see P

### Requirement: Only owners can delete a project
The system SHALL soft-delete a project only when the caller holds `Owner` in it. A member without `Owner` MUST be refused as forbidden. A caller who holds no role in the project MUST be told the project was not found. A refused deletion MUST NOT change any data.

#### Scenario: Owner deletes
- **WHEN** an `Owner` of project P deletes it
- **THEN** P is soft-deleted and no longer appears in any user's list

#### Scenario: Member without Owner tries to delete
- **WHEN** a user who holds only `Member` in P deletes it
- **THEN** the system answers forbidden and P is unchanged

#### Scenario: Non-member tries to delete
- **WHEN** a user who holds no role in P deletes it
- **THEN** the system answers not found and P is unchanged

### Requirement: Projects are hidden from non-members
For every operation on a specific project, such as listing its members, changing its members or deleting it, the system SHALL answer not found to a caller who holds no role in that project. The same answer applies when the project does not exist or is deleted, so non-members cannot learn which projects exist.

#### Scenario: Non-member lists members
- **WHEN** a user who holds no role in project P requests P's members
- **THEN** the system answers not found

#### Scenario: Same answer as an unknown project
- **WHEN** a non-member requests the members of existing project P, and also requests the members of a project id that does not exist
- **THEN** both requests get the same not-found answer

### Requirement: Tasks are visible only to members of their project
The system SHALL list, for a signed-in user, only the tasks of non-deleted projects in which that user holds at least one role. A request for a single task in a project where the caller holds no role MUST be answered as not found.

#### Scenario: Task list is filtered
- **WHEN** user A is a member of project P but not of project Q, and both have tasks
- **THEN** user A's task list contains P's tasks and none of Q's tasks

#### Scenario: Read a task of another project
- **WHEN** user A requests a task of project Q, where user A holds no role
- **THEN** the system answers not found

### Requirement: Only members can create and update tasks
The system SHALL create a task only in a project where the caller holds at least one role, and SHALL update a task only when the caller holds at least one role in the task's project. Any role is enough. Otherwise the system MUST answer not found and change nothing.

#### Scenario: Member creates a task
- **WHEN** a user who holds `Member` in project P creates a task in P
- **THEN** the task is created

#### Scenario: Create in a project the user does not belong to
- **WHEN** a user who holds no role in project Q creates a task in Q
- **THEN** the system answers not found and no task is created

#### Scenario: Update a task of another project
- **WHEN** a user who holds no role in project Q updates a task of Q
- **THEN** the system answers not found and the task is unchanged

### Requirement: Project and task endpoints require authentication
The system SHALL reject every project and task request that lacks a valid token, and it MUST NOT return or change any data for such a request.

#### Scenario: Unauthenticated project list
- **WHEN** a request without a valid token asks for the list of projects
- **THEN** the system rejects it as unauthorized
