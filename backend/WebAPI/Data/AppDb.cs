namespace WebAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebAPI.Models;
public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
}
