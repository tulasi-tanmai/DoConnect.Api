# DoConnect API — Backend (ASP.NET Core **.NET 9**)

This is the backend for the **DoConnect** Q&A app. It exposes REST endpoints for
authentication, questions/answers, admin moderation, and image upload/serving.

**What this README is based on:** I scanned your repository and found the API at  
`Backend_DoConnect/DoConnect.Api/DoConnect.Api` targeting **net9.0**, using **EF Core 9**
with a **SQL Server** connection string named **`DefaultConnection`**, JWT auth,
Swagger, static file hosting (`wwwroot/uploads`), and a seeded **admin** user.

---

## Stack

- **.NET** 9 (ASP.NET Core Web API)
- **Entity Framework Core** 9 (SQL Server provider)
- **JWT** bearer authentication
- **BCrypt.Net** for password hashing
- **Swagger / Swashbuckle** for API docs
- **Static files** for uploaded images (served from `wwwroot/uploads`)

Project file highlights (`DoConnect.Api.csproj`):
- `TargetFramework: net9.0`
- Packages: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `Swashbuckle.AspNetCore`, `BCrypt.Net-Next`

---

## Prerequisites

- **.NET SDK 9.0** (or newer that supports net9.0)
- **SQL Server** (LocalDB/Express works fine)  
  *The code currently uses `UseSqlServer(...)` with `ConnectionStrings:DefaultConnection`.*
- (Optional) **dotnet-ef** tool for DB migrations
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## Configuration

Edit **`appsettings.json`** (and/or environment variables). Detected keys:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "<your SQL Server connection string>"
  },
  "Jwt": {
    "Key": "super_secret_key_goes_here_change_me",
    "Issuer": "DoConnect",
    "Audience": "DoConnectUsers",
    "ExpiresMinutes": 120
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

> **CORS** is configured in code with a policy named **"ng"** that allows  
> `http://localhost:4200` (Angular dev server). If your frontend runs elsewhere,
> change this origin inside `Program.cs`:
>
> ```csharp
> builder.Services.AddCors(opt => {
>     opt.AddPolicy("ng", p => p
>         .WithOrigins("http://localhost:4200")
>         .AllowAnyHeader()
>         .AllowAnyMethod());
> });
> app.UseCors("ng");
> ```

---

## Database

EF Core is registered as:

```csharp
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connStr));
```

If your DB is empty, run the migrations to create schema:

```bash
cd Backend_DoConnect/DoConnect.Api/DoConnect.Api
dotnet restore
dotet ef migrations add InCreate 
>it is used for creating database for firsttym >migrations -> updating the database overtime while changing the data
dotnet ef database update
```

> **Seeded admin:** At startup the app seeds an admin when none exists:  
> **email**: `admin@doconnect.local` • **username**: `admin` • **password**: `Admin@123`  
> (Change after first login.)

---

## Running

```bash
cd Backend_DoConnect/DoConnect.Api/DoConnect.Api

# Restore & run
dotnet restore
dotnet run
```

**Ports / URLs**
- Launch settings default to **`http://localhost:5108`** (HTTP) and **`https://localhost:7141`** (HTTPS).
- To force a different port:
  ```bash
  dotnet run --urls "http://localhost:5172"
  ```

**Swagger** (dev): `http://localhost:<port>/swagger`

If you see “address already in use”, pick another port or kill the process:
```powershell
netstat -ano | findstr :5108
taskkill /PID <pid> /F
```

---

## Auth

JWT authentication is enabled with issuer/audience/secret from `appsettings.json`.
Attach tokens as: `Authorization: Bearer <token>`.

**Routes (`/api/auth`)**
- `POST /api/auth/register`
  ```json
  { "username": "alice", "email": "alice@example.com", "password": "Pass@123" }
  ```
- `POST /api/auth/login`
  ```json
  { "usernameOrEmail": "alice", "password": "Pass@123" }
  ```
  → Returns `{ token, expires }`
- `GET /api/auth/me` (requires `Bearer <token>`) → basic user claims

---

## Questions & Answers

### Questions (`/api/questions`)

- `POST /api/questions` *(authorized)* — **multipart/form-data**  
  **Fields:** `Title` (max 140), `Text` (max 4000), optional `Files` (images).  
  Saves images to `wwwroot/uploads` and returns the created question.
- `GET /api/questions` *(public)* — list with paging & search  
  Query params:
  - `q` *(string, optional)*
  - `page` *(int, default 1)*
  - `pageSize` *(int, default 10)*
  - `includePending` *(bool, default false – only works for Admins)*
- `GET /api/questions/{id}` *(public)* — expands author, images, and answers (with images)

### Answers (`/api/questions/{questionId}/answers`)

- `POST` *(authorized)* — **multipart/form-data** with fields:  
  `Text` (max 4000), optional `Files` (images)
- `GET` *(public)* — list answers for a question  
  (Non-admins only see `Approved` answers; admins can see all)

**Approval states:** `Pending`, `Approved`, `Rejected`

---

## Admin Endpoints (`/api/admin`) — *[Authorize(Roles="Admin")]*

- `POST /api/admin/questions/{id}/approve`
- `POST /api/admin/questions/{id}/reject`
- `POST /api/admin/answers/{id}/approve`
- `POST /api/admin/answers/{id}/reject`
- `DELETE /api/admin/questions/{id}`
- `GET /api/admin/questions/pending` — pending questions
- `GET /api/admin/answers/pending` — pending answers

---

## File Uploads & Static Files

- Uploaded images are written under **`wwwroot/uploads`** with generated names.
- Static files are served via `app.UseStaticFiles()`, so images can be referenced by
  the relative path returned from the API (e.g. `"/uploads/<filename>.png"`).

If you need to allow larger uploads, adjust request size limits:
```csharp
[RequestSizeLimit(25_000_000)] // on controllers/actions that accept uploads
```

---

## Project Structure (backend)

```
DoConnect.Api/
 ├─ Controllers/
 │   ├─ AuthController.cs
 │   ├─ QuestionsController.cs
 │   ├─ AnswersController.cs
 │   └─ AdminController.cs
 ├─ Data/
 │   └─ AppDbContext.cs
 ├─ Dtos/                 # Register/Login, Question/Answer DTOs
 ├─ Models/               # User, Question, Answer, ImageFile, enums
 ├─ Services/             # JwtTokenService, ImageStorageService
 ├─ Properties/
 │   └─ launchSettings.json
 ├─ wwwroot/
 │   └─ uploads/
 ├─ appsettings.json
 ├─ appsettings.Development.json
 └─ Program.cs
```

---

## Troubleshooting

- **CORS blocked** — Ensure your frontend origin matches the `.WithOrigins("http://localhost:4200")` in `Program.cs`.
- **401 Unauthorized** — Send `Authorization: Bearer <token>`; token may have expired; re-login.
- **413 Payload Too Large** — Increase `[RequestSizeLimit(...)]` and/or Kestrel body size limits.
- **SQL connection fails** — Check `DefaultConnection` string, SQL instance accessibility, and permissions.
- **Images not loading** — Confirm the path you render matches the API response and that `UseStaticFiles()` is executed.

---

## Useful Commands

```bash
# From DoConnect.Api folder
dotnet restore
dotnet run --urls "http://localhost:5172"

# EF Core
dotnet ef migrations add <Migrations-name >
dotnet ef database update
```

---

## Security Notes

- Replace the default **JWT Key** with a long, random secret in production.

- Restrict max upload size and validate file types if exposing publicly.

