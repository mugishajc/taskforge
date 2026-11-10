using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tasks.Api.Domain;

namespace Tasks.Api.Features.Tasks;

[ApiController]
[Authorize]
[Route("api")]
public class TasksController(TaskService svc) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);

    [HttpPost("projects/{projectId:int}/tasks")]
    public async Task<IActionResult> Create(int projectId, CreateTaskRequest req, CancellationToken ct)
    {
        var dto = await svc.CreateAsync(CurrentUserId, projectId, req, ct);
        return dto is null ? NotFound() : Created($"/api/tasks/{dto.Id}", dto);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> List(
        [FromQuery] Domain.TaskStatus? status,
        [FromQuery] int? assigneeId,
        [FromQuery] int? projectId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new TaskFilter(status, assigneeId, projectId, search, page, pageSize);
        return Ok(await svc.ListAsync(CurrentUserId, filter, ct));
    }

    [HttpGet("projects/{projectId:int}/tasks")]
    public async Task<IActionResult> ListByProject(int projectId, [FromQuery] Domain.TaskStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var filter = new TaskFilter(status, null, projectId, null, page, pageSize);
        return Ok(await svc.ListAsync(CurrentUserId, filter, ct));
    }

    [HttpGet("tasks/{taskId:int}")]
    public async Task<IActionResult> Get(int taskId, CancellationToken ct)
    {
        var dto = await svc.GetAsync(CurrentUserId, taskId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("tasks/{taskId:int}")]
    public async Task<IActionResult> Update(int taskId, UpdateTaskRequest req, CancellationToken ct)
    {
        var dto = await svc.UpdateAsync(CurrentUserId, taskId, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPatch("tasks/{taskId:int}/assign")]
    public async Task<IActionResult> Assign(int taskId, AssignTaskRequest req, CancellationToken ct)
    {
        var dto = await svc.AssignAsync(CurrentUserId, taskId, req.AssigneeId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("tasks/{taskId:int}")]
    public async Task<IActionResult> Delete(int taskId, CancellationToken ct) =>
        await svc.DeleteAsync(CurrentUserId, taskId, ct) ? NoContent() : NotFound();
}

public record AssignTaskRequest(int? AssigneeId);

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DueOn).Must(d => d is null || d > DateTime.UtcNow.AddYears(-10)).WithMessage("DueOn too far in the past");
    }
}
public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator() => RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
}
