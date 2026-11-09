using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tasks.Api.Features.Projects;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController(ProjectService svc) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest req, CancellationToken ct)
    {
        var dto = await svc.CreateAsync(CurrentUserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await svc.ListForUserAsync(CurrentUserId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var dto = await svc.GetForUserAsync(CurrentUserId, id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await svc.DeleteAsync(CurrentUserId, id, ct) ? NoContent() : NotFound();
}

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator() => RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(120);
}
