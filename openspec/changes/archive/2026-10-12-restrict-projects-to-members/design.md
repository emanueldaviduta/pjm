# Design

## Context

See proposal.md for the motivation. The current state that shapes the approach:

- **Membership data exists but nothing enforces it.**
  - `ProjectUserRole (ProjectId, UserId, RoleId)` stores membership, and `Owner` (id 1) and `Member` (id 2) are seeded.
  - `ProjectService`, `ProjectMembershipService` and `TaskService` all query without any notion of the caller.
- **The caller's id is available.** The token carries it as `ClaimTypes.NameIdentifier`. `ClaimsPrincipal.GetUserId()` in `ServiceResultExtensions` already reads it, and `AccountEndpoints` uses it.
- **Layering rules.** The `backend-architecture` spec says endpoints only translate HTTP, and use cases live in Application services behind interfaces.
- **Service results.** Services report outcomes through `ServiceResult` / `ServiceStatus`, which the Api maps to HTTP. `Forbidden` does not exist yet.
- **Project endpoints use the entity directly.** `POST /api/projects` binds the `Project` entity, `id` included, and the endpoints return entities.
- **The frontend has the project in hand.** `ProjectDetail` finds its project in `ProjectService.projects()` and shows "Project not found" when the project is missing. The page renders `<app-project-members [projectId]>`. There is no project delete action in the UI.

## Goals / Non-Goals

**Goals:**
- Each access rule lives in one place in the Application layer and is unit-testable with the existing in-memory SQLite test setup.
- A non-member gets the same response as for a project that does not exist, as specified in `project-access`.

**Non-Goals:**
- Restricting task assignees to project members. `AssignedId` may still name any user.
- A global admin or super-user who sees every project.
- Restricting `GET /api/account/users`. The user directory is needed to add members.
- Roles beyond `Owner` and `Member`, or per-task permissions.
- Assigning owners to existing projects that have no members (see Migration Plan).
- A project-delete action in the UI.

## Decisions

### 1. Authorization is checked in Application services, with the caller's id passed explicitly
Every affected service method gets an `int userId` parameter. Endpoints read it with `principal.GetUserId()` and answer `401` when the claim is missing, exactly like `AccountEndpoints`.
- *Alternative: resource-based ASP.NET authorization handlers.* Rejected. The rules need database lookups per project, which would put data access in the Api layer and split each rule from the use case it guards.
- *Alternative: an injected `ICurrentUser` built on `IHttpContextAccessor`.* Rejected for now. Passing the id explicitly keeps services free of HTTP, makes the dependency visible in signatures, and lets tests pass any id without mocking.

### 2. One shared access helper
A small static helper in `Application/Common`, for example `ProjectAccess`, works over `IAppDbContext` and provides:
- `VisibleProjects(userId)`: an `IQueryable<Project>` of non-deleted projects where the user has a `ProjectUserRole`. The project list and the task filter both use it.
- `RolesInProjectAsync(projectId, userId)`: the role names the caller holds in a non-deleted project. An empty set means "not a member, or the project does not exist". Services turn an empty set into `NotFound`, and a set without `Owner` into `Forbidden` on owner-only operations.

Owner checks compare against a single `RoleNames.Owner = "Owner"` constant. The role name is unique and is what the spec talks about, whereas the seeded id is incidental.

**Order of checks in a project-scoped operation:**
1. Is the caller a member of the project? If not, answer `NotFound`.
2. Is the caller an `Owner`, if the operation requires one? If not, answer `Forbidden`.
3. Run the existing validations: target user, role, duplicate, and so on.

Checking membership first means non-members learn nothing, not even whether a user id exists.

### 3. `ServiceStatus.Forbidden` maps to 403
Add `Forbidden` to `ServiceStatus`, add `Forbidden()` factories to both result records, and map it in `ServiceResultExtensions.FromFailure` to `TypedResults.StatusCode(403)`.
- *Alternative: `TypedResults.Forbid()`.* Rejected because it goes through the authentication scheme's forbid handler. A plain 403 is simpler and needs no scheme.

