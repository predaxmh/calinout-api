using API_Calinout.Tests.Infrastructure;
using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace API_Calinout.Tests.E2E;

// ── IClassFixture<T> ──────────────────────────────────────────────────────────
// Tells xUnit: create ONE instance of CalinoutWebApplicationFactory,
// share it across every test in this class, then dispose it when done.
// Without this, each test would boot and tear down the entire ASP.NET
// application — extremely slow.
//
// Compare with IAsyncLifetime below — IClassFixture is class-level (once),
// IAsyncLifetime is test-level (before/after each test).
public class FoodEndpointTests
    : IClassFixture<CalinoutWebApplicationFactory>, IAsyncLifetime
{
    private readonly CalinoutWebApplicationFactory _factory;
    private HttpClient _client = null!;

    // ── Constructor injection ─────────────────────────────────────────────────
    // xUnit injects the shared factory here because of IClassFixture<T>.
    // By the time this constructor runs, InitializeAsync on the factory
    // has already completed — the DB schema exists and Mapster is configured.
    public FoodEndpointTests(CalinoutWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── IAsyncLifetime.InitializeAsync — runs before EACH test ───────────────
    // Creates a fresh HttpClient for each test.
    // Why fresh? HttpClient carries the Authorization header from the previous
    // test if reused. A fresh client starts with no headers — each test
    // authenticates as the user it needs, not whoever the last test used.
    //
    // Also seeds the FoodType if it doesn't exist.
    // Why check first? The factory is shared — the FoodType seeded by test 1
    // is still in the database when test 2 runs. Inserting again would violate
    // the unique index on FoodType.Name.
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        if (!await db.FoodTypes.AnyAsync(ft => ft.Id == 1))
        {
            db.FoodTypes.Add(new FoodType
            {
                Id = 1,
                Name = "Chicken Breast",
                BaseWeightInGrams = 100,
                Calories = 165,
                Protein = 31,
                Fat = 3.6m,
                Carbs = 0,
                UserId = "system"
            });
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth helper ───────────────────────────────────────────────────────────
    // Simulates the full auth flow a real user performs:
    //   1. POST /register  → creates Identity user in the DB
    //   2. POST /login     → verifies password, returns JWT + refresh token
    //   3. Attach JWT      → all subsequent requests carry the token
    //
    // Each test passes a unique email so tests don't collide on the
    // unique email constraint in the Users table.
    private async Task<string> AuthenticateAsync(string email)
    {
        // Register — creates ApplicationUser via UserManager
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                email,
                password = "Test1234!",
                confirmPassword = "Test1234!"
            });

        // 200 = new registration, Conflict = already exists (test reruns)
        // Both are acceptable — we proceed to login either way
        registerResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Conflict);

        // Login — goes through full pipeline:
        // Middleware → AuthController → AuthService → UserManager
        //   → password check → TokenService → JWT creation → response
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Test1234!" });

        loginResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            because: $"login must succeed for {email}");

        var auth = await loginResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();

        // Attach JWT to the client's default headers.
        // Every subsequent request from this client automatically includes:
        //   Authorization: Bearer <jwt>
        // This is exactly what Flutter's AuthInterceptor does on every request.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return auth.AccessToken;
    }

    // ── Test 15 ───────────────────────────────────────────────────────────────
    // What: no token → 401
    // Why: proves UseAuthentication middleware is correctly placed and configured
    //      in Program.cs. If this fails, ALL endpoints are publicly accessible.
    // Pipeline path: Request → UseAuthentication (no token found → 401)
    //                        → never reaches controller
    [Fact]
    public async Task PostFood_WithoutToken_Returns401()
    {
        // No AuthenticateAsync call — client has no Authorization header
        var response = await _client.PostAsJsonAsync("/api/v1/foods", new
        {
            foodTypeId = 1,
            weightInGrams = 150
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Test 16 ───────────────────────────────────────────────────────────────
    // What: valid token + valid body → 201 Created with correct macros
    // Why: proves the entire vertical slice works together —
    //   JWT validated → userId extracted from claims → FoodService.CreateAsync
    //   → macro calculation → EF write → Mapster map → 201 response
    // The assertion on userId specifically proves it came from the JWT claim,
    // not from the request body (which has no userId field at all).
    [Fact]
    public async Task PostFood_Authenticated_Returns201WithCorrectMacros()
    {
        await AuthenticateAsync("create@test.com");

        var response = await _client.PostAsJsonAsync("/api/v1/foods", new
        {
            foodTypeId = 1,
            weightInGrams = 200,
            consumedAt = DateTime.UtcNow,
            isTemplate = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var food = await response.Content.ReadFromJsonAsync<FoodResponse>();
        food.Should().NotBeNull();
        food!.WeightInGrams.Should().Be(200);
        food.Calories.Should().Be(330);   // 165 * 2
        food.Protein.Should().Be(62);     // 31 * 2

        // Query the DB directly — bypasses the service to verify
        // EF actually persisted the record, not just returned an object
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var inDb = await db.Foods
            .FirstOrDefaultAsync(f => f.WeightInGrams == 200);

        inDb.Should().NotBeNull();
        inDb!.UserId.Should().NotBeNullOrEmpty(
            because: "userId must be injected from JWT claims, never from request body");
    }

    // ── Test 17 ───────────────────────────────────────────────────────────────
    // What: user B requests user A's food → 403 Forbidden
    // Why: the integration test proved isolation at the service layer.
    //      This proves it at the HTTP layer — going through controller,
    //      auth middleware, and service together.
    //      A controller bug (returning 200 regardless of service result)
    //      would pass integration tests but fail this one.
    // Pipeline path: Request → UseAuthentication (validates user B's token)
    //   → UseAuthorization (user B is authenticated, so passes)
    //   → FoodController.GetById → FoodService.GetByIdAsync
    //   → service finds food, checks userId != user B → Forbidden result
    //   → controller maps Result.Forbidden → 403 response
    [Fact]
    public async Task GetFood_BelongingToAnotherUser_Returns403()
    {
        // User A creates food
        await AuthenticateAsync("userA@test.com");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/foods", new
        {
            foodTypeId = 1,
            weightInGrams = 100,
            consumedAt = DateTime.UtcNow,
            isTemplate = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "user A must create food before user B can attempt to read it");

        var created = await createResponse.Content
            .ReadFromJsonAsync<FoodResponse>();

        // Switch to user B — fresh client, fresh JWT, no trace of user A
        // If we reused _client, user A's Authorization header would still
        // be attached and the test would prove nothing about user B
        _client = _factory.CreateClient();
        await AuthenticateAsync("userB@test.com");

        var getResponse = await _client
            .GetAsync($"/api/v1/foods/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}