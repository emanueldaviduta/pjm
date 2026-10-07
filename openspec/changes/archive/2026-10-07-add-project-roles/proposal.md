# Proposal

## Why

Today every signed-in user sees and edits every project, and a project has no notion of who belongs to it or in what capacity (`Project` has no owner or members, and `TaskItem.AssignedId` can point at any user). The plan in `project-task-manager-tasks.md` already calls for project members with roles (`Owner`, `Member`) and for owner-only edits. This change adds the data model for that: a catalog of roles, roles held by users, and per-project membership with a role. It is the prerequisite for any later authorization rules.

## What Changes

- Add a `Role` table: a named, unique role (for example `Owner`, `Member`).
- Let a user hold multiple roles through a `UserRole` join table (many-to-many between `AppUser` and `Role`).
- Add a `ProjectUserRole` table that assigns users to a project together with their role in that project. A project has many users, and a user can hold more than one role in the same project, so a row is unique per (project, user, role).
- Seed the default roles `Owner` and `Member`.
- Expose API endpoints to list roles, manage a user's roles, and add, list and remove a project's members with their roles.
- One EF Core migration that creates the three tables; existing data is untouched.

Not in scope (kept for later changes): enforcing permissions based on these roles (for example "only the owner can delete a project"), an `OwnerId` column on `Project`, the Angular UI, and automatically making the creator of a project its first member.

## Capabilities

### New Capabilities
- `role-management`: the catalog of roles and the assignment of multiple roles to a user.
- `project-membership`: assigning multiple users to a project, each with one or more roles in that project.

### Modified Capabilities

None. The project has no existing specs under `openspec/specs/`.

## Impact

- **Backend (`backend/src`)**: new entities `Role`, `UserRole`, `ProjectUserRole` in Domain; the three `DbSet`s on `IAppDbContext` (Application) and `AppDb` (Infrastructure) plus relationship configuration, seed data and the new migration in Infrastructure; new services, interfaces and DTOs in Application following the `TaskItem` pattern, registered in `AddApplication()`; new endpoint files in the Api project, mapped in `Program.cs`.
- **Database**: three new tables with foreign keys to `AppUsers`, `Projects` and `Roles`. Dev uses SQLite and production uses PostgreSQL, so the migration must work on both.
- **Existing behavior**: unchanged. No existing endpoint, DTO or frontend call is modified.
- **Assumptions recorded here** (interpretation of the request, easy to change): "associate multiple roles" is read as a user-to-role many-to-many, and a user may hold several roles in one project.
