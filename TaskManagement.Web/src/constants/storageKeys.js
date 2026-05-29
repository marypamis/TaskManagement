/**
 * Central keys for localStorage so token access stays consistent across the app.
 * We store the JWT here after login so the SPA can attach it to API requests.
 */
export const STORAGE_KEYS = {
  TOKEN: 'taskmgmt_jwt_token',
}
