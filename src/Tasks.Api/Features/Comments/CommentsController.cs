using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tasks.Api.Features.Comments;

[ApiController]
[Authorize]
[Route("api/tasks/{taskId:int}/comments")]
public class CommentsController(CommentService svc) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);

    [HttpPost]
    public async Task<IActionResult> Create(int taskId, CreateCommentRequest req, CancellationToken ct)
    {
        var dto = await svc.AddAsync(CurrentUserId, taskId, req, ct);
        return dto is null ? NotFound() : Created($"/api/tasks/{taskId}/comments/{dto.Id}", dto);
    }

    [HttpGet]
    public async Task<IActionResult> List(int taskId, CancellationToken ct) =>
        Ok(await svc.ListAsync(CurrentUserId, taskId, ct));

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int taskId, int commentId, CancellationToken ct) =>
        await svc.DeleteAsync(CurrentUserId, commentId, ct) ? NoContent() : NotFound();
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}
