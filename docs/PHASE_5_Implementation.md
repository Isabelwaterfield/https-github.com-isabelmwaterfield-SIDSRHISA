# PHASE 5 — Implementation
## Secure Integrated Drone System for Remote Healthcare (SIDSRH)
**Isabel Waterfield | 402410147 | Supervisor: Royal Goronga | 2026**

Aligned to Richfield Faculty IT Project Guidelines — Implementation Phase (25%).  
Documentation 50% + Presentation 50% of overall project assessment.

---

### 5.1 Introduction

Phase 5 implements the Phase 4 design as a working ASP.NET Core 8 web application with Entity Framework Core, JWT authentication, role-based menus, Haversine distance rules (urgent ≤15 km, routine ≤40 km), multi-factor login for every account, dual parcel codes, reporting, and backup routines.  

All drone flights and medical deliveries are **software-simulated**. The system does not control physical aircraft. This satisfies the faculty rule that the project must include software development and may not be theory-only or physical installation only.

This chapter covers: development environment, coding standards, key modules, error handling and validation, testing strategy and results, installation, security measures in practice, cost notes, sample reports, future scope, and glossary.

---

### 5.2 Coding

#### 5.2.1 Development environment
| Item | Choice |
|------|--------|
| IDE | Visual Studio 2022/2026 or VS Code |
| Runtime | .NET 8 SDK |
| Language | C# 12 |
| Web framework | ASP.NET Core Web API + static SPA (wwwroot) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (offline demo) / SQL Server LocalDB HealthcareDB / MySQL |
| Auth | JWT Bearer + BCrypt password hashes + MFA OTP (6 minutes) |
| Maps | Leaflet + OpenStreetMap tiles |

#### 5.2.2 Coding standards
- Controllers thin; business rules in `BusinessRules` and services.
- Entities match domain tables (Users, Locations, Drones, Deliveries, …).
- Parameterised EF queries (no concatenated SQL).
- Meaningful activity log entries for security-relevant actions.
- Consistent JSON APIs under `/api/...`.
- UI role menus match Phase 4 Table 4.7.1 structure.

#### 5.2.3 Modules implemented
| Module | Implementation |
|--------|----------------|
| M1 Auth / Users | Login, register (patients), MFA, forgot/reset password, user create |
| M2 Dashboard | KPI counts, weather status |
| M3 Drones | List, maintenance toggle, battery |
| M4 Deliveries / Supplies | Create, assign, dispatch, complete with codes; stock reservation |
| M5 Staff / Clinics / Equipment | CRUD-style list and staff delete; clinics; equipment |
| M6 Emergency simulation | ≤15 km Haversine scenario run |
| M7 Reporting / Activity log | CSV exports, patient receipt HTML, activity trail |
| M8 Extended ops | Patient requests, dispense+verify, stations, appointments, backup |

#### 5.2.4 Error handling and validation
- Invalid login increments failed count; lockout after 5 attempts.
- Distance checks against 15/40 km and rain rule.
- Stock checked before delivery/dispense.
- Arrival code must match before complete.
- MFA OTP expires after 6 minutes; one-time use.
- API returns clear JSON error messages for UI alerts.

#### 5.2.5 Parameters and security
- JWT carries user id, email, role.
- Role menus hide unauthorised pages client-side; sensitive writes still require authenticated session.
- BCrypt for passwords and OTP hashes.
- Backup folder under application root; SQLite file copy + instructions for SQL Server/MySQL.

---

### 5.3 Testing

#### 5.3.1 Strategy
| Level | Approach |
|-------|----------|
| Unit | BusinessRules Haversine, fee slot, ETA helpers |
| Integration | Auth → dashboard → create delivery → assign → dispatch → complete |
| System | Full role walkthroughs (Admin, Manager, Nurse, Tech, Patient) |
| UAT-style | Patient request → accept → track codes → receipt |

#### 5.3.2 Sample test cases (executed)

| ID | Case | Steps | Expected | Result |
|----|------|-------|----------|--------|
| T01 | MFA login all roles | Login each seed user | 6-digit code required; token after OTP | Pass |
| T02 | Wrong password lockout | Fail 5 times | Account Locked | Pass |
| T03 | Patient register | Register new email | Patient role + MFA on | Pass |
| T04 | Password reset | Forgot → OTP → new password | Login with new password | Pass |
| T05 | Urgent distance | Request >15 km urgent | Rejected | Pass |
| T06 | Routine 40 km | Valid routine distance | Accepted | Pass |
| T07 | Rain rule | Set weather Rain; long delivery | Blocked >15 km | Pass |
| T08 | Delivery lifecycle | Create→Assign→Dispatch→Complete | Codes issued; status Completed | Pass |
| T09 | Wrong arrival code | Complete with bad code | Rejected | Pass |
| T10 | Dispense verify | Nurse dispense; manager verify | Status Verified | Pass |
| T11 | Simulation ≤15 km | Run SCN001 | Completed or Failed with reason | Pass |
| T12 | Reports | Download CSVs; open receipt | Files/HTML returned | Pass |
| T13 | Backup SQLite | POST /api/Backup/sqlite | File in Backups/ | Pass |
| T14 | Role menus | Login as Patient vs Admin | Different nav items | Pass |
| T15 | Activity log | Login + create delivery | LOG entries present | Pass |

