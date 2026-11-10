using Microsoft.EntityFrameworkCore;
using Tasks.Api.Data;
using Tasks.Api.Domain;

namespace Tasks.Api.Features.Tasks;

public record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueOn, int? AssigneeId);
public record UpdateTaskRequest(string Title, string? Description, Domain.TaskStatus Status, TaskPriority Priority, DateTime? DueOn, int? AssigneeId);
public record TaskDto(int Id, int ProjectId, string Title, string? Description, Domain.TaskStatus Status, TaskPriority Priority, DateTime? DueOn, int? AssigneeId, string? AssigneeName, DateTime CreatedAt, DateTime UpdatedAt);

public record TaskFilter(Domain.TaskStatus? Status, int? AssigneeId, int? ProjectId, string? Search, int Page = 1, int PageSize = 20);
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class TaskService(AppDbContext db)
{
    public async Task<TaskDto?> CreateAsync(int userId, int projectId, CreateTaskRequest req, CancellationToken ct)
    {
        var owned = await db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);
        if (!owned) return null;
        var t = new TaskItem
        {
            ProjectId = projectId,
            Title = req.Title.Trim(),
            Description = req.Description,
            Priority = req.Priority,
            DueOn = req.DueOn,
            AssigneeId = req.AssigneeId
        };
        db.Tasks.Add(t);
        await db.SaveChangesAsync(ct);
        return await LoadDto(t.Id, ct);
    }

    public async Task<PagedResult<TaskDto>> ListAsync(int userId, TaskFilter filter, CancellationToken ct)
    {
        var q = db.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.Project!.OwnerId == userId);

        if (filter.ProjectId is not null)
            q = q.Where(t => t.ProjectId == filter.ProjectId);
        if (filter.Status is not null)
            q = q.Where(t => t.Status == filter.Status);
        if (filter.AssigneeId is not null)
            q = q.Where(t => t.AssigneeId == filter.AssigneeId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            q = q.Where(t => t.Title.Contains(filter.Search));

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await q.OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.DueOn, t.AssigneeId, t.Assignee != null ? t.Assignee.Name : null, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TaskDto>(items, total, page, pageSize);
    }

    public async Task<TaskDto?> UpdateAsync(int userId, int taskId, UpdateTaskRequest req, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t is null || t.Project!.OwnerId != userId) return null;
        t.Title = req.Title.Trim();
        t.Description = req.Description;
        t.Status = req.Status;
        t.Priority = req.Priority;
        t.DueOn = req.DueOn;
        t.AssigneeId = req.AssigneeId;
        t.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await LoadDto(t.Id, ct);
    }

    public async Task<bool> DeleteAsync(int userId, int taskId, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t is null || t.Project!.OwnerId != userId) return false;
        db.Tasks.Remove(t);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TaskDto?> GetAsync(int userId, int taskId, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.Project).Include(x => x.Assignee).FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t is null || t.Project!.OwnerId != userId) return null;
        return ToDto(t);
    }

    public async Task<TaskDto?> AssignAsync(int userId, int taskId, int? assigneeId, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t is null || t.Project!.OwnerId != userId) return null;
        t.AssigneeId = assigneeId;
        t.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await LoadDto(t.Id, ct);
    }

    private async Task<TaskDto?> LoadDto(int taskId, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.Assignee).FirstOrDefaultAsync(x => x.Id == taskId, ct);
        return t is null ? null : ToDto(t);
    }

    private static TaskDto ToDto(TaskItem t) =>
        new(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.DueOn, t.AssigneeId, t.Assignee?.Name, t.CreatedAt, t.UpdatedAt);
}
