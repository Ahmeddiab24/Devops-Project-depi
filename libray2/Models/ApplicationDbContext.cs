using Microsoft.EntityFrameworkCore;
using libray2.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace libray2.Models
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WorkspaceEntry> WorkspaceEntries { get; set; }
        public DbSet<Settings> Settings { get; set; }
    }
} 