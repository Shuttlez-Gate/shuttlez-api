# Shuttlez API

Backend for the **Shuttlez** shuttle booking Flutter app.

## Stack

- .NET 8 (ASP.NET Core Web API)
- PostgreSQL 16 + EF Core 8
- Redis 7
- MediatR (CQRS) + FluentValidation
- JWT + Refresh Tokens
- SignalR (trip tracking + support chat)
- Swagger/OpenAPI

## Project Structure

```
src/
├── Shuttlez.Domain/          # Entities, Enums
├── Shuttlez.Application/     # CQRS, DTOs, Validators
├── Shuttlez.Infrastructure/  # EF Core, JWT, OTP, External services
└── Shuttlez.API/             # Controllers, Hubs, Middleware
tests/
├── Shuttlez.UnitTests/
└── Shuttlez.IntegrationTests/
```

## Quick Start

### 1) Start infrastructure

```bash
docker compose up -d
```

### 2) Apply migrations & run API

```bash
cd src/Shuttlez.API
dotnet ef migrations add InitialCreate --project ../Shuttlez.Infrastructure
dotnet ef database update --project ../Shuttlez.Infrastructure
dotnet run
```

Swagger (Development): `https://localhost:7xxx/swagger`

### 3) Dev OTP

In Development, OTP is always **`1234`** (logged to console).

## API Endpoints (Phase 0/1)

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/api/v1/health` | No |
| POST | `/api/v1/auth/send-otp` | No |
| POST | `/api/v1/auth/verify-otp` | No |
| POST | `/api/v1/auth/register` | No |
| POST | `/api/v1/auth/refresh-token` | No |
| POST | `/api/v1/auth/logout` | No |
| GET | `/api/v1/users/me` | Yes |
| PATCH | `/api/v1/users/me` | Yes |

## Auth Flow

### Login
1. `POST /api/v1/auth/send-otp` `{ "phone": "+2010xxxxxxxx", "purpose": "login" }`
2. `POST /api/v1/auth/verify-otp` `{ "phone": "+2010xxxxxxxx", "code": "1234", "purpose": "login" }`

### Register
1. `POST /api/v1/auth/send-otp` `{ "phone": "+2010xxxxxxxx", "purpose": "register" }`
2. `POST /api/v1/auth/register` `{ "phone": "...", "fullName": "...", "email": "...", "gender": "ذكر", "code": "1234" }`

## Response Format

```json
{
  "success": true,
  "data": {},
  "message": "تم بنجاح",
  "errors": []
}
```

## SignalR Hubs

- `/hubs/trip-tracking` — live driver location
- `/hubs/support-chat` — support messages

Pass JWT via query: `?access_token=YOUR_TOKEN`

## Flutter Integration

```dart
const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://10.0.2.2:5000', // Android emulator
);
```

## Next Phases

- [ ] Saved Locations module
- [ ] Booking Preview + Create Booking
- [ ] Trips list + live tracking
- [ ] Routes catalog
- [ ] Subscriptions + Wallet
- [ ] Notifications + FCM
- [ ] Support tickets + chat
- [ ] FAQ + Legal CMS