### 4. Last-owner protection answers `Invalid` (400) with a message
In `RemoveRoleAsync`, when the role being removed is `Owner`, and in `RemoveMemberAsync`, when the target holds `Owner`, count the other owners of the project first. If there are none, return `Invalid("A project must keep at least one owner.")`.

The frontend already shows plain-text 400 bodies through `apiErrorMessage`. `409 Conflict` was considered, but it is used for duplicates and the UI maps it to "already has this role".

### 5. `ProjectDto` with the caller's roles; a dedicated create request
- **Responses.** Project endpoints return `ProjectDto { Id, Name, Description, CreatedAt, UpdatedAt, IsDeleted, Roles: string[] }`. The fields match the current JSON, so existing frontend code keeps working, and `Roles` is added. The DTO is built by projection, which also keeps the `Tasks` navigation out of responses.
- **Create.** `POST /api/projects` binds a `CreateProjectRequest { Name, Description }` instead of the entity. A client therefore cannot send an `id` or `isDeleted`, and the odd "id ≠ 0 means update" branch in `CreateAsync` goes away.
- **Atomic save.** The project and the creator's `Owner` `ProjectUserRole` are added through the navigation property and saved with one `SaveChangesAsync`. Both rows are stored or neither is.
- **Delete response.** `DeleteAsync` returns the caller's visible projects as DTOs, not every project including deleted ones.

### 6. Tasks are filtered by project membership
- **Listing.** `TaskService.Query(userId)` starts from tasks whose project is in `VisibleProjects(userId)`, which also drops tasks of deleted projects.
- **Read and update.** `GetByIdAsync` and `UpdateAsync` return `null` for a task outside the caller's projects. Endpoints already map `null` to 404.
- **Create.** `CreateAsync` returns `TaskItemDto?`, with `null` when the target project is not visible to the caller, mapped to 404. This keeps the existing nullable style instead of introducing `ServiceResult` in `TaskService`.

### 7. Frontend reads ownership from the project
- **Model.** `Project` gains `roles?: string[]`, and an `isOwner(project)` helper is added next to `projectKey`.
- **Wiring.** `ProjectDetail` passes `[canManage]="isOwner(project)"` to `ProjectMembers`, which hides the add, change-role and remove controls when `canManage` is false.
- **After a change.** When a membership change succeeds, `ProjectService.load(true)` reloads projects, so losing your own `Owner` role, or leaving the project, updates the controls or removes the project.
- **Errors.** `ProjectMembers.fail()` handles `403` with "Only project owners can manage members", and shows the server's text for `400`.

## Risks / Trade-offs

- **[Race on last-owner removal]** Two owners removing each other at the same moment could both pass the count check and leave the project with no owner. → Accepted for now: the window is small and the result is recoverable through the database. If it matters later, run the check and the delete in a serializable transaction.
- **[Existing projects disappear]** Projects created before this change have no members and become invisible. → Acceptable because the Docker database was just recreated. Locally, add an owner row by hand if needed (see Migration Plan).
- **[Breaking API change]** Clients that relied on seeing everything break. → The only client is the web app in this repo, and it is updated in the same change.
- **[Extra query per request]** Each project-scoped call loads the caller's roles first. → One indexed lookup on the `ProjectUserRoles` composite key, which is negligible.
- **[Stale ownership in the UI]** `ProjectService` caches projects. If someone else changes your roles, the controls stay stale until reload. → The server still enforces the rules, and the UI reports `403` and refreshes.

## Migration Plan

- No schema change and no EF migration.
- Deploy the backend and frontend together with `docker compose up -d --build`.
- To keep an old project visible, insert an owner row:
  `INSERT INTO "ProjectUserRoles" ("ProjectId","UserId","RoleId") VALUES (<project>, <user>, 1);`
- Rollback: redeploy the previous images. No data changes need undoing, because creator-owner rows are valid under the old code too.