#### 5.3.3 Debugging and improvements during Phase 5
- Application Control on campus PCs blocked `.exe` → set `UseAppHost=false` and run via `dotnet` DLL host.
- Schema mismatch on legacy HealthcareDB → multi-provider config (Sqlite default for reliable demo; SqlServer/MySql switch).
- MFA extended to **every** account for uniform security demonstration.
- Backup API added for offline SQLite and documented for SQL Server/MySQL.

---

### 5.4 System testing evaluation

All critical paths in the table above passed in the SQLite offline configuration. Role separation is visible in menus and in who can create users, verify dispense, or open full reports. Financial fields show R0.00 for in-slot free deliveries and R10 outside preferred slots. Emergency simulation never exceeds the 15 km planning rule without a Failed result message.

**Screenshot checklist (paste into your printed Phase 5):**
1. Welcome screen  
2. Login + MFA code prompt  
3. Admin dashboard KPIs  
4. Live map with locations/drones  
5. Delivery list with dispatch/arrival codes  
6. Patient request accept  
7. Emergency simulation result  
8. Patient receipt  
9. Activity log  
10. Backup success response  

---

### 5.5 Installation

1. Install .NET 8 SDK.  
2. Unzip package to `C:\SIDSRH_PHASE5_FINAL` (avoid spaces if possible).  
3. PowerShell:  
```powershell
cd C:\SIDSRH_PHASE5_FINAL\SIDSRH.Api
Get-ChildItem -Recurse | Unblock-File
dotnet restore
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5080
```
4. Browse to `http://localhost:5080`.  
5. Optional: set `Database:Provider` to `SqlServer` and ensure LocalDB HealthcareDB exists.

**HealthcareDB (SQL Server):**  
`Server=(localdb)\mssqllocaldb;Database=HealthcareDB;Trusted_Connection=True;TrustServerCertificate=True`

EF `EnsureCreated` + seed runs on first start when the database is empty.

---

### 5.6 System security measures (guideline item 7)

| Control | How implemented |
|---------|-----------------|
| Password storage | BCrypt hashes |
| MFA | 6-minute OTP for every user |
| Session | JWT, configurable expiry |
| Authorisation | Role claims + role-specific menus |
| Audit | ActivityLogs with user and type |
| Lockout | After 5 failed logins |
| Input | EF parameter binding |
| Transport | HTTPS recommended in production |
| POPIA-minded | Minimal patient fields; brief receipt reason only |

---

### 5.7 Cost estimation (guideline item 8)

Academic build cost is primarily student time. Indicative model (COCOMO-style simple estimate): ~4–6 person-months student effort across Phases 1–5. Cash costs for demo: zero (SQLite, OSS stacks). Production would add hosting, SMS OTP gateway, and support — outside academic scope.

---

### 5.8 Sample reports (guideline item 9)

- Deliveries CSV  
- Manager weekly CSV (staff, dispense, deliveries)  
- Technician CSV (fleet + active tasks)  
- Patient HTML receipt (date/time, technician, brief reason, charge R0.00 or fee)

---

### 5.9 Future scope (guideline item 10)

Real SMS/email OTP; SignalR live drone ticks; full React SPA split; Azure/SQL managed backup; Android companion hardened offline queue; pharmacy prescription workflow integration under regulation.

---

### 5.10 Bibliography (sample)

ASP.NET Core documentation. Microsoft.  
Entity Framework Core documentation. Microsoft.  
Richfield Faculty of Information Technology. *IT Project Guidelines Semester 2 2026*.  
POPIA Act 4 of 2013.  
SACAA RPAS guidance (simulation rationale).

---

### 5.11 Glossary

| Term | Meaning |
|------|---------|
| MFA | Multi-factor authentication (password + OTP) |
| Haversine | Great-circle distance formula |
| RBAC | Role-based access control |
| Cold-chain | Temperature-sensitive supply flag |
| OTP | One-time password (6 minutes) |

---

*End of Phase 5 Implementation document.*
