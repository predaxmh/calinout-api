using API_Calinout_Project.Configurations;
using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Services.Features;
using API_Calinout_Project.Services.Interfaces.Features;
using API_Calinout_Project.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace API_Calinout.Tests.Integration;

// ── Why this is an integration test, not a unit test ─────────────────────────
// Unit tests test ONE thing in isolation with mocks replacing dependencies.
// This test uses:
//   - Real FoodService (no mock)
//   - Real ApplicationDbContext (InMemory, but real EF pipeline)
//   - Real Mapster mappings
//   - Real data flowing through all three layers together
//
// What we're proving: the pieces work correctly TOGETHER, not just individually.
// A unit test could pass while the integration silently breaks — for example,
// if Mapster maps a property incorrectly, unit tests with mocked responses
// would never catch it. This test would.
public class FoodIntegrationTests : IAsyncLifetime
{
    private ApplicationDbContext _db = null!;
    private FoodService _foodService = null!;

    // ── IAsyncLifetime ────────────────────────────────────────────────────────
    // This is xUnit's async setup/teardown interface.
    // Use it instead of the constructor when setup requires async work — 
    // constructors cannot be async in C#.
    // InitializeAsync runs before each test.
    // DisposeAsync runs after each test.
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        MappingConfig.Configure();

        _foodService = new FoodService(_db, NullLogger<IFoodService>.Instance);

        // ── Seed shared data ─────────────────────────────────────────────
        // FoodTypes are reference data — shared between users, like a catalogue.
        // Both users will log food against the same FoodType.
        var foodTypes = new[]
        {
            new FoodType
            {
                Id = 1,
                Name = "Chicken Breast",
                BaseWeightInGrams = 100,
                Calories = 165,
                Protein = 31,
                Fat = 3.6m,
                Carbs = 0,
                UserId = "system"
            },
            new FoodType
            {
                Id = 2,
                Name = "Brown Rice",
                BaseWeightInGrams = 100,
                Calories = 130,
                Protein = 2.7m,
                Fat = 0.9m,
                Carbs = 28,
                UserId = "system"
            }
        };

        _db.FoodTypes.AddRange(foodTypes);
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    // ── Test 12: Full create → retrieve round trip ────────────────────────────
    // What: create a food then retrieve it by id — verify data survived the
    // full pipeline: service → EF → InMemory → EF → service → Mapster → DTO.
    // Why: confirms that what goes in comes back out correctly mapped.
    // If any layer silently drops or transforms a field, this catches it.
    [Fact]
    public async Task CreateThenGetById_RoundTrip_DataIntegrityPreserved()
    {
        // Arrange
        var request = new CreateFoodRequest
        {
            FoodTypeId = 1,
            WeightInGrams = 150,
            ConsumedAt = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            IsTemplate = false
        };

        // Act — Step 1: create
        var createResult = await _foodService.CreateAsync("user-1", request);
        createResult.IsSuccess.Should().BeTrue("creation should succeed before we test retrieval");

        var createdId = createResult.Value!.Id;

        // Act — Step 2: retrieve the same food
        var getResult = await _foodService.GetByIdAsync("user-1", createdId);

        // Assert — data integrity through the full pipeline
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.WeightInGrams.Should().Be(150);
        getResult.Value.Calories.Should().Be(247.5m);  // 165 * 1.5
        getResult.Value.Protein.Should().Be(46.5m);    // 31 * 1.5
        getResult.Value.ConsumedAt.Should().Be(request.ConsumedAt);

        // Verify it's actually persisted — query the DB directly,
        // bypassing the service entirely, to confirm EF wrote it.
        var inDb = await _db.Foods.FindAsync(createdId);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be("user-1");
    }

