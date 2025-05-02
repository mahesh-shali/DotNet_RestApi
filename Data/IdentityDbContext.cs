// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using RestApi.Models;

// public class ApplicationDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<IdentityUser>
// {
//     public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
//         : base(options)
//     {
//     }
//     public DbSet<User> Users { get; set; }
//     public DbSet<Role> Roles { get; set; }
//     public DbSet<LoginDetails> LoginDetails { get; set; }

//     internal async Task SaveChangesAsync()
//     {
//         throw new NotImplementedException();
//     }
// }

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestApi.Models;

public class ApplicationDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<LoginDetails> LoginDetails { get; set; }
    public DbSet<Organization> Organization { get; set; }
    public DbSet<OrganizationUsers> OrganizationUsers { get; set; }
    public DbSet<Permissions> Permissions { get; set; }
    public DbSet<OrganizationUserPermissions> OrganizationUserPermissions { get; set; }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add custom logic here if needed (e.g., audit logging)

        return await base.SaveChangesAsync(cancellationToken);
    }
}

