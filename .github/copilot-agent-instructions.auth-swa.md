# 🔐 Authentication Override (Sandbox Limitation)
<!-- AUTH-SECTION-START -->

The SWA CLI login UI (`/.auth/login/aad`) **cannot be used** in the GitHub
Actions sandbox due to blocked external CDN dependencies.

Therefore, **authentication must be performed programmatically.**

## Recommended: JavaScript Auth Cookie Injection

```javascript
const clientPrincipal = {
  identityProvider: "aad",
  userId: "test-user-id-12345",
  userDetails: "test@example.com",
  userRoles: ["anonymous", "authenticated"],
  claims: []
};

document.cookie = `StaticWebAppsAuthCookie=${btoa(JSON.stringify(clientPrincipal))}; path=/`;
```

Then navigate to:

    /app/

## curl-Based Authentication (Alternative)

```bash
AUTH_COOKIE=$(echo -n '{"identityProvider":"aad","userId":"test-user-id","userDetails":"test@example.com","userRoles":["anonymous","authenticated"],"claims":[]}' | base64 -w0)

curl -b "StaticWebAppsAuthCookie=$AUTH_COOKIE" http://127.0.0.1:4280/app/
```

## Verify Auth State

```bash
curl http://127.0.0.1:4280/.auth/me
```

This returns the injected client principal.

## Important Notes

- Cookie injection only works with SWA CLI (local development)
- In production, Azure handles authentication automatically
- The `/.auth/*` routes are reserved by SWA

<!-- AUTH-SECTION-END -->
