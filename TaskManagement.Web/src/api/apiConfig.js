/**
 * API base URL for the existing TaskManagement.Web SPA.
 *
 * Development: leave VITE_API_BASE_URL empty — Vite proxy (vite.config.js) forwards
 *   /api/* → http://localhost:5016/api/* (no CORS issues).
 *
 * Production: set VITE_API_BASE_URL to your deployed API origin.
 */
export const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
