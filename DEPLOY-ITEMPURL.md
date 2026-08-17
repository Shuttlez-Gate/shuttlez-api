# Deploy Shuttlez API to itempurl / SmarterASP

## Recommended package (self-contained — no server dotnet required)

Upload everything from:

```
F:\aa_MOC\publish\shuttlez-hosting
```

## Critical: keep production `appsettings.json`

Before upload, **backup** the `appsettings.json` already on the server (JWT secret, connection string).

Upload **DLL/EXE files only** OR merge your server settings into the new package.

Required in `Cors:AllowedOrigins`:

- `https://shuttlez-dashboard.web.app`
- `https://shuttlez-dashboard.firebaseapp.com`

## If API returns 503 after upload

1. SmarterASP panel → **Restart** site / recycle app pool
2. Open **`logs/startup-error.txt`** on the server (DB/migration failure)
3. Open **`logs/stdout_*.log`** (IIS startup)
4. Open **`logs/shuttlez-*.log`** (app log)

### Database connection from hosting

If `startup-error.txt` mentions SSL or connection failure, edit server `appsettings.json` connection string:

Replace:

```
Channel Binding=Require
```

With:

```
SSL Mode=Require
```

(Keep the same Host, Database, Username, Password.)

## Verify

- `https://shuttlez-001-site1.itempurl.com/api/v1/admin/auth/send-otp` → **405** or **400** (not 503)
- Login from https://shuttlez-dashboard.web.app
