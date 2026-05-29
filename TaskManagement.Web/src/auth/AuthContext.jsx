import { createContext, useContext, useMemo, useState, useCallback, useEffect } from 'react'
import { getStoredToken, login as apiLogin, logout as apiLogout } from '../api/authService'
import {
  decodeJwt,
  getRoleFromToken,
  getUsernameFromToken,
  getTenantIdFromToken,
  isTokenExpired,
} from './jwtUtils'
import { isAdmin } from '../constants/roles'

/**
 * AuthContext centralizes login state for the whole SPA.
 *
 * JWT flow (step-by-step):
 * 1. User submits credentials on Login page
 * 2. authService calls POST /api/Auth/login
 * 3. API returns JWT with UserId, TenantId, Role claims
 * 4. Token saved to localStorage
 * 5. Context exposes user info decoded from token for UI (role-based buttons)
 * 6. axiosClient attaches Bearer token on every /api/Tasks request
 */

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => getStoredToken())

  useEffect(() => {
    if (token && isTokenExpired(token)) {
      apiLogout()
      setToken(null)
    }
  }, [token])

  const user = useMemo(() => {
    if (!token) return null

    return {
      token,
      username: getUsernameFromToken(token),
      role: getRoleFromToken(token),
      tenantId: getTenantIdFromToken(token),
      claims: decodeJwt(token),
      isAdmin: isAdmin(getRoleFromToken(token)),
    }
  }, [token])

  const login = useCallback(async (username, password) => {
    const result = await apiLogin(username, password)
    setToken(result.token)
    return result
  }, [])

  const logout = useCallback(() => {
    apiLogout()
    setToken(null)
  }, [])

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: Boolean(token && !isTokenExpired(token)),
      login,
      logout,
    }),
    [user, token, login, logout]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
