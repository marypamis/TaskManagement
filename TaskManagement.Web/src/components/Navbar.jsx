import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

/**
 * Top navigation bar shown on authenticated pages.
 * Displays username, role, and logout action.
 */
export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    // Clear JWT from localStorage and return to login screen.
    logout()
    navigate('/login')
  }

  return (
    <header className="navbar">
      <div className="navbar-brand">
        <Link to="/dashboard">Task Management</Link>
      </div>
      <div className="navbar-meta">
        <span className="badge">{user?.role}</span>
        <span>{user?.username}</span>
        <span className="muted">Tenant: {user?.tenantId}</span>
        <button type="button" className="btn btn-secondary" onClick={handleLogout}>
          Logout
        </button>
      </div>
    </header>
  )
}
