using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


var builder = WebApplication.CreateBuilder(args);


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
                // Use the resource here
            }

            
            
        }
    }
}
