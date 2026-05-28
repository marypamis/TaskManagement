using TaskManagement.API.Models.Tasks;

namespace TaskManagement.API.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetTasksAsync(CancellationToken cancellationToken = default);
    Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> UpdateTaskAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTaskAsync(int id, CancellationToken cancellationToken = default);
    Task<TaskResponse?> CompleteTaskAsync(int id, CancellationToken cancellationToken = default);
}
