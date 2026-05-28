namespace TaskManagement.API.Models;

public class TaskItem
{
    public int Id { get; set; }

    // Required by the database; always set before insert (never from the client body).
    public string Title { get; set; } = string.Empty;

    // Required column; use empty string when the client omits description.
    public string Description { get; set; } = string.Empty;

    // New tasks always start incomplete unless explicitly updated later.
    public bool IsCompleted { get; set; }

    // Soft-delete flag; defaults to false on create.
    public bool IsDeleted { get; set; }

    // Set from JWT TenantId claim so each tenant only sees their own data.
    public int TenantId { get; set; }

    // Set from JWT UserId claim to record who created the task.
    public int CreatedByUserId { get; set; }
}
