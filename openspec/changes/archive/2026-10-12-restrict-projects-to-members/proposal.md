# Proposal

## Why

Projects already have members with `Owner` and `Member` roles (`ProjectUserRole`), but the roles control nothing. Every signed-in user sees every project and every task, can delete any project, and can add or remove anyone on any project. The `project-membership` spec explicitly deferred role-based authorization to a separate capability. This change is that capability.

## What Changes

- **BREAKING** `GET /api/projects` returns only the projects the caller is a member of, meaning the caller holds at least one role in them. Each project carries the caller's roles in it, so the UI can tell owners from plain members.
- Creating a project makes the creator its `Owner` as part of the same save.
- **BREAKING** Only an `Owner` of a project may soft-delete it.
- **BREAKING** Only an `Owner` may add a member, add or remove a role, or remove a member. Any member may list the project's members.
- A project must always keep at least one `Owner`. Removing the last `Owner` role, or removing the last owner as a member, is rejected.
- **BREAKING** Tasks follow their project. Listing returns only tasks of the caller's projects. Reading, creating or updating a task requires membership in its project, and any role is enough.
- To a non-member, a project behaves as if it does not exist: they get `404`, not `403`, so the API does not reveal which projects exist. A member who lacks the `Owner` role gets `403` on owner-only operations.
- Web app: on the project page, non-owners see the member list read-only, with no add, change-role or remove controls. The app reports a `403` as "only owners can do this".
- Existing projects that have no members become invisible to everyone. No data migration assigns owners, because who created those projects was never recorded.

## Capabilities

### New Capabilities
- `project-access`: who can see and act on a project and its tasks, based on the caller's membership and roles in that project. Covers project visibility, creator-as-owner, owner-only deletion and member-only task access.

### Modified Capabilities
- `project-membership`: membership changes become owner-only, listing becomes member-only, the "any authenticated user may manage membership" rule is replaced, and a project must keep at least one `Owner`.
- `project-members-ui`: management controls are shown only to owners, and `403` responses are reported.

## Impact

- **Application layer**:
  - `IProjectService`, `IProjectMembershipService` and `ITaskService` take the caller's user id.
  - `ServiceStatus` gains `Forbidden`.
  - A new `ProjectDto` carries the caller's roles, replacing the `Project` entity in project responses.
- **Api layer**: the project, membership and task endpoints read the user id from the token, as `AccountEndpoints` already does, and map `Forbidden` to `403`.
- **Database**: no schema change and no migration. The existing `ProjectUserRoles` table and the seeded `Owner` role are enough.
- **Frontend**: the `Project` model gains `roles`, the project members component hides controls for non-owners, and error handling covers `403`.
- **Tests**: new service tests for visibility, creator-as-owner, owner-only operations, last-owner protection and task access. Existing `ProjectServiceTests` are updated for the new signatures.
- **API clients**: any client that relied on seeing all projects or tasks, or on managing membership without being an owner, breaks.
