# Todo list 2: ce a rămas de făcut

Revizuit pe 2026-10-07, prin citirea codului din `backend/src`. Conține:

1. constatările din `/code-review` (partea A);
2. punctele nefăcute sau făcute parțial din [todo-list.md](todo-list.md), mutate aici (partea B). La punctele parțiale e notat ce există deja.

Ce e făcut stă în [todo-list.md](todo-list.md).

Legendă: `[ ]` de făcut · `[~]` parțial (restul de făcut e descris) · `[x]` făcut

---

# Partea A: constatări din `/code-review`

Review făcut pe modificările necomise de atunci (cod backend pentru roluri și membri, plus tab-ul Members din frontend). S-a citit doar codul.

## Severitate ridicată

- [ ] **Orice utilizator autentificat poate atribui sau scoate orice rol oricui.** `RoleEndpoints.cs:17`. De exemplu, `POST /api/account/users/{id}/roles` îi permite unui utilizator să-și dea singur rolul `Owner`.
  - Cauză: rutele cer doar un token valid, fără nicio verificare pe rol.
  - De făcut: restricționează rutele de administrare a rolurilor (de exemplu politică de autorizare pentru un rol de administrator) sau scoate-le până există autorizarea pe roluri.

- [ ] **Endpoint-urile de membri ale proiectului cer doar autentificare.** `ProjectMembershipEndpoints.cs:11`. Un utilizator care nu e membru poate să se adauge ca `Owner` la un proiect sau să elimine alți membri.
  - Cauză: nu există verificare că cel care apelează este membru sau `Owner` al proiectului.
  - De făcut: la adăugare și eliminare cere ca apelantul să fie `Owner` al proiectului, iar la listare să fie membru. Autorizarea a fost amânată explicit în `add-project-roles` (non-goal), deci trebuie planificată ca schimbare separată.
  - După ce autorizarea există, UI-ul din `ProjectMembers` ar trebui să ascundă acțiunile pentru cine nu le poate folosi (vezi riscul `canManage` din arhiva `add-project-members-ui`).

## Severitate scăzută

- [ ] **`catch (DbUpdateException)` întoarce 409 pentru orice eșec la salvare.** `ProjectMembershipService.cs:47`, același tipar în `RoleService.AssignToUserAsync`. O violare de cheie străină, de pildă după ștergerea concurentă a unui utilizator, ar apărea ca „deja există".
  - De făcut: tratează ca 409 doar încălcarea cheii primare compuse (verifică tipul erorii în excepția interioară sau reverifică existența rândului după eșec); pentru restul întoarce 404 sau 500.

- [ ] **`AddMember` întoarce 201 cu un `Location` care arată spre colecție.** `ProjectMembershipEndpoints.cs:26`. URI-ul nu identifică noul membru.
  - Frontend-ul tipează răspunsul POST ca `void`, deși corpul este un `MemberDto` (`project-member.service.ts`).
  - De făcut: ori întoarce un `Location` util, ori întoarce `200`/`204` fără `Location`; apoi aliniază tipul din `ProjectMemberService.addRole`.

---

# Partea B: nefăcut din `todo-list.md`

## ⚠️ Urgent

- [~] **Secretele rămân în istoricul Git.** Fișierele `appsettings*.json` curente nu mai conțin parole sau chei, dar valorile vechi (parola RDS și `TokenKey`) au fost comise, de exemplu în `25e1596`.
  - De făcut: schimbă parola RDS și cheia JWT, apoi mută-le în variabile de mediu / SSM pentru producție (în dezvoltare merg User Secrets). Rescrierea istoricului e opțională, rotirea nu.
- [ ] **Migrările rulează automat la pornire** (`Program.cs` apelează `ApplyMigrations()` la fiecare start, în toate mediile). Faza 7 cere să nu fie așa în producție.
- [~] **Soft delete incomplet.** `GetAllAsync` filtrează proiectele cu `IsDeleted`, dar `DeleteAsync` întoarce toate proiectele, inclusiv pe cele șterse (testul `Delete_SoftDeletes_AndReturnsEveryProject` fixează comportamentul), și nu verifică dacă proiectul e deja șters.

## Cerințe funcționale

- [~] **Proprietarul proiectului.** Rolul `Owner` și membrii pe proiect există, dar `Project` nu are `OwnerId` și creatorul nu devine automat `Owner` (vezi și autorizarea din partea A).
- [~] **Câmpurile task-ului.** Statusurile sunt tot `Created/InProgress/Completed/OnHold`, nu `Todo/InProgress/Done` (frontend-ul le afișează deja ca „To do” / „Done”).
- [~] **Listare / filtrare / update / delete.**
  - Proiecte: există listare, creare și ștergere (soft). Lipsesc update și filtrare.
  - Task-uri: există listare, citire după id, creare și update. Lipsesc delete și filtrare.

## Faza 0: Setup

- [ ] `.gitignore` pentru .NET e acum parțial: `bin/`, `obj/` și `**.db` sunt ignorate. Verifică restul (de exemplu `.vs/`, `*.user`) și mută regulile backend sub o secțiune clară.
- [ ] `Directory.Build.props`
- [ ] `.editorconfig` în backend (există doar în frontend)
- [ ] PostgreSQL local în Docker Compose

## Faza 1: Domain

