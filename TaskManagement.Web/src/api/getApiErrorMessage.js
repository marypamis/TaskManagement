/**
 * Extracts a user-friendly message from an Axios error response.
 */
export function getApiErrorMessage(err, fallback) {
  const data = err.response?.data
  if (typeof data === 'string' && data.trim()) return data
  if (data?.message) return data.message

  const status = err.response?.status
  if (status === 401) return 'Session expired or invalid. Please log in again.'
  if (status === 403) return 'You do not have permission for this action (Admin role required).'
  if (!err.response) return 'Cannot reach the API. Ensure the backend is running on port 5016.'

  return fallback
}
