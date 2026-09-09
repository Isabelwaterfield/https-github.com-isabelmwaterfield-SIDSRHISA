# SIDSRH — Smart Integrated Drone Supply for Rural Healthcare

**Phase 5 Professional Package**  
Student: Isabel Waterfield | 402410147

## Quick Start (SQLite — works offline immediately)

```powershell
cd SIDSRH.Api
Get-ChildItem -Recurse | Unblock-File
Remove-Item sidsrh_offline.db -ErrorAction SilentlyContinue
dotnet restore
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5080
```

Open: **http://localhost:5080**

## Login (MFA on every account)

| Email | Password |
|-------|----------|
| admin@sidsrh.health | Admin@123 |
| manager@sidsrh.health | Manager@123 |
| supply@sidsrh.health | Supply@123 |
| emergency@sidsrh.health | Emerg@123 |
| nurse@sidsrh.health | Staff@123 |
| tech@sidsrh.health | Tech@123 |
| patient@demo.health | Patient@123 |

After password, a 6-digit demo OTP appears on screen (valid 6 minutes).

## Database Options

Default is **SQLite** (`sidsrh_offline.db`).

For SQL Server (HealthcareDB):

1. Run `database/HealthcareDB_Schema_SQLServer.sql` (or your existing DB)
2. Run `database/HealthcareDB_Merge.sql` to add missing columns/tables
3. In `appsettings.json` set `"Provider": "SqlServer"`

MySQL also supported via `"Provider": "MySql"`.

## Features

- Role-based menus & JWT + BCrypt
- MFA (6-minute codes) for all users
- Live Leaflet map + animated drone flight paths
- Patient requests, dual dispatch/arrival codes
- Dispense + dual-staff verify
- Emergency simulation (≤15 km)
- Charging stations, weather hold
- Appointments, chronic schedules
- Reports (CSV + patient receipt)
- Activity log, lockout, backup API
- Android companion app (optional)

## Backup

```
POST /api/Backup/sqlite
GET  /api/Backup/list
GET  /api/Backup/instructions
```

## Project Structure

```
SIDSRH.Api/          ← .NET 8 Web API + SPA frontend
SIDSRH_Android/      ← Optional Android mini client
database/            ← SQL Server schema + merge script
docs/                ← Implementation notes & presentation
```