- [ ] Clasa de bază `Entity` / `AuditableEntity` (`Id`, `CreatedAt`, `UpdatedAt`), acum repetate în fiecare entitate
- [~] Entitatea `Project` (fără `OwnerId`)
- [~] Enum-urile `Status` și `Priority` (valorile `Status` diferă de specificație)
- [ ] Metode în entități: `Complete()`, `AssignTo()`, `ChangePriority()` (entitățile au doar proprietăți)
- [ ] Reguli: un task `Done` nu poate fi reasignat; termenul nu poate fi în trecut la creare
- [ ] `DomainException`
- [ ] Teste unitare pentru reguli

## Faza 2: Application

Arhitectura aleasă (`restructure-clean-services`) folosește servicii în loc de MediatR. Punctele din a doua secțiune sunt deci înlocuite de decizie; confirmă dacă le păstrezi sau le ștergi.

**De făcut**

- [~] DTO-uri: proiectele expun entitatea `Project` direct (`CreateProject(Project project)`); lipsesc DTO-urile pentru proiecte
- [~] Validatori: doar DataAnnotations (`AddValidation()` în `Program.cs`); lipsesc validările pe proiecte și task-uri
- [ ] Interfețe lipsă: `ICurrentUserService` (acum `ClaimsPrincipal.GetUserId()` direct în endpoint-uri) și `IPasswordHasher` (hash-ul e în `AccountService`)
- [ ] Servicii pentru: update proiect, delete task, filtre
- [ ] Interogări cu paginare: proiecte; task-uri după proiect (filtre, sortare, paginare); „task-urile mele”
- [ ] Excepții / rezultate tipate: `ForbiddenException` sau echivalent în `ServiceResult` (acum doar not-found, conflict, invalid)
- [ ] Autorizare: doar proprietarul modifică sau șterge proiectul
- [~] Teste unitare: există pentru `AccountService`, `ProjectService`, `TokenService`; lipsesc `TaskService`, `RoleService`, `ProjectMembershipService`

**Înlocuite de decizia cu servicii (de confirmat)**

- [ ] MediatR și FluentValidation
- [ ] Commands și Queries (`CreateProject`, `AssignTask` etc.), acoperite acum de metodele serviciilor
- [ ] Pipeline behavior pentru validare și logging
- [ ] `IProjectRepository`, `ITaskRepository`, `IUserRepository`, `IUnitOfWork` (serviciile folosesc `IAppDbContext`)

## Faza 3: Infrastructure

- [ ] Configurări `IEntityTypeConfiguration<T>` (acum totul e în `OnModelCreating`)
- [ ] Nume în `snake_case`
- [ ] Repository-uri și `UnitOfWork` (vezi nota de mai sus)
- [~] `CreatedAt` / `UpdatedAt` automat: acum din inițializatori de proprietăți și din servicii; lipsește un mecanism central (de exemplu în `SaveChanges`)
- [~] Seed de date: există doar rolurile `Owner` și `Member`; lipsesc date de exemplu pentru dezvoltare
- [ ] `PasswordHasher`: `AccountService` folosește `HMACSHA512` pentru parole, ceea ce nu e potrivit; folosește PBKDF2 sau `PasswordHasher<T>` (cere și migrarea parolelor existente)
- [ ] Teste de integrare cu Testcontainers

## Faza 4: Api

- [ ] Middleware global pentru erori cu `ProblemDetails`
- [~] `ICurrentUserService` (vezi Faza 2)
- [~] OpenAPI + Swagger UI: active doar în development, fără schema Bearer pentru butonul „Authorize”
- [ ] Versionare `/api/v1/...`
- [ ] `/health` cu verificare Postgres
- [~] Secretele în afara codului (vezi „Urgent”)

## Faza 5: Teste

- [ ] Teste unitare Domain
- [~] Teste unitare Application (vezi Faza 2)
- [ ] Teste de integrare API (`WebApplicationFactory` + Testcontainers)
- [ ] Scenariile din specificație
- [~] Code coverage: `coverlet.collector` e referit în proiectul de teste, dar nu există raport sau prag

## Faza 6: Calitate și observabilitate

- [ ] Serilog
- [ ] Correlation ID
- [ ] Rate limiting pe autentificare
- [ ] Refresh token (opțional)
- [ ] Caching (opțional)

## Faza 7: Deploy

- [ ] Dockerfile multi-stage
- [ ] `docker-compose.yml` cu API + Postgres
- [ ] GitHub Actions: build + test (`staging.yml` e încă template-ul cu `echo Hello, world!`)
- [ ] GitHub Actions: deploy pe Elastic Beanstalk cu OIDC (acum deploy manual, vezi `aws-backend.md`)
- [ ] Migrări rulate controlat la deploy
- [ ] Secretele în SSM Parameter Store

## Extra

- [~] Soft delete: doar la `Project`, cu filtrarea incompletă (vezi „Urgent”)

## Pașii următori

1. Rezolvă secțiunea „Urgent” (rotirea secretelor și migrările la pornire).
2. Adaugă autorizarea pe proiect și `OwnerId` (partea A, severitate ridicată).
3. Mută regulile în entități și scrie testele de Domain.
4. Înlocuiește hash-ul parolelor și adaugă `ProblemDetails`.
5. Pune în funcțiune pipeline-ul de build, test și deploy.