    // ── Test 13: Two users, data isolation ───────────────────────────────────
    // What: user-1 and user-2 both create food in the same database.
    //       Each can only see their own. Neither can see the other's.
    // Why: this is the most realistic security scenario. In production,
    // all users share one database. The WHERE userId = @userId clause
    // in every query is the only thing separating their data.
    // This test proves that isolation holds end-to-end, not just at
    // the service method level.
    [Fact]
    public async Task TwoUsers_CreateFood_CannotAccessEachOthersData()
    {
        // Arrange — both users create food against the same FoodType
        var user1Request = new CreateFoodRequest
        {
            FoodTypeId = 1,
            WeightInGrams = 100,
            ConsumedAt = DateTime.UtcNow,
            IsTemplate = false
        };

        var user2Request = new CreateFoodRequest
        {
            FoodTypeId = 2,
            WeightInGrams = 200,
            ConsumedAt = DateTime.UtcNow,
            IsTemplate = false
        };

        // Act — both create their food
        var user1Create = await _foodService.CreateAsync("user-1", user1Request);
        var user2Create = await _foodService.CreateAsync("user-2", user2Request);

        user1Create.IsSuccess.Should().BeTrue();
        user2Create.IsSuccess.Should().BeTrue();

        var user1FoodId = user1Create.Value!.Id;
        var user2FoodId = user2Create.Value!.Id;

        // Assert — user-1 can read their own food
        var user1GetsOwn = await _foodService.GetByIdAsync("user-1", user1FoodId);
        user1GetsOwn.IsSuccess.Should().BeTrue();

        // Assert — user-2 can read their own food
        var user2GetsOwn = await _foodService.GetByIdAsync("user-2", user2FoodId);
        user2GetsOwn.IsSuccess.Should().BeTrue();

        // Assert — user-1 CANNOT read user-2's food
        var user1GetsUser2Food = await _foodService.GetByIdAsync("user-1", user2FoodId);
        user1GetsUser2Food.IsSuccess.Should().BeFalse();
        user1GetsUser2Food.ErrorType.Should().Be(ErrorType.Forbidden);

        // Assert — user-2 CANNOT read user-1's food
        var user2GetsUser1Food = await _foodService.GetByIdAsync("user-2", user1FoodId);
        user2GetsUser1Food.IsSuccess.Should().BeFalse();
        user2GetsUser1Food.ErrorType.Should().Be(ErrorType.Forbidden);

        // Final check — confirm the DB actually has exactly 2 food entries total
        // and each belongs to the correct user. This rules out any scenario where
        // the service returns the wrong data without the DB reflecting it.
        var allFoods = await _db.Foods.ToListAsync();
        allFoods.Should().HaveCount(2);
        allFoods.Should().ContainSingle(f => f.UserId == "user-1");
        allFoods.Should().ContainSingle(f => f.UserId == "user-2");
    }

    // ── Test 14: Delete — only owner can delete ───────────────────────────────
    // What: user-2 tries to delete user-1's food → denied.
    //       user-1 deletes their own food → succeeds, DB record gone.
    // Why: combines authorization + persistence verification in one flow.
    // Proves that a failed delete attempt leaves data untouched.
    [Fact]
    public async Task DeleteAsync_OnlyOwnerCanDelete_UnauthorizedAttemptLeavesDataIntact()
    {
        // Arrange — create food for user-1
        var createResult = await _foodService.CreateAsync("user-1", new CreateFoodRequest
        {
            FoodTypeId = 1,
            WeightInGrams = 100,
            ConsumedAt = DateTime.UtcNow,
            IsTemplate = false
        });

        var foodId = createResult.Value!.Id;

        // Act — user-2 tries to delete it
        var unauthorizedDelete = await _foodService.DeleteAsync("user-2", foodId);

        // Assert — denied
        unauthorizedDelete.IsSuccess.Should().BeFalse();
        unauthorizedDelete.ErrorType.Should().Be(ErrorType.Forbidden);

        // Food must still exist after the failed attempt
        var stillExists = await _db.Foods.FindAsync(foodId);
        stillExists.Should().NotBeNull("unauthorized delete must not remove data");

        // Act — owner deletes it
        var authorizedDelete = await _foodService.DeleteAsync("user-1", foodId);

        // Assert — succeeds and is gone from DB
        authorizedDelete.IsSuccess.Should().BeTrue();
        var nowGone = await _db.Foods.FindAsync(foodId);
        nowGone.Should().BeNull("food should be removed after authorized delete");
    }
}