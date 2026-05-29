/**
 * JWT helpers for the React SPA.
 *
 * SECURITY NOTE:
 * - We decode the JWT payload on the client ONLY to read claims (Role, TenantId, Username).
 * - This is for UI convenience (show/hide buttons), NOT for security.
 * - The backend always validates the token signature and enforces RBAC + tenant isolation.
 *
 * localStorage risk (brief):
 * - Storing JWT in localStorage is simple for demos/interviews but vulnerable to XSS.
 * - Production apps often prefer httpOnly cookies; we document this trade-off in comments.
 */

/**
 * Decodes the JWT payload (middle segment) without verifying the signature.
 * Signature verification happens on the ASP.NET Core API only.
 */
export function decodeJwt(token) {
  if (!token) return null

  try {
    const payloadSegment = token.split('.')[1]
    const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
    const json = atob(normalized)
    return JSON.parse(json)
  } catch {
    return null
  }
}

/**
 * Extracts the Role claim from our API token.
 * AuthService emits both "Role" and standard role claims; we check both.
 */
export function getRoleFromToken(token) {
  const payload = decodeJwt(token)
  if (!payload) return null

  return payload.Role || payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null
}

export function getUsernameFromToken(token) {
  const payload = decodeJwt(token)
  if (!payload) return null
  return payload.Username || payload.unique_name || payload.sub || 'User'
}

export function getTenantIdFromToken(token) {
  const payload = decodeJwt(token)
  if (!payload) return null
  return payload.TenantId ?? null
}

export function getUserIdFromToken(token) {
  const payload = decodeJwt(token)
  if (!payload) return null
  return payload.UserId ?? null
}

/** Returns true if token exists and exp claim is still in the future. */
export function isTokenExpired(token) {
  const payload = decodeJwt(token)
  if (!payload?.exp) return true
  return Date.now() >= payload.exp * 1000
}
