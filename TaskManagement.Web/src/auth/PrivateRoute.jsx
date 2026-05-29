import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'

/**
 * Protects routes that require a valid JWT.
 * Unauthenticated users are redirected to /login.
 */
export default function PrivateRoute({ children }) {
  const { isAuthenticated } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return children
}
