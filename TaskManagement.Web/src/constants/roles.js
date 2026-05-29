/**
 * Role names must match the backend AppRoles and JWT Role claim values.
 * UI uses these constants to show/hide Admin-only actions.
 */
export const ROLES = {
  ADMIN: 'Admin',
  USER: 'User',
}

/** Returns true when the user has Admin role (case-insensitive). */
export function isAdmin(role) {
  return role?.toLowerCase() === ROLES.ADMIN.toLowerCase()
}

/** Returns true when the user has User role (case-insensitive). */
export function isUser(role) {
  return role?.toLowerCase() === ROLES.USER.toLowerCase()
}
