using API_Calinout_Project.Configurations;
using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Services.Features;
using API_Calinout_Project.Services.Interfaces.Features;
using API_Calinout_Project.Shared;
using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Calinout.Tests.Unit.Services.Features
{
    public class FoodServiceTests
    {
        private readonly ApplicationDbContext _db;
        private readonly FoodService _sut;


        public FoodServiceTests()
        {
            var option = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            _db = new ApplicationDbContext(option);

            MappingConfig.Configure();

            var logger = NullLogger<IFoodService>.Instance;

            _sut = new FoodService(_db, logger);
        }

        // ── Test 1: Happy path ───────────────────────────────────────────────────
        // What: a valid userId + valid request should create and return the food.
        // Why: confirms the full happy path — DB write + macro calculation + mapping.
        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsCreatedFood()
        {
            // Arrange ────────────────────────────────────────────────────────
            // Seed a FoodType so FindAsync(request.FoodTypeId) finds something.
            // Without this the service returns NotFound before doing anything.
            var foodType = new FoodType
            {
                Id = 1,
                Name = "Chicken Breast",
                BaseWeightInGrams = 100,
                Calories = 165,
                Protein = 31,
                Fat = 3.6m,
                Carbs = 0,
                UserId = "user-1"
            };
            _db.FoodTypes.Add(foodType);
            await _db.SaveChangesAsync();

            var request = new CreateFoodRequest
            {
                FoodTypeId = 1,
                WeightInGrams = 200,      // double the base weight
                ConsumedAt = DateTime.UtcNow,
                IsTemplate = false
            };

            // Act ─────────────────────────────────────────────────────────────
            var result = await _sut.CreateAsync("user-1", request);

            // Assert ──────────────────────────────────────────────────────────
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();

            // 200g is 2x base weight of 100g, so macros should double
            result.Value!.Calories.Should().Be(330);   // 165 * 2
            result.Value.Protein.Should().Be(62);      // 31 * 2
            result.Value.WeightInGrams.Should().Be(200);
        }

        // ── Test 2: Null userId ──────────────────────────────────────────────────
        // What: null userId should immediately return Unauthorized.
        // Why: userId comes from JWT claims. If it's missing, nothing should run.
        // This is a security boundary test — not just a null check test.
        [Fact]
        public async Task CreateAsync_NullUserId_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CreateFoodRequest { FoodTypeId = 1, WeightInGrams = 100 };

            // Act
            var result = await _sut.CreateAsync(null!, request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Unauthorized);

            // Also verify nothing was written to the DB
            // A security failure should never persist data
            _db.Foods.Should().BeEmpty();
        }

        // ── Test 3: FoodType not found ───────────────────────────────────────────
        // What: requesting a FoodTypeId that doesn't exist returns NotFound.
        // Why: the service should guard against orphaned food entries.
        // Note: we seed NO FoodType here intentionally.
        [Fact]
        public async Task CreateAsync_FoodTypeNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateFoodRequest { FoodTypeId = 999, WeightInGrams = 100 };

            // Act
            var result = await _sut.CreateAsync("user-1", request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.NotFound);
        }

        // ── Test 4: GetById — owner gets their food ──────────────────────────────
        // What: the correct user can retrieve their own food.
        // Why: baseline — before testing the security case, confirm the happy path works.
        [Fact]
        public async Task GetByIdAsync_OwnFood_ReturnsFood()
        {
            // Arrange
            var foodType = new FoodType
            {
                Id = 1,
                Name = "Eggs",
                BaseWeightInGrams = 100,
                Calories = 155,
                Protein = 13,
                Fat = 11,
                Carbs = 1.1m,
                UserId = "user-1"
            };
            var food = new Food
            {
                Id = 1,
                UserId = "user-1",
                FoodTypeId = 1,
                FoodType = foodType,
                Name = "Eggs",
                WeightInGrams = 100,
                Calories = 155,
                Protein = 13,
                Fat = 11,
                Carbs = 1.1m,
                CreatedAt = DateTime.UtcNow,
                ConsumedAt = DateTime.UtcNow
            };
            _db.FoodTypes.Add(foodType);
            _db.Foods.Add(food);
            await _db.SaveChangesAsync();

            // Act
            var result = await _sut.GetByIdAsync("user-1", 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(1);
        }

        // ── Test 5: GetById — another user's food returns Forbidden ──────────────
        // What: user-2 tries to read user-1's food and gets denied.
        // Why: this is the critical security boundary. Without this check, any
        // authenticated user can read any other user's food just by guessing an id.
        // A hiring manager will look for exactly this kind of test.
        [Fact]
        public async Task GetByIdAsync_AnotherUsersFood_ReturnsForbidden()
        {
            // Arrange — food belongs to user-1
            var foodType = new FoodType
            {
                Id = 2,
                Name = "Rice",
                BaseWeightInGrams = 100,
                Calories = 130,
                Protein = 2.7m,
                Fat = 0.3m,
                Carbs = 28,
                UserId = "user-1"
            };
            var food = new Food
            {
                Id = 2,
                UserId = "user-1", // ← belongs to user-1
                FoodTypeId = 2,
                FoodType = foodType,
                Name = "Rice",
                WeightInGrams = 100,
                Calories = 130,
                CreatedAt = DateTime.UtcNow
            };
            _db.FoodTypes.Add(foodType);
            _db.Foods.Add(food);
            await _db.SaveChangesAsync();

            // Act — user-2 tries to access it
            var result = await _sut.GetByIdAsync("user-2", 2);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ErrorType.Forbidden);

            // Why check ErrorType and not just IsSuccess?
            // IsSuccess = false could mean NotFound, Unauthorized, or Forbidden.
            // We want to confirm the service FOUND the food but DENIED access —
            // not that it pretended the food doesn't exist. Both are valid security
            // strategies (404 vs 403) but you should be explicit about which you chose.
        }
    }
}