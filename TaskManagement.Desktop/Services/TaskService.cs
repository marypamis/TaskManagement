using TaskManagement.Desktop.Models;

namespace TaskManagement.Desktop.Services;

public interface ITaskService
{
    Task<(bool Success, IReadOnlyList<TaskItem> Tasks, string? ErrorMessage)> GetTasksAsync();
    Task<(bool Success, string? ErrorMessage)> CompleteTaskAsync(int taskId);
}

public class TaskService : ITaskService
{
    private readonly ApiClient _apiClient;

    public TaskService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<(bool Success, IReadOnlyList<TaskItem> Tasks, string? ErrorMessage)> GetTasksAsync()
    {
        var result = await _apiClient.GetAsync<List<TaskItem>>("/api/Tasks");

        if (!result.Success || result.Data is null)
        {
            return (false, Array.Empty<TaskItem>(), result.ErrorMessage ?? "Could not load tasks.");
        }

        return (true, result.Data, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> CompleteTaskAsync(int taskId)
    {
        var result = await _apiClient.PatchAsync<TaskItem>($"/api/Tasks/{taskId}/complete");
        return (result.Success, result.ErrorMessage ?? "Could not complete task.");
    }
}
