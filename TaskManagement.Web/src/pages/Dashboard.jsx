import { useCallback, useEffect, useState } from 'react'
import Layout from '../components/Layout'
import TaskList from '../components/TaskList'
import TaskForm from '../components/TaskForm'
import { useAuth } from '../auth/AuthContext'
import {
  fetchTasks,
  createTask,
  updateTask,
  deleteTask,
  completeTask,
} from '../api/taskService'
import { getApiErrorMessage } from '../api/getApiErrorMessage'

/**
 * Dashboard — main task management screen after login.
 *
 * Tenant isolation:
 * - Frontend calls GET /api/Tasks with JWT only
 * - Backend uses TenantId from token + stored procedure / EF filters
 * - We display whatever the API returns (already tenant-scoped)
 */
export default function Dashboard() {
  const { user } = useAuth()
  const isAdmin = user?.isAdmin

  const [tasks, setTasks] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingTask, setEditingTask] = useState(null)

  const loadTasks = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await fetchTasks()
      setTasks(data)
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load tasks.'))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadTasks()
  }, [loadTasks])

  const handleCreate = async (formData) => {
    try {
      await createTask(formData)
      setShowForm(false)
      await loadTasks()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to create task.'))
    }
  }

  const handleUpdate = async (formData) => {
    try {
      await updateTask(editingTask.id, formData)
      setEditingTask(null)
      await loadTasks()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to update task.'))
    }
  }

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this task?')) return
    try {
      await deleteTask(id)
      await loadTasks()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to delete task.'))
    }
  }

  const handleComplete = async (id) => {
    try {
      await completeTask(id)
      await loadTasks()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to complete task.'))
    }
  }

  return (
    <Layout>
      <div className="dashboard-header">
        <div>
          <h1>Tasks</h1>
          <p className="muted">
            Showing tasks for tenant <strong>{user?.tenantId}</strong> — role:{' '}
            <strong>{user?.role}</strong>
          </p>
        </div>

        {/* Admin-only: User role cannot create tasks (backend also returns 403). */}
        {isAdmin && !showForm && !editingTask && (
          <button type="button" className="btn btn-primary" onClick={() => setShowForm(true)}>
            + New Task
          </button>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {showForm && (
        <TaskForm
          onSubmit={handleCreate}
          onCancel={() => setShowForm(false)}
          submitLabel="Create Task"
        />
      )}

      {editingTask && (
        <TaskForm
          initialValues={editingTask}
          onSubmit={handleUpdate}
          onCancel={() => setEditingTask(null)}
          submitLabel="Update Task"
        />
      )}

      {loading ? (
        <p>Loading tasks…</p>
      ) : (
        <TaskList
          tasks={tasks}
          canModify={isAdmin}
          onEdit={setEditingTask}
          onDelete={handleDelete}
          onComplete={handleComplete}
        />
      )}

      {!isAdmin && (
        <p className="info-note">
          You have read-only access. Contact an Admin to create or modify tasks.
        </p>
      )}
    </Layout>
  )
}
