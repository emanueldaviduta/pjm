# Tasks

## 1. Data model and migration

- [x] 1.1 Add `Role` (id, unique name), `UserRole` (user id, role id) and `ProjectUserRole` (project id, user id, role id, created at) entities under `backend/src/ProjectManager.Domain/Entities/`; verify `dotnet build` succeeds
- [x] 1.2 Add the three `DbSet`s to `IAppDbContext` (Application) and `AppDb` (Infrastructure) and configure composite primary keys, relationships, the unique index on `Role.Name`, and delete behavior (cascade from `Project` and `AppUser`, restrict on `Role`) per design decisions 1 and 4; verify `dotnet ef migrations has-pending-model-changes` reports changes only for these tables
- [x] 1.3 Seed `Owner` (id 1) and `Member` (id 2) with `HasData`; generate one migration `AddProjectRoles` in Infrastructure (`dotnet ef ... --project src/ProjectManager.Infrastructure --startup-project src/ProjectManager.Api`); verify its `Up` creates the three tables and inserts the two roles, and its `Down` drops the three tables
- [x] 1.4 Apply the migration to a copy of `pjm.db`; verify the roles table holds exactly `Owner` and `Member`, existing users, projects and tasks keep their row counts, and `PRAGMA foreign_key_check` returns nothing
- [x] 1.5 Review the generated migration for PostgreSQL compatibility (auto-increment annotation on `Roles.Id`, seed ids) and note any adjustment needed; verify by reading the migration against the design's migration risk

## 2. Role catalog and user roles

- [x] 2.1 Add `RoleDto` and a request DTO for assigning a role under `ProjectManager.Application/DTOs/`; verify the project builds
- [x] 2.2 Add `IRoleService` and `RoleService` in Application (list roles; list, assign and remove a user's roles, returning `Ok`/`NotFound`/`Conflict` and treating a composite-key violation as `Conflict`); verify the build and register the service in `AddApplication()`
- [x] 2.3 Add `Endpoints/RoleEndpoints.cs` in the Api project with `GET /api/roles` and the three `/api/account/users/{userId}/roles` routes, all requiring authorization, and map it in `Program.cs` (the endpoint only calls the service); verify with the running API: `GET /api/roles` lists `Owner` and `Member` and returns 401 without a token
- [x] 2.4 Exercise the role-management scenarios against the running API (assign a second role, assign twice gives 409, unknown user or role gives 404, list, remove one role, remove a role the user lacks gives 404) and remove the test data afterwards; verify every scenario in `specs/role-management/spec.md` behaves as written

## 3. Project membership

- [x] 3.1 Add `MemberDto` (user id, names, email, roles) and the add-member request DTO under `ProjectManager.Application/DTOs/`; verify the project builds
- [x] 3.2 Add `IProjectMembershipService` and `ProjectMembershipService` in Application (list members grouped per user through a projection that excludes password data; add a role; remove one role; remove a member; unknown or soft-deleted project gives `NotFound`; duplicate gives `Conflict`); verify the build and register the service in `AddApplication()`
- [x] 3.3 Add `Endpoints/ProjectMembershipEndpoints.cs` in the Api project with the four `/api/projects/{projectId}/members` routes, all requiring authorization, and map it in `Program.cs`; verify the routes appear in `/openapi/v1.json`
- [x] 3.4 Exercise the project-membership scenarios against the running API (two users in one project, one user with two roles, same user in two projects with different roles, duplicate gives 409, unknown user, role or project gives 404, soft-deleted project gives 404, list shows no password fields, remove one role, remove a member, remove something absent gives 404, no token gives 401) and remove the test data afterwards; verify every scenario in `specs/project-membership/spec.md` behaves as written

## 4. Integration

- [x] 4.1 Add example requests for the new endpoints to `backend/src/ProjectManager.Api/WebAPI.http`; verify each request runs against the local API and returns the documented status
- [x] 4.2 Run the full solution (`dotnet build` and `dotnet test` on `backend/PJMSolution.slnx`, including the dependency-rule test) and the API: confirm existing `/api/account`, `/api/projects` and `/api/taskitem` calls still behave as before and the frontend type check (`npx tsc --noEmit -p tsconfig.app.json` in `frontend`) still passes; verify no existing endpoint or DTO changed

## Workflow follow-up

- Archive the change once reviewed.
- Role-based authorization (what `Owner` and `Member` may do) and `Project.OwnerId` are separate follow-up changes.
