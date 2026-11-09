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

    [HttpGet("projects/{projectId:int}/tasks")]
    public async Task<IActionResult> List(int projectId, [FromQuery] Domain.TaskStatus? status, CancellationToken ct) =>
        Ok(await svc.ListAsync(CurrentUserId, projectId, status, ct));

    [HttpPut("tasks/{taskId:int}")]
    public async Task<IActionResult> Update(int taskId, UpdateTaskRequest req, CancellationToken ct)
    {
        var dto = await svc.UpdateAsync(CurrentUserId, taskId, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("tasks/{taskId:int}")]
    public async Task<IActionResult> Delete(int taskId, CancellationToken ct) =>
        await svc.DeleteAsync(CurrentUserId, taskId, ct) ? NoContent() : NotFound();
}

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
