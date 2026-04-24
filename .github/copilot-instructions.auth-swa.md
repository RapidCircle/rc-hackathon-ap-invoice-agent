# 🔐 Authentication
<!-- AUTH-SECTION-START -->

This project uses **Azure Static Web Apps (SWA) built-in authentication**.

## SWA Auth Overview

Azure Static Web Apps provides built-in authentication with:

- Azure AD (Microsoft Entra ID) integration
- Automatic token management
- Role-based access control via `staticwebapp.config.json`
- No custom authentication code required

## Authentication Endpoints (SWA Built-in)

| Endpoint | Description |
|----------|-------------|
| `/.auth/login/aad` | Initiates Azure AD login |
| `/.auth/logout` | Logs out the user |
| `/.auth/me` | Returns current user's client principal |

## Testing Authentication

1. Navigate to `http://127.0.0.1:4280`
2. Access a protected route (e.g., `/app/`)
3. You'll be redirected to `/.auth/login/aad`
4. Complete Azure AD login
5. You'll be redirected back to the app

## Checking Auth State

```bash
curl http://127.0.0.1:4280/.auth/me
```

Returns the client principal with user info and roles.

## Route Protection

Protected routes are configured in `staticwebapp.config.json`:

```json
{
  "routes": [
    {
      "route": "/app/*",
      "allowedRoles": ["authenticated"]
    }
  ]
}
```

## User Roles

SWA provides these built-in roles:

| Role | Description |
|------|-------------|
| `anonymous` | All users (including unauthenticated) |
| `authenticated` | Any logged-in user |

Custom roles can be assigned via Azure Portal or custom role assignment.

## Important Notes

- **Do not implement custom authentication** - use SWA built-in auth
- All API routes under `/api/` are automatically protected by SWA proxy
- The `/.auth/*` routes are reserved by SWA

<!-- AUTH-SECTION-END -->
