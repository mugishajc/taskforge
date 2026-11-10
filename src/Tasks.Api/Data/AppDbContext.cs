using Microsoft.EntityFrameworkCore;
using Tasks.Api.Domain;

namespace Tasks.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(254);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120).HasDefaultValue("");
            e.Property(x => x.PasswordHash).IsRequired();
        });
        b.Entity<Project>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        });
        b.Entity<TaskItem>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Priority).HasConversion<string>();
            e.HasOne(x => x.Project).WithMany(p => p.Tasks).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).IsRequired(false);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AssigneeId);
        });
        b.Entity<Comment>(e =>
        {
            e.Property(x => x.Body).IsRequired().HasMaxLength(2000);
            e.HasOne(x => x.TaskItem).WithMany(t => t.Comments).HasForeignKey(x => x.TaskItemId);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId);
        });
    }
}
