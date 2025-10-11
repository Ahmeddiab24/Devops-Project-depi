using Microsoft.EntityFrameworkCore;
using libray2.Models;
using Microsoft.AspNetCore.Identity;

namespace libray2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add DbContext with SQLite
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add Identity
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    // Disable user registration
                    options.User.RequireUniqueEmail = true;
                    // Disable password recovery (requires scaffolding Identity UI to fully remove pages)
                    // A more robust approach is to remove the links and not handle the routes if UI is not scaffolded.
                    // We will handle removing links and ensure no direct access possible if UI is not scaffolded.
                })
                .AddRoles<IdentityRole>() // Enable roles
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddRazorPages(); // Required for Identity UI pages

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages(); // Map Identity UI pages
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Create default Admin user and role, and initialize settings
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Create Admin Role if it doesn't exist
                if (!roleManager.RoleExistsAsync("Admin").Result)
                {
                    roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
                }

                // Create default Admin user if it doesn't exist
                var adminUser = userManager.FindByEmailAsync("admin@codgoo.com").Result;
                if (adminUser == null)
                {
                    adminUser = new IdentityUser { UserName = "admin@codgoo.com", Email = "admin@codgoo.com", EmailConfirmed = true };
                    var createResult = userManager.CreateAsync(adminUser, "AdminPassword123!").Result; // <--- Change this to a strong password

                    if(createResult.Succeeded)
                    {
                         // Assign Admin role to the user
                        userManager.AddToRoleAsync(adminUser, "Admin").Wait();
                    }
                }

                // Initialize default settings if they don't exist
                if (!context.Settings.Any())
                {
                    context.Settings.Add(new Settings { PrivateRoomRatePerHour = 10.0m, SharedRoomRatePerHour = 5.0m });
                    context.SaveChanges();
                }
            }

            app.Run();
        }
    }
}