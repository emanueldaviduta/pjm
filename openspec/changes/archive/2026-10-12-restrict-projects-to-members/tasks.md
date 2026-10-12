# Tasks

## 1. Shared access building blocks

- [x] 1.1 Add `ServiceStatus.Forbidden` with `Forbidden()` factories on `ServiceResult` and `ServiceResult<T>`, and map it to `403` in `ServiceResultExtensions.FromFailure` (design decision 3). Verify that `dotnet build backend/PJMSolution.slnx` succeeds.
- [x] 1.2 Add a `RoleNames.Owner` constant and the `ProjectAccess` helper in `ProjectManager.Application/Common` (design decision 2), with:
  - `VisibleProjects(userId)`: non-deleted projects the user holds a role in.
  - `RolesInProjectAsync(projectId, userId)`.

  Add `ProjectAccessTests`, using the existing in-memory SQLite setup, covering these cases: a member sees the project, a non-member does not, a deleted project is hidden, a project with no members is hidden, and the roles of other users are not returned. Verify with `dotnet test backend/PJMSolution.slnx`.

## 2. Projects: visibility, creator-as-owner, owner-only delete

- [x] 2.1 Add `ProjectDto` (including `Roles`) and `CreateProjectRequest { Name, Description }`. Change `IProjectService` / `ProjectService` to take `userId`:
  - `GetAllAsync(userId)` returns the visible projects as DTOs with the caller's roles.
  - `CreateAsync(userId, request)` saves the project and the creator's `Owner` row in one `SaveChangesAsync`.
  - `DeleteAsync(userId, id)` answers `NotFound` for a non-member, `Forbidden` for a non-owner, and otherwise soft-deletes and returns the caller's visible projects.

  Verify that `dotnet build` succeeds.
- [x] 2.2 Update `ProjectEndpoints` to read `principal.GetUserId()`, answer `401` when it is missing, bind `CreateProjectRequest`, and return the DTOs. Verify that `dotnet build` succeeds and that Swagger shows the new request and response shapes.
- [x] 2.3 Update `ProjectServiceTests` for the new signatures and add tests for the `project-access` scenarios:
  - Creator becomes `Owner` and sees the project with the role `Owner`.
  - Another user does not see it.
  - Owner delete works, Member delete is `Forbidden`, non-member delete is `NotFound`, and refused deletes leave the data unchanged.

  Verify with `dotnet test`.

## 3. Membership: owner-only changes, member-only listing, last owner

- [x] 3.1 Change `IProjectMembershipService` / `ProjectMembershipService` to take the caller's `userId` and apply the check order from design decision 2:
  1. Not a member: `NotFound`.
  2. Not an owner on add or remove: `Forbidden`.
  3. The existing validations.

  Verify that `dotnet build` succeeds.
- [x] 3.2 Add the last-owner guard to `RemoveRoleAsync` and `RemoveMemberAsync`, returning `Invalid("A project must keep at least one owner.")` (design decision 4). Verify through the tests in 3.4.
- [x] 3.3 Update `ProjectMembershipEndpoints` to pass `principal.GetUserId()` and answer `401` when it is missing. Verify that `dotnet build` succeeds.
- [x] 3.4 Add `ProjectMembershipServiceTests` covering the `project-membership` delta scenarios:
  - Owner adds a member, and adds a second role.
  - Member add or remove is `Forbidden`.
  - Non-member list or add is `NotFound`.
  - A member can list.
  - Removing the last `Owner` role or the last owner is rejected with no data change.
  - Removal works when another owner remains.
  - The existing duplicate and unknown-user/role cases still hold.

  Verify with `dotnet test`.

## 4. Tasks follow their project

- [x] 4.1 Change `ITaskService` / `TaskService` to take `userId`, filter `Query` by `ProjectAccess.VisibleProjects(userId)`, and make `CreateAsync` return `TaskItemDto?`, with `null` when the project is not visible (design decision 6). Update `TaskItemEndpoints` to pass the user id and map `null` to `404`. Verify that `dotnet build` succeeds.
- [x] 4.2 Add `TaskServiceTests` covering:
  - The list is filtered per member.
  - Reading or updating a task of another project returns `null`.
  - Creating in a non-member project returns `null` and stores nothing.
  - Tasks of a deleted project are hidden.
  - A Member, not only an Owner, can create and update.

  Verify with `dotnet test`.

## 5. Web app: owner-only controls and error messages

- [x] 5.1 Add `roles?: string[]` to the `Project` model and an `isOwner(project)` helper in `_models/project.ts`. Verify that `npm run build` in `frontend/` succeeds.
- [x] 5.2 Give `ProjectMembers` a `canManage` input. When it is false, hide the add-member button, the role-chip remove buttons, the add-role select and the remove-member button in `project-members.html`. Pass `[canManage]="isOwner(p)"` from `project-detail.html`. Verify with a new case in `project-members.spec.ts`: a non-owner sees members and badges with no management controls, and an owner sees the controls. Run `npm test` in `frontend/`.
- [x] 5.3 In `ProjectMembers.fail()`:
  - Handle `403` with "Only project owners can manage members".
  - Show the server's message for `400`, using `apiErrorMessage`.
  - After every successful membership change, call `ProjectService.load(true)` so that your own role changes take effect.

  Verify with spec cases for the `403` and last-owner `400` messages. Run `npm test`.

## 6. End-to-end check

- [x] 6.1 Run `docker compose up -d --build`, then register users A and B, and check through the UI or `curl` against `http://localhost:8080/api`:
  - A creates a project.
  - B does not see the project and gets `404` on its members.
  - A adds B as `Member`, and B now sees the project and its tasks, but gets `403` on adding a member and the UI shows read-only members.
  - A cannot remove A's own `Owner` role while A is the only owner.
  - A can delete the project, and B gets `403` when trying to delete it.

  Record the outcome in the change before archiving.

  **Outcome (2026-10-12, `docker compose` stack, checked with `curl` against `http://localhost:8080/api`):** all checks passed.
  - A creating a project returned `roles: ["Owner"]`.
  - Before B was added: B listed 0 projects, got `404` on the members, and got `404` creating a task in A's project.
  - A added B as `Member`: `201`.
  - After that, B saw the project with `roles: ["Member"]` and saw its task, but got `403` adding a member.
  - A removing A's own last `Owner` role: `400` with "A project must keep at least one owner."
  - B deleting the project: `403`. A deleting it: `200`, after which B listed 0 projects.
  - A request with no token: `401`.

  The read-only member list for non-owners was verified by the `project-members.spec.ts` cases, not by clicking through the UI. The test users and project were removed from the database afterwards.

## Workflow follow-up

- Run `/opsx:archive` once the change is implemented and verified, so the `project-access` spec is created and the `project-membership` and `project-members-ui` specs are updated.
