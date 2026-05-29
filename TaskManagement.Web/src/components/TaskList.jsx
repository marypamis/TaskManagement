/**
 * Task list table/card view.
 *
 * Role-based UI:
 * - Admin sees Edit, Delete, Complete buttons
 * - User sees read-only list (canViewOnly = true)
 *
 * IMPORTANT: Hiding buttons is UX only. Backend [Authorize(Roles="Admin")]
 * and service-layer checks still enforce real security.
 */
export default function TaskList({
  tasks,
  canModify,
  onEdit,
  onDelete,
  onComplete,
}) {
  if (!tasks.length) {
    return <p className="empty-state">No tasks found for your tenant.</p>
  }

  return (
    <div className="table-wrapper">
      <table className="task-table">
        <thead>
          <tr>
            <th>Title</th>
            <th>Description</th>
            <th>Status</th>
            <th>Created By</th>
            {canModify && <th>Actions</th>}
          </tr>
        </thead>
        <tbody>
          {tasks.map((task) => (
            <tr key={task.id}>
              <td>{task.title}</td>
              <td>{task.description}</td>
              <td>
                <span className={task.isCompleted ? 'status done' : 'status pending'}>
                  {task.isCompleted ? 'Completed' : 'Pending'}
                </span>
              </td>
              <td>{task.createdByUserId}</td>
              {canModify && (
                <td className="actions">
                  {!task.isCompleted && (
                    <button
                      type="button"
                      className="btn btn-small"
                      onClick={() => onComplete(task.id)}
                    >
                      Complete
                    </button>
                  )}
                  <button
                    type="button"
                    className="btn btn-small btn-secondary"
                    onClick={() => onEdit(task)}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="btn btn-small btn-danger"
                    onClick={() => onDelete(task.id)}
                  >
                    Delete
                  </button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
