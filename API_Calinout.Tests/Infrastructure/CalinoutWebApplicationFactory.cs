using API_Calinout_Project.Data; // Update with your actual namespace
using API_Calinout_Project.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace API_Calinout.Tests.Infrastructure;

public class CalinoutWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private DbConnection _connection = null!;

    public async Task InitializeAsync()
    {
        // 1. Create and open the SQLite connection. 
        // Keeping this open keeps the in-memory database alive for the test run.
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        try
        {
            using (var scope = Services.CreateScope()) // 'Services' is now available
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();

                // Ensure schema is ready
                await db.Database.EnsureCreatedAsync();

                // Seed Roles
                string[] roleNames = { "Admin", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log this so you can see if the seeding itself is crashing
            throw new Exception("Seeding failed in WebApplicationFactory", ex);
        }


    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(async services =>
        {
            // 2. THE .NET 9 FIX: Remove ALL existing DbContext configurations
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbConnection) ||
                    d.ServiceType.Name.Contains("IDbContextOptionsConfiguration")) // Crucial for .NET 9
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 3. Register ApplicationDbContext with SQLite
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // 4. Build the schema (This triggers your OnModelCreating to rename Identity tables)
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();




        });
    }

    public new async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        await base.DisposeAsync();
    }
}