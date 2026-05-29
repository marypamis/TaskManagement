import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { registerUser } from '../api/authService'
import { ROLES } from '../constants/roles'

/**
 * Registration page — extends the existing Vite app (no new project).
 *
 * Flow:
 * 1. User submits username, password, role, tenantId
 * 2. POST /api/Auth/register creates the account on the API
 * 3. On success → redirect to /login (user signs in to receive JWT)
 *
 * Register does NOT log the user in. JWT is only issued by /api/Auth/login
 * and stored in localStorage during the login step (see AuthContext + authService).
 */
export default function Register() {
  const navigate = useNavigate()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState(ROLES.USER)
  const [tenantId, setTenantId] = useState(1)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    setSuccess('')
    setLoading(true)

    try {
      await registerUser({
        username: username.trim(),
        password,
        role,
        tenantId: Number(tenantId),
      })

      setSuccess('Account created. Redirecting to login…')

      // Register → Login → Dashboard (JWT obtained only at login).
      setTimeout(() => navigate('/login', { replace: true }), 1200)
    } catch (err) {
      // Show API message when available; otherwise network/CORS/server-down hint.
      const apiMessage = err.response?.data?.message
      const networkHint = !err.response
        ? 'Cannot reach API. Start the backend (dotnet run in TaskManagement.API) and restart React (npm run dev).'
        : null

      setError(apiMessage || networkHint || 'Registration failed. Please check your details.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <form className="card login-card" onSubmit={handleSubmit}>
        <h1>Create Account</h1>
        <p className="subtitle">Register for Task Management</p>

        {error && <div className="alert alert-error">{error}</div>}
        {success && <div className="alert alert-success">{success}</div>}

        <label>
          Username
          <input
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
            required
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
            required
            minLength={4}
          />
        </label>

        <label>
          Role
          <select value={role} onChange={(e) => setRole(e.target.value)}>
            <option value={ROLES.USER}>User (read-only tasks)</option>
            <option value={ROLES.ADMIN}>Admin (full task access)</option>
          </select>
        </label>

        <label>
          Tenant ID
          <input
            type="number"
            value={tenantId}
            onChange={(e) => setTenantId(e.target.value)}
            min={1}
            required
          />
        </label>

        <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
          {loading ? 'Creating account…' : 'Register'}
        </button>

        <p className="auth-switch">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </div>
  )
}
