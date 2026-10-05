# Todo list: status față de `project-task-manager-tasks.md`

Evaluare făcută pe 2026-10-05, doar prin citirea codului (nimic rulat).
Progres estimat: **~15–20%**. API-ul funcționează (login, proiecte, task-uri, JWT, deploy manual pe AWS), dar e un singur proiect `WebAPI` cu toată logica în controllere. Clean Architecture nu e implementată.

Legendă: `[x]` făcut · `[~]` parțial · `[ ]` lipsă

## ⚠️ Urgent

- [ ] **Secrete în Git:** parola RDS (`appsettings.json`, `appsettings.Development.json`) și `TokenKey` sunt comise. Schimbă parola RDS și cheia JWT, apoi mută-le în User Secrets / variabile de mediu / SSM.
- [ ] **Producția folosește SQLite:** `Program.cs` apelează mereu `UseSqlite(...)`, dar `DefaultConnection` din `appsettings.json` (producție) e un connection string de Postgres.
- [ ] **Migrările rulează automat la pornire** (`Program.cs`, `Database.Migrate()`). Faza 7 cere să nu fie așa în producție.
- [ ] **Soft delete incomplet:** `GetProjects` și `DeleteProject` întorc și proiectele cu `IsDeleted = true`.
- [ ] **`PJMSolution.slnx` nu conține `WebAPI`**, doar `PJMConsoleApp`.

## Cerințe funcționale

- [x] Înregistrare + autentificare
- [ ] Proprietarul proiectului (`Project` nu are `OwnerId`)
- [~] Proiectul conține task-uri (există `ProjectId`, dar fără FK sau relație EF)
- [~] Câmpurile task-ului (statusurile sunt `Created/InProgress/Completed/OnHold` în loc de `Todo/InProgress/Done`)
- [~] Listare / filtrare / update / delete (lipsesc update și filtrare la proiecte, delete și filtrare la task-uri)

## Faza 0: Setup (1/7)

- [x] Repo Git
- [ ] `.gitignore` pentru .NET (acoperă doar frontend-ul, de aceea `pjm.db` e urmărit de Git)
- [ ] Cele 4 proiecte (`Domain`, `Application`, `Infrastructure`, `Api`) și referințele dintre ele
- [ ] `Directory.Build.props`
- [ ] `.editorconfig` în backend (există doar în frontend)
- [ ] Proiectele de teste (xUnit)
- [ ] PostgreSQL local în Docker Compose

## Faza 1: Domain (~2/9)

- [ ] Clasa de bază `Entity` / `AuditableEntity`
- [x] Entitatea `User` (`AppUser`)
- [~] Entitatea `Project` (fără `OwnerId`)
- [x] Entitatea `TaskItem`
- [~] Enum-urile (`Status`, `Priority`; valorile diferă de specificație)
- [ ] Metode în entități: `Complete()`, `AssignTo()`, `ChangePriority()` (acum entitățile au doar proprietăți)
- [ ] Reguli: un task `Done` nu poate fi reasignat; termenul nu poate fi în trecut la creare
- [ ] `DomainException`
- [ ] Teste unitare pentru reguli

## Faza 2: Application (~1/16)

- [ ] MediatR și FluentValidation
- [ ] Interfețele: `IProjectRepository`, `ITaskRepository`, `IUserRepository`, `IUnitOfWork`, `ICurrentUserService`, `IJwtTokenGenerator`, `IPasswordHasher` (există doar `ITokenService`)
- [~] DTO-uri (doar pentru cont; proiectele și task-urile expun entitățile direct)
- [ ] Commands: `CreateProject`, `UpdateProject`, `DeleteProject`
- [ ] Commands: `CreateTask`, `UpdateTask`, `ChangeTaskStatus`, `AssignTask`, `DeleteTask`
- [~] Commands: `RegisterUser`, `LoginUser` (există ca acțiuni în controller)
- [ ] Queries: `GetProjects` (cu paginare), `GetProjectById`
- [ ] Queries: `GetTasksByProject` (filtre, sortare, paginare)
- [ ] Queries: `GetMyTasks`
- [~] Validatori (doar DataAnnotations în `RegisterDto` și `AccountDto`)
- [ ] Pipeline behavior pentru validare
- [ ] Pipeline behavior pentru logging (opțional)
- [ ] `NotFoundException`, `ValidationException`, `ForbiddenException`
- [ ] Autorizare: doar proprietarul modifică/șterge proiectul
- [ ] `AddApplication()`
- [ ] Teste unitare pentru handlere

