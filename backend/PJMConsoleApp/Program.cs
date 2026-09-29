using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PJMConsoleApp
{
    public class SqlDbContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var connectionString = config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            // Your code here
            Console.WriteLine("App running...");
            using (var resource = new SqlDbContext())
            {
                resource.Database.EnsureCreated();
                resource.Projects.CreateDbCommand();
                resource.Projects.Add(new Project
                {
                    Name = "New Project",
                    Description = "Project Description",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                });
                resource.SaveChanges();
                // Use the resource here
            }
        }
    }
}