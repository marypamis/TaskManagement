import axios from 'axios'
import { API_BASE } from './apiConfig'
import { STORAGE_KEYS } from '../constants/storageKeys'

/**
 * Authentication API layer for the existing TaskManagement.Web SPA.
 *
 * Register vs Login:
 * - registerUser() creates a new account (no JWT returned)
 * - login() authenticates and stores JWT in localStorage for protected API calls
 *
 * We extend this file rather than creating a new project so login, dashboard,
 * and task features keep sharing one Axios base URL and token storage pattern.
 */

/**
 * POST /api/Auth/register
 * Creates a user on the backend. Password is hashed server-side by ASP.NET Identity.
 * Does NOT store a token — user must log in separately after registration.
 */
export async function registerUser(data) {
  const response = await axios.post(`${API_BASE}/api/Auth/register`, {
    username: data.username,
    password: data.password,
    role: data.role,
    tenantId: data.tenantId,
  })

  return response.data
}

/**
 * POST /api/Auth/login
 * Exchanges credentials for a JWT. Token is saved to localStorage and attached
 * to subsequent requests via axiosClient Authorization header.
 */
export async function login(username, password) {
  const response = await axios.post(`${API_BASE}/api/Auth/login`, {
    username,
    password,
  })

  const { token } = response.data

  if (token) {
    localStorage.setItem(STORAGE_KEYS.TOKEN, token)
  }

  return response.data
}

export function logout() {
  localStorage.removeItem(STORAGE_KEYS.TOKEN)
}

export function getStoredToken() {
  return localStorage.getItem(STORAGE_KEYS.TOKEN)
}
