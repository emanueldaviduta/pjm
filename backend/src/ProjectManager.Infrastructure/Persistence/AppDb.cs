namespace ProjectManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;
public class AppDb : DbContext, IAppDbContext, IDataProtectionKeyContext
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

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.HasData(
                new Role { Id = 1, Name = "Owner" },
                new Role { Id = 2, Name = "Member" },
                new Role { Id = 3, Name = "Admin" });
        });

        // Join tables: the composite key itself rejects duplicates. A membership is meaningless
        // without its user or project (cascade); a role that is in use cannot be removed (restrict).
        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectUserRole>(e =>
        {
            e.HasKey(pur => new { pur.ProjectId, pur.UserId, pur.RoleId });
            e.HasOne(pur => pur.Project).WithMany().HasForeignKey(pur => pur.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pur => pur.User).WithMany().HasForeignKey(pur => pur.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pur => pur.Role).WithMany().HasForeignKey(pur => pur.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        // Single row; the id is always MailSettings.SingletonId, never generated.
        modelBuilder.Entity<MailSettings>(e =>
        {
            e.Property(m => m.Id).ValueGeneratedNever();
            e.HasOne(m => m.Project).WithMany().HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        // The unique key is what guarantees an email is processed at most once, even across replicas.
        modelBuilder.Entity<InboundEmail>(e =>
        {
            e.HasIndex(i => new { i.MailboxKey, i.MessageKey }).IsUnique();
            e.HasOne(i => i.TaskItem).WithMany().HasForeignKey(i => i.TaskItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskAttachment>()
            .HasOne(a => a.TaskItem)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<ProjectUserRole> ProjectUserRoles { get; set; }
    public DbSet<MailSettings> MailSettings { get; set; }
    public DbSet<InboundEmail> InboundEmails { get; set; }
    public DbSet<TaskAttachment> TaskAttachments { get; set; }

    /// <summary>ASP.NET Data Protection key ring (protects the mailbox password); kept in the database so it survives image rebuilds.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}
