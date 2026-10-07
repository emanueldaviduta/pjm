namespace ProjectManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;
public class AppDb : DbContext, IAppDbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Assigned)
            .WithMany()
            .HasForeignKey(t => t.AssignedId);
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
}
