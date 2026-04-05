using API_Calinout_Project.Entities;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace API_Calinout_Project.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(WebApplication app)
    {
        // Create a scope to get services (DbContext, Managers)
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Ensure Database is Created & Migrated
            // This runs 'Update-Database' automatically at startup.
            // Great for Docker/Production, but safe to use in Dev too.
            await context.Database.MigrateAsync();

            // 2. Seed Roles
            await SeedRolesAsync(roleManager);

        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Define the roles your app needs
        string[] roleNames = { "Admin", "User" };

        foreach (var roleName in roleNames)
        {
            // Check if role exists
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                // Create it if it doesn't
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}