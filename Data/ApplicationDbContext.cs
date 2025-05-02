using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestApi.Models;

namespace RestApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSets for your entities
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<LoginDetails> LoginDetails { get; set; }
        public DbSet<Organization> Organization { get; set; }
        public DbSet<OrganizationUsers> OrganizationUsers { get; set; }

        public DbSet<Permissions> Permissions { get; set; }
        public DbSet<OrganizationUserPermissions> OrganizationUserPermissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
                var dbName = Environment.GetEnvironmentVariable("DB_NAME");
                var dbUser = Environment.GetEnvironmentVariable("DB_USER");
                var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

                string connectionString = $"Server=tcp:{dbServer},1433;" +
                                          $"Initial Catalog={dbName};" +
                                          "Persist Security Info=False;" +
                                          $"User ID={dbUser};" +
                                          $"Password={dbPassword};" +
                                          "MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Add custom logic here if needed (e.g., audit logging)
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
