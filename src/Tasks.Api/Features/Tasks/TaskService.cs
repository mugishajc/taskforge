using Microsoft.EntityFrameworkCore;
using Tasks.Api.Data;
using Tasks.Api.Domain;

namespace Tasks.Api.Features.Tasks;

public record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueOn);
public record UpdateTaskRequest(string Title, string? Description, Domain.TaskStatus Status, TaskPriority Priority, DateTime? DueOn);
public record TaskDto(int Id, int ProjectId, string Title, string? Description, Domain.TaskStatus Status, TaskPriority Priority, DateTime? DueOn);

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
            DueOn = req.DueOn
        };
        db.Tasks.Add(t);
        await db.SaveChangesAsync(ct);
        return ToDto(t);
    }

    public async Task<List<TaskDto>> ListAsync(int userId, int projectId, Domain.TaskStatus? status, CancellationToken ct)
    {
        var q = db.Tasks
            .Where(t => t.ProjectId == projectId && t.Project!.OwnerId == userId);
        if (status is not null) q = q.Where(t => t.Status == status);
        return await q.OrderBy(t => t.Id)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.DueOn))
            .ToListAsync(ct);
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
        await db.SaveChangesAsync(ct);
        return ToDto(t);
    }

    public async Task<bool> DeleteAsync(int userId, int taskId, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t is null || t.Project!.OwnerId != userId) return false;
        db.Tasks.Remove(t);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TaskDto ToDto(TaskItem t) =>
        new(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.DueOn);
}
