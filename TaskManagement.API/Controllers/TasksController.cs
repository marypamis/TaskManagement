using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Authorization;
using TaskManagement.API.Models.Tasks;
using TaskManagement.API.Services;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All task endpoints require a valid JWT.
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    // Admin and User may view tasks in their tenant only (tenant enforced in service/EF).
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.User}")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetTasks(CancellationToken cancellationToken)
    {
        try
        {
            var tasks = await _taskService.GetTasksAsync(cancellationToken);
            return Ok(tasks);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden task list request.");
            return Forbid();
        }
    }

    // Only Admin may create tasks; User role receives 403 from authorization pipeline.
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TaskResponse>> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        try
        {
            var task = await _taskService.CreateTaskAsync(request, cancellationToken);
            if (task is null)
            {
                return BadRequest("Invalid authentication context. TenantId and UserId claims are required.");
            }

            return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden task create request.");
            return Forbid();
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTask(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        try
        {
            var task = await _taskService.UpdateTaskAsync(id, request, cancellationToken);
            if (task is null)
            {
                return NotFound();
            }

            return Ok(task);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden task update request. TaskId: {TaskId}", id);
            return Forbid();
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _taskService.DeleteTaskAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden task delete request. TaskId: {TaskId}", id);
            return Forbid();
        }
    }

    [HttpPatch("{id:int}/complete")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> CompleteTask(int id, CancellationToken cancellationToken)
    {
        try
        {
            var task = await _taskService.CompleteTaskAsync(id, cancellationToken);
            if (task is null)
            {
                return NotFound();
            }

            return Ok(task);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden task complete request. TaskId: {TaskId}", id);
            return Forbid();
        }
    }
}
