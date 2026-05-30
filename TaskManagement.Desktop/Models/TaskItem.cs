namespace TaskManagement.Desktop.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int CreatedByUserId { get; set; }

    public string Status => IsCompleted ? "Completed" : "Open";
}
