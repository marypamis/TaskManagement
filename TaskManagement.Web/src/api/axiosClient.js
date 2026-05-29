import axios from 'axios'
import { API_BASE } from './apiConfig'
import { STORAGE_KEYS } from '../constants/storageKeys'

/**
 * Shared Axios instance for all API calls.
 *
 * Why Authorization header:
 * - Protected endpoints (/api/Tasks) require a valid JWT Bearer token.
 * - The interceptor reads the token from localStorage and attaches it automatically
 *   so individual service functions do not repeat header logic.
 */
const apiClient = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(STORAGE_KEYS.TOKEN)

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

/**
 * On 401, clear stale token so the user is sent back to login.
 * Backend still enforces auth; this improves UX when token expires.
 */
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(STORAGE_KEYS.TOKEN)
    }
    return Promise.reject(error)
  }
)

export default apiClient
