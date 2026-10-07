# Todo list: ce e făcut față de `project-task-manager-tasks.md`

Revizuit pe 2026-10-07, prin citirea codului din `backend/src` (nimic rulat pentru această evaluare).

Acest fișier ține doar punctele **făcute**. Tot ce e nefăcut sau făcut parțial a fost mutat în [todo-list-2.md](todo-list-2.md), cu ce s-a făcut deja din fiecare punct.

Față de evaluarea din 2026-10-05: codul e împărțit în `Domain` / `Application` / `Infrastructure` / `Api` (schimbarea `restructure-clean-services`), logica a ieșit din controllere în servicii, controllerele au devenit endpoint-uri minimale, iar roluri și membri pe proiect există în backend și în frontend.

Legendă: `[x]` făcut

## ⚠️ Urgent

- [x] **Producția nu mai forțează SQLite:** `AddInfrastructure` alege `UseSqlite` doar în Development și `UseNpgsql` altfel.
- [x] **`PJMSolution.slnx` conține proiectele:** cele 4 din `src/` și testele din `tests/`.
- [x] **Fișierele `appsettings*.json` nu mai conțin parole sau chei** (în dezvoltare se folosesc User Secrets).

## Cerințe funcționale

- [x] Înregistrare + autentificare
- [x] Proiectul conține task-uri (FK `TaskItem.ProjectId` → `Project`, cu relație EF și cascadă)
- [x] Task-ul poate fi atribuit unui utilizator (`AssignedId` este FK nullable către `AppUser`)

## Faza 0: Setup

- [x] Repo Git
- [x] Cele 4 proiecte (`Domain`, `Application`, `Infrastructure`, `Api`) și referințele dintre ele, protejate de un test de dependențe (`DependencyRuleTests`)
- [x] Proiect de teste xUnit (`ProjectManager.Application.Tests`)

## Faza 1: Domain

- [x] Entitatea `User` (`AppUser`)
- [x] Entitatea `TaskItem`
- [x] Entitățile pentru roluri și membri: `Role`, `UserRole`, `ProjectUserRole`

## Faza 2: Application

- [x] `AddApplication()`
- [x] Serviciile din `Application` în spatele interfețelor, fără tipuri HTTP: `AccountService`, `ProjectService`, `TaskService`, `RoleService`, `ProjectMembershipService`
- [x] `ServiceResult` pentru rezultatele not-found / conflict / invalid
- [x] DTO-uri pentru cont, task-uri, roluri și membri

## Faza 3: Infrastructure

- [x] `Npgsql.EntityFrameworkCore.PostgreSQL` și `Microsoft.EntityFrameworkCore.Design`
- [x] `AppDbContext` (`AppDb`) în Infrastructure, expus prin `IAppDbContext`
- [x] Migrări (7), mutate în `Infrastructure/Persistence/Migrations`
- [x] `JwtTokenGenerator` (`TokenService`)
- [x] `AddInfrastructure(configuration, environment)`
- [x] Seed pentru rolurile `Owner` și `Member` (`HasData`)

## Faza 4: Api

- [x] `Program.cs` cu `AddApplication()` și `AddInfrastructure()`
- [x] Endpoint-urile (`Account`, `Projects`, `TaskItem`, `Roles`, `ProjectMembership`) doar traduc HTTP și apelează un serviciu, fără acces la date
- [x] JWT Bearer și autorizare pe endpoint-uri (inclusiv `GET /api/projects`)
- [x] CORS
- [x] Swagger UI în development
- [x] Namespace-ul `MyApp.Namespace` din template a dispărut odată cu controllerele

## Faza 5: Teste

- [x] Teste unitare pentru `AccountService`, `ProjectService` și `TokenService`, plus testul regulii de dependențe

## Faza 7: Deploy

- [x] Bază de date RDS Postgres

## Frontend (în afara specificației backend)

- [x] Tab-urile Tasks / Members pe pagina proiectului, cu adăugare de membri și editare de roluri
- [x] Atribuirea unui utilizator pe task, din editarea task-ului
