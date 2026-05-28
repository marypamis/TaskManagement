using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.Models;
using TaskManagement.API.Models.Tasks;

namespace TaskManagement.API.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        AppDbContext dbContext,
        ICurrentUserContext currentUser,
        ILogger<TaskService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        // Service-layer RBAC: defense in depth if controller attributes are bypassed.
        _currentUser.RequireCanReadTasks();
        _currentUser.TryGetTenantAndUser(out var tenantId, out var userId);

        var tasks = await _dbContext.GetTasksByTenantFromStoredProcedureAsync(tenantId, cancellationToken);

        _logger.LogInformation(
            "Retrieved {TaskCount} tasks for TenantId {TenantId}, UserId {UserId}, Role {Role}",
            tasks.Count,
            tenantId,
            userId,
            _currentUser.Role);

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        // Only Admin may create tasks; User is read-only.
        _currentUser.RequireCanModifyTasks();
        _currentUser.TryGetTenantAndUser(out var tenantId, out var userId);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            _logger.LogWarning(
                "Task creation denied. Title is required. TenantId: {TenantId}, UserId: {UserId}",
                tenantId,
                userId);
            return null;
        }

        var task = BuildNewTask(request, tenantId, userId);

        if (!IsValidForInsert(task))
        {
            _logger.LogWarning(
                "Task creation blocked. Invalid entity state. TenantId: {TenantId}, UserId: {UserId}, Title: {Title}",
                task.TenantId,
                task.CreatedByUserId,
                task.Title);
            throw new InvalidOperationException("Task data is invalid. Title, TenantId, and CreatedByUserId are required.");
        }

        _dbContext.Tasks.Add(task);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            var dbError = GetInnermostMessage(ex);
            _logger.LogError(
                ex,
                "Task creation failed. TenantId: {TenantId}, UserId: {UserId}, Title: {Title}, DatabaseError: {DatabaseError}",
                tenantId,
                userId,
                request.Title,
                dbError);
            throw new InvalidOperationException($"Unable to save the task: {dbError}", ex);
        }

        _logger.LogInformation(
            "Task created. TaskId {TaskId}, TenantId {TenantId}, UserId {UserId}, Role {Role}",
            task.Id,
            tenantId,
            userId,
            _currentUser.Role);

        return MapToResponse(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.RequireCanModifyTasks();
        _currentUser.TryGetTenantAndUser(out var tenantId, out var userId);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return null;
        }

        var task = await FindTenantTaskAsync(id, tenantId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning(
                "Task update failed. Not found. TaskId {TaskId}, TenantId {TenantId}, UserId {UserId}",
                id,
                tenantId,
                userId);
            return null;
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim() ?? string.Empty;
        task.IsCompleted = request.IsCompleted;

        await SaveChangesSafelyAsync("update", task.Id, tenantId, userId, cancellationToken);

        _logger.LogInformation("Task updated. TaskId {TaskId}, TenantId {TenantId}", task.Id, tenantId);
        return MapToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.RequireCanModifyTasks();
        _currentUser.TryGetTenantAndUser(out var tenantId, out var userId);

        var task = await FindTenantTaskAsync(id, tenantId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning(
                "Task deletion failed. Not found. TaskId {TaskId}, TenantId {TenantId}",
                id,
                tenantId);
            return false;
        }

        task.IsDeleted = true;
        await SaveChangesSafelyAsync("delete", task.Id, tenantId, userId, cancellationToken);

        _logger.LogInformation("Task soft-deleted. TaskId {TaskId}, TenantId {TenantId}", task.Id, tenantId);
        return true;
    }

    public async Task<TaskResponse?> CompleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.RequireCanModifyTasks();
        _currentUser.TryGetTenantAndUser(out var tenantId, out var userId);

        var task = await FindTenantTaskAsync(id, tenantId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning(
                "Task completion failed. Not found. TaskId {TaskId}, TenantId {TenantId}",
                id,
                tenantId);
            return null;
        }

        task.IsCompleted = true;
        await SaveChangesSafelyAsync("complete", task.Id, tenantId, userId, cancellationToken);

        _logger.LogInformation("Task marked complete. TaskId {TaskId}, TenantId {TenantId}", task.Id, tenantId);
        return MapToResponse(task);
    }

    private static TaskItem BuildNewTask(CreateTaskRequest request, int tenantId, int userId) => new()
    {
        Title = request.Title.Trim(),
        Description = request.Description?.Trim() ?? string.Empty,
        IsCompleted = false,
        IsDeleted = false,
        TenantId = tenantId,
        CreatedByUserId = userId
    };

    private static bool IsValidForInsert(TaskItem task) =>
        !string.IsNullOrWhiteSpace(task.Title)
        && !string.IsNullOrEmpty(task.Description)
        && task.TenantId > 0
        && task.CreatedByUserId > 0;

    private async Task SaveChangesSafelyAsync(
        string operation,
        int taskId,
        int tenantId,
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            var dbError = GetInnermostMessage(ex);
            _logger.LogError(
                ex,
                "Task {Operation} failed. TaskId: {TaskId}, TenantId: {TenantId}, DatabaseError: {DatabaseError}",
                operation,
                taskId,
                tenantId,
                dbError);
            throw new InvalidOperationException($"Unable to {operation} the task: {dbError}", ex);
        }
    }

    private async Task<TaskItem?> FindTenantTaskAsync(int id, int tenantId, CancellationToken cancellationToken)
    {
        // TenantId from JWT ensures Admin cannot access another tenant's tasks.
        return await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
    }

    private static TaskResponse MapToResponse(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        IsCompleted = task.IsCompleted,
        CreatedByUserId = task.CreatedByUserId
    };

    private static string GetInnermostMessage(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception.Message;
    }
}
