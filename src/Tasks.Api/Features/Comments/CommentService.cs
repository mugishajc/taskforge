using Microsoft.EntityFrameworkCore;
using Tasks.Api.Data;
using Tasks.Api.Domain;

namespace Tasks.Api.Features.Comments;

public record CreateCommentRequest(string Body);
public record CommentDto(int Id, int TaskItemId, int AuthorId, string AuthorName, string Body, DateTime CreatedAt);

public class CommentService(AppDbContext db)
{
    public async Task<CommentDto?> AddAsync(int userId, int taskId, CreateCommentRequest req, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.Project!.OwnerId != userId) return null;

        var comment = new Comment
        {
            TaskItemId = taskId,
            AuthorId = userId,
            Body = req.Body.Trim()
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return await LoadDto(comment.Id, ct);
    }

    public async Task<List<CommentDto>> ListAsync(int userId, int taskId, CancellationToken ct)
    {
        var owned = await db.Tasks.AnyAsync(t => t.Id == taskId && t.Project!.OwnerId == userId, ct);
        if (!owned) return new();

        return await db.Comments
            .Where(c => c.TaskItemId == taskId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.TaskItemId, c.AuthorId, c.Author!.Name, c.Body, c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(int userId, int commentId, CancellationToken ct)
    {
        var comment = await db.Comments.FindAsync([commentId], ct);
        if (comment is null || comment.AuthorId != userId) return false;
        db.Comments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<CommentDto?> LoadDto(int commentId, CancellationToken ct)
    {
        var c = await db.Comments.Include(x => x.Author).FirstOrDefaultAsync(x => x.Id == commentId, ct);
        return c is null ? null : new CommentDto(c.Id, c.TaskItemId, c.AuthorId, c.Author!.Name, c.Body, c.CreatedAt);
    }
}
