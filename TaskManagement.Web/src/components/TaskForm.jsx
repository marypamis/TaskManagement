import { useEffect, useState } from 'react'

/**
 * Reusable form for create and edit task flows.
 * Only rendered for Admin users from Dashboard (User role never sees this).
 */
export default function TaskForm({ initialValues, onSubmit, onCancel, submitLabel = 'Save' }) {
  const [title, setTitle] = useState(initialValues?.title ?? '')
  const [description, setDescription] = useState(initialValues?.description ?? '')
  const [isCompleted, setIsCompleted] = useState(initialValues?.isCompleted ?? false)
  const [showCompleted, setShowCompleted] = useState(Boolean(initialValues?.id))

  useEffect(() => {
    setTitle(initialValues?.title ?? '')
    setDescription(initialValues?.description ?? '')
    setIsCompleted(initialValues?.isCompleted ?? false)
    setShowCompleted(Boolean(initialValues?.id))
  }, [initialValues])

  const handleSubmit = (event) => {
    event.preventDefault()
    onSubmit({ title, description, isCompleted })
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <h3>{initialValues?.id ? 'Edit Task' : 'Create Task'}</h3>

      <label>
        Title *
        <input
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          placeholder="Task title"
        />
      </label>

      <label>
        Description
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={3}
          placeholder="Task description"
        />
      </label>

      {showCompleted && (
        <label className="checkbox-row">
          <input
            type="checkbox"
            checked={isCompleted}
            onChange={(e) => setIsCompleted(e.target.checked)}
          />
          Mark as completed
        </label>
      )}

      <div className="form-actions">
        <button type="submit" className="btn btn-primary">
          {submitLabel}
        </button>
        {onCancel && (
          <button type="button" className="btn btn-secondary" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
