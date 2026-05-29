import apiClient from './axiosClient'

/**
 * Task API layer — all calls use axiosClient which attaches JWT Authorization header.
 *
 * Tenant isolation:
 * - We do NOT send TenantId from the frontend.
 * - The API reads TenantId from the JWT and returns only that tenant's tasks.
 */

/**
 * GET /api/Tasks
 * Purpose: Load all tasks for the logged-in user's tenant (filtered on backend).
 */
export async function fetchTasks() {
  const response = await apiClient.get('/api/Tasks')
  return response.data
}

/**
 * POST /api/Tasks
 * Purpose: Create a new task (Admin only — backend returns 403 for User role).
 * Body only includes Title/Description; TenantId and CreatedByUserId come from JWT.
 */
export async function createTask(task) {
  const response = await apiClient.post('/api/Tasks', {
    title: task.title,
    description: task.description ?? '',
  })
  return response.data
}

/**
 * PUT /api/Tasks/{id}
 * Purpose: Update title, description, and completion status (Admin only).
 */
export async function updateTask(id, task) {
  const response = await apiClient.put(`/api/Tasks/${id}`, {
    title: task.title,
    description: task.description ?? '',
    isCompleted: task.isCompleted ?? false,
  })
  return response.data
}

/**
 * DELETE /api/Tasks/{id}
 * Purpose: Soft-delete a task (Admin only).
 */
export async function deleteTask(id) {
  await apiClient.delete(`/api/Tasks/${id}`)
}

/**
 * PATCH /api/Tasks/{id}/complete
 * Purpose: Mark task as completed without editing other fields (Admin only).
 */
export async function completeTask(id) {
  const response = await apiClient.patch(`/api/Tasks/${id}/complete`)
  return response.data
}