## Faza 3: Infrastructure (~4/11)

- [x] `Npgsql.EntityFrameworkCore.PostgreSQL` și `Microsoft.EntityFrameworkCore.Design`
- [x] `AppDbContext` (`AppDb`)
- [ ] Configurări `IEntityTypeConfiguration<T>`
- [ ] Nume în `snake_case`
- [ ] Repository-uri și `UnitOfWork`
- [~] `CreatedAt` / `UpdatedAt` automat (acum din inițializatori sau manual în controller)
- [x] Migrări (3)
- [ ] Seed de date
- [~] `JwtTokenGenerator` (`TokenService` există) și `PasswordHasher` (HMACSHA512 direct în controller; HMAC nu e potrivit pentru parole, folosește PBKDF2 sau `PasswordHasher<T>`)
- [ ] `AddInfrastructure(configuration)` (există doar `AddIdentityServices`)
- [ ] Teste de integrare cu Testcontainers

## Faza 4: Api (~3/11)

- [~] `Program.cs` cu `AddApplication()` și `AddInfrastructure()`
- [x] Controllere (`AccountController`, `ProjectsController`, `TaskItemController`)
- [ ] Controllerele trimit doar comenzi prin `IMediator` (acum conțin logica de business)
- [x] JWT Bearer și `[Authorize]`
- [~] `ICurrentUserService` (doar o metodă privată `CurrentUser()` în `AccountController`)
- [ ] Middleware global pentru erori cu `ProblemDetails`
- [~] OpenAPI (activ doar în development, fără UI și fără Bearer)
- [x] CORS
- [ ] Versionare `/api/v1/...`
- [ ] `/health` cu verificare Postgres
- [ ] Secretele în afara codului
- [ ] Detaliu: `TaskItemController` e în namespace-ul `MyApp.Namespace`, rămas din template

## Faza 5: Teste (0/5)

- [ ] Teste unitare Domain
- [ ] Teste unitare Application
- [ ] Teste de integrare API (`WebApplicationFactory` + Testcontainers)
- [ ] Scenariile din specificație
- [ ] Code coverage (Coverlet)

## Faza 6: Calitate și observabilitate (0/5)

- [ ] Serilog
- [ ] Correlation ID
- [ ] Rate limiting pe autentificare
- [ ] Refresh token (opțional)
- [ ] Caching (opțional)

## Faza 7: Deploy (~1/7)

- [ ] Dockerfile multi-stage
- [ ] `docker-compose.yml` cu API + Postgres
- [ ] GitHub Actions: build + test (`staging.yml` e încă template-ul cu `echo Hello, world!`)
- [ ] GitHub Actions: deploy pe Elastic Beanstalk cu OIDC (acum deploy manual, vezi `aws-backend.md`)
- [x] Bază de date RDS Postgres
- [ ] Migrări rulate controlat la deploy
- [ ] Secretele în SSM Parameter Store

## Extra

- [~] Soft delete (doar la `Project`, fără filtrare)

## Pașii următori

1. Rezolvă secțiunea „Urgent”.
2. Adaugă `OwnerId` pe `Project`, verificarea de owner și relațiile EF (FK).
3. Împarte codul în Domain / Application / Infrastructure / Api și mută logica din controllere.
4. Mută regulile în entități și scrie primele teste unitare.
