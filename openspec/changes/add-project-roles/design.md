# Design

## Context

The backend is a single ASP.NET Core project (`backend/WebAPI`) using Minimal API endpoints, EF Core with SQLite in development and PostgreSQL in production, and JWT authentication. `TaskItem` already follows the pattern this change reuses: endpoints in `Endpoints/`, logic in a service behind an interface (`ITaskService` / `TaskService`), responses as DTOs under `Models/DTOs/`, never as entities (entities would form object cycles and `AppUser` carries password hash and salt). Migrations are applied at startup. `Project` has soft delete (`IsDeleted`); nothing enforces ownership yet. See proposal.md for motivation and the specs for required behavior.

## Goals / Non-Goals

**Goals:**
- Model roles, user roles and project membership so that they are consistent at the database level (no duplicates, no orphans).
- Follow the existing endpoint, service and DTO structure so the new code looks like `TaskItem`.

**Non-Goals:**
- Role-based authorization. Endpoints only require a valid token.
- An endpoint to create, rename or delete roles; the catalog is fixed by seed data.
- `Project.OwnerId` and auto-adding a project's creator as `Owner`.
- Frontend work.

## Decisions

**1. Composite primary keys for the two join tables.** `UserRole` is keyed by (UserId, RoleId) and `ProjectUserRole` by (ProjectId, UserId, RoleId). The key itself rejects duplicates, so the "conflict" scenarios are enforced by the database and need no separate unique index or surrogate `Id`. `ProjectUserRole` also carries `CreatedAt`. Alternative considered: a surrogate `Id` plus a unique index; it adds a column nobody reads and a second way to be inconsistent.

**2. One row per (project, user, role).** A user with two roles in one project has two rows. This is what "multiple roles" in the request requires and makes "remove one role" a single-row delete. Alternative: one row per (project, user) with a single `RoleId`, which would forbid multiple roles in a project, so it was rejected. Listing members groups rows by user in the service.

**3. Roles are seeded with `HasData`, fixed ids.** `Owner` = 1, `Member` = 2, inserted by the migration, so existing databases get them on upgrade (spec: roles exist on a database with data). `Role.Name` has a unique index. Alternative: seed in code at startup; rejected because it hides schema-level data from migrations and runs on every start.

**4. Delete behavior.** `ProjectUserRole` and `UserRole` cascade from `Project` and `AppUser` (a membership is meaningless without either side) and restrict on `Role` (a role in use cannot be removed). Projects are soft-deleted, so the project cascade only matters if a hard delete is ever added. User deletion is not supported today.

**5. Endpoints.** All under `RequireAuthorization()`:
- `GET /api/roles`
- `GET /api/account/users/{userId}/roles`, `POST /api/account/users/{userId}/roles` (body: roleId), `DELETE /api/account/users/{userId}/roles/{roleId}`
- `GET /api/projects/{projectId}/members`, `POST /api/projects/{projectId}/members` (body: userId, roleId), `DELETE /api/projects/{projectId}/members/{userId}/roles/{roleId}`, `DELETE /api/projects/{projectId}/members/{userId}`

User-role routes sit under `/api/account` because that is where users are already exposed. Member routes nest under the project they belong to. POST returns `201` on success, `404` for an unknown user, role or project (including a soft-deleted project), `409` for a duplicate.

**6. Services return an outcome, not `IResult`.** `IRoleService` and `IProjectMembershipService` return a small enum (`Ok`, `NotFound`, `Conflict`) or `null`, and the endpoint maps it to the HTTP status. This matches `TaskService` (which returns `null` for not found) and keeps services free of HTTP types. Duplicate detection checks existence first and also treats a `DbUpdateException` from the composite key as `Conflict`, so two concurrent identical requests cannot both succeed.

**7. DTOs.** `RoleDto` (id, name); `MemberDto` (user id, first name, last name, email, roles) built with a projection that selects only those fields, so password data cannot leak. No entity is serialized.

## Risks / Trade-offs

- [Existing migrations are generated on SQLite and carry SQLite-specific annotations; the new `Roles` table has an auto-increment `Id`] → Verify the migration against PostgreSQL before deploying, and check that `HasData` ids do not clash with later inserts there (acceptable because there is no create-role endpoint).
- [Any authenticated user can add members and roles, including to projects they have nothing to do with] → Accepted for this change and stated in the spec; role-based authorization is the follow-up that closes it.
- [`Project` has no owner, so `Owner` is just a role name with no enforced meaning] → The follow-up change defines what `Owner` may do and how a project gets its first owner.
- [Granting a user a role in `UserRole` has no effect on anything yet] → It is a prerequisite and has no behavior until authorization uses it.

## Migration Plan

1. Add the three models and configuration, then generate one migration (`dotnet ef migrations add AddProjectRoles`).
2. Apply it through the existing startup `Migrate()` in development. For production, apply as for other migrations; it only adds tables and two rows, so existing data is not touched.
3. Rollback: the migration's `Down` drops the three tables; no existing table is altered.
