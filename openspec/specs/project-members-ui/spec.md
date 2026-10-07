# project-members-ui Specification

## Purpose

Defines what a signed-in person can see and do in the web app to review who belongs to a project and to add, re-role and remove its members, without using the API directly.

## Requirements

### Requirement: Show the members of a project
The project page SHALL show every member of the project with the member's name and the roles held in that project. It SHALL show an explicit empty state when there are no members, and a loading state while members are being fetched.

#### Scenario: Project with members
- **WHEN** a person opens a project that has members
- **THEN** each member appears once, with a badge for each role held in that project

#### Scenario: Project without members
- **WHEN** a person opens a project that has no members
- **THEN** the page says there are no members yet and offers the action to add one

#### Scenario: Members fail to load
- **WHEN** the members request fails
- **THEN** the page shows an error message with a way to retry, and the rest of the project page stays usable

### Requirement: Add a member by searching for a person
The project page SHALL let a person add a member by searching the registered users by name or email, choosing one, choosing a role, and confirming. The role SHALL default to `Member`.

#### Scenario: Search and add
- **WHEN** a person opens the add-member dialog, types part of a name or email, selects a match, keeps the default role and confirms
- **THEN** the dialog closes, the user appears among the members with the `Member` role, and a success message is shown

#### Scenario: No matching user
- **WHEN** the search text matches no user
- **THEN** the dialog says no user was found and the confirm action stays disabled

#### Scenario: Confirm requires a choice
- **WHEN** no user is selected
- **THEN** the confirm action is disabled

### Requirement: Prevent adding a duplicate membership
The add-member dialog SHALL show users who already hold a role in the project as already members, and SHALL NOT allow confirming a user and role pair that is already recorded.

#### Scenario: User already a member
- **WHEN** the search results include a user who is already on the project
- **THEN** that user is marked as a member and the roles they already hold cannot be chosen again

#### Scenario: Server reports a conflict
- **WHEN** the server rejects an addition as a duplicate
- **THEN** the person sees a message that the user already has that role and the member list is refreshed

### Requirement: Change a member's roles
The project page SHALL let a person add another role to a member and remove a single role from a member, from the member's row, without leaving the page.

#### Scenario: Add a second role
- **WHEN** a person adds the `Owner` role to a member who holds `Member`
- **THEN** the member's row shows both roles

#### Scenario: Remove one role
- **WHEN** a person removes one of two roles from a member
- **THEN** the member's row shows only the remaining role

### Requirement: Remove a member with confirmation
The project page SHALL let a person remove a member from the project. Removing the member entirely, including by removing their last role, SHALL ask for confirmation first and SHALL leave the member list unchanged if the person cancels.

#### Scenario: Confirm removal
- **WHEN** a person removes a member and confirms
- **THEN** the member disappears from the list and a success message is shown

#### Scenario: Cancel removal
- **WHEN** a person removes a member and cancels the confirmation
- **THEN** nothing changes

### Requirement: Report failures and expired sessions
Every membership action SHALL report failure to the person and SHALL NOT leave the list showing a change that was not saved. If the session has expired, the page SHALL tell the person to sign in again.

#### Scenario: Action fails
- **WHEN** adding or removing a member fails on the server
- **THEN** an error message is shown and the list shows the server's current state

#### Scenario: Session expired
- **WHEN** a membership request is rejected as unauthorized
- **THEN** the person is told to sign in again with a link to the sign-in page

### Requirement: Keyboard and screen reader access
All member actions SHALL be reachable and usable by keyboard, with labelled controls and dialog focus handled, and role badges SHALL not rely on color alone.

#### Scenario: Keyboard only
- **WHEN** a person uses only the keyboard
- **THEN** they can open the add-member dialog, search, pick a user and role, confirm, and remove a member
