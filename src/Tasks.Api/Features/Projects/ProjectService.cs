using Microsoft.EntityFrameworkCore;
using Tasks.Api.Data;
using Tasks.Api.Domain;

namespace Tasks.Api.Features.Projects;

public record CreateProjectRequest(string Name);
public record ProjectDto(int Id, string Name, int OwnerId, DateTime CreatedAt);

public class ProjectService(AppDbContext db)
{
    public async Task<ProjectDto> CreateAsync(int ownerId, CreateProjectRequest req, CancellationToken ct)
    {
        var p = new Project { Name = req.Name.Trim(), OwnerId = ownerId };
        db.Projects.Add(p);
        await db.SaveChangesAsync(ct);
        return ToDto(p);
    }

    public async Task<List<ProjectDto>> ListForUserAsync(int ownerId, CancellationToken ct) =>
        await db.Projects.Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.Id)
            .Select(p => new ProjectDto(p.Id, p.Name, p.OwnerId, p.CreatedAt))
            .ToListAsync(ct);

    public async Task<ProjectDto?> GetForUserAsync(int ownerId, int id, CancellationToken ct)
    {
        var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<bool> DeleteAsync(int ownerId, int id, CancellationToken ct)
    {
        var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId, ct);
        if (p is null) return false;
        db.Projects.Remove(p);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static ProjectDto ToDto(Project p) => new(p.Id, p.Name, p.OwnerId, p.CreatedAt);
}
