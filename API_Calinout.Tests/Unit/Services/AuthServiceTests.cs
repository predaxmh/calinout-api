using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Services;
using API_Calinout_Project.Services.Interfaces;
using API_Calinout_Project.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace API_Calinout.Tests.Unit.Services;

public class AuthServiceTests
{
    // ── The mocks ────────────────────────────────────────────────────────────
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // ── UserManager mock ─────────────────────────────────────────────
        // UserManager is not an interface — it's a concrete class with
        // virtual methods. Moq can only mock virtual/abstract members.
        // The constructor requires IUserStore at minimum. Everything else
        // can be null because UserManager checks internally before using them.
        // This is the standard pattern used everywhere in .NET testing.
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object,   // required
            null,           // IOptions<IdentityOptions>
            null,           // IPasswordHasher
            null,           // IEnumerable<IUserValidator>
            null,           // IEnumerable<IPasswordValidator>
            null,           // ILookupNormalizer
            null,           // IdentityErrorDescriber
            null,           // IServiceProvider
            null            // ILogger
        );

        // ── ITokenService mock ───────────────────────────────────────────
        // This IS an interface — straightforward Moq, no tricks needed.
        _tokenServiceMock = new Mock<ITokenService>();

        // ── IHttpContextAccessor mock ────────────────────────────────────
        // AuthService calls GetIpAddress() which reads from HttpContext.
        // We set up a fake HttpContext that returns a known IP string
        // so the test doesn't crash on a null reference.
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _sut = new AuthService(
            _userManagerMock.Object,
            NullLogger<AuthService>.Instance,
            _tokenServiceMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    // ── Test 8: Login — user not found ───────────────────────────────────────
    // What: email doesn't exist → returns failure with generic message.
    // Why: notice the error message is "Invalid email or password" — NOT
    // "User not found". This is intentional security design. If you returned
    // "User not found", an attacker could enumerate valid emails by trying
    // many addresses and seeing which ones say "wrong password" vs "not found".
    // Generic messages prevent user enumeration attacks.
    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsGenericFailure()
    {
        // Arrange
        // Tell the mock: when FindByEmailAsync is called with any string,
        // return null — simulating a user that doesn't exist in the database.
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new LoginRequestDto
        {
            Email = "ghost@test.com",
            Password = "Whatever123!"
        };

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();

        // Critical: the message must NOT reveal whether the user exists.
        // If this test fails because you changed the error message to
        // "User not found", that's a real security regression.
        result.Error.Should().Be("Invalid email or password.");

        // Also verify: since user wasn't found, password check should
        // never have been called. Calling CheckPasswordAsync on a null
        // user would throw — verify we short-circuited correctly.
        _userManagerMock.Verify(
            x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }

    // ── Test 9: Login — wrong password ───────────────────────────────────────
    // What: user exists but password is wrong → same generic message.
    // Why: same anti-enumeration principle. Both "not found" and "wrong
    // password" must return identical messages. This test and Test 8 together
    // prove both failure paths produce the same output — a hiring manager
    // reading these two tests immediately understands your security intent.
    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsGenericFailure()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "real@test.com",
            UserName = "real@test.com"
        };

        // User IS found this time
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("real@test.com"))
            .ReturnsAsync(user);

        // But password check fails
        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false);

        var request = new LoginRequestDto
        {
            Email = "real@test.com",
            Password = "WrongPassword!"
        };

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password.");

        // Verify password WAS checked this time (unlike Test 8)
        // This confirms the code reached the password check step.
        _userManagerMock.Verify(
            x => x.CheckPasswordAsync(user, "WrongPassword!"),
            Times.Once
        );

        // Token service should never have been called — no token for failed login
        _tokenServiceMock.Verify(
            x => x.GenerateTokensAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    // ── Test 10: Login — valid credentials ───────────────────────────────────
    // What: correct email and password → token generation is called and
    // the result is returned as-is from ITokenService.
    // Why: AuthService is a thin orchestrator for the login flow. This test
    // confirms it wires the pieces together correctly — it finds the user,
    // checks the password, then delegates token creation to ITokenService.
    // We don't test the token content here — that belongs in TokenService tests.
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenFromTokenService()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "real@test.com",
            UserName = "real@test.com"
        };

        var expectedResponse = new AuthResponseDto(
            AccessToken: "fake.jwt.token",
            RefreshToken: "fake-refresh-token",
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
        );

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("real@test.com"))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, "CorrectPassword!"))
            .ReturnsAsync(true);

        // Token service returns a successful result with our fake token
        _tokenServiceMock
            .Setup(x => x.GenerateTokensAsync(user, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Success(expectedResponse));

        var request = new LoginRequestDto
        {
            Email = "real@test.com",
            Password = "CorrectPassword!"
        };

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("fake.jwt.token");

        // Verify the full chain executed in order:
        // 1. User was looked up
        _userManagerMock.Verify(x => x.FindByEmailAsync("real@test.com"), Times.Once);
        // 2. Password was verified
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "CorrectPassword!"), Times.Once);
        // 3. Token was generated
        _tokenServiceMock.Verify(
            x => x.GenerateTokensAsync(user, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    // ── Test 11: Register — duplicate email ───────────────────────────────────
    // What: registering with an already-used email returns failure.
    // Why: tests the conflict guard. Also verifies CreateAsync is never called
    // when the email already exists — no partial user creation.
    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange — FindByEmailAsync returns an existing user
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Email = "taken@test.com" });

        var request = new RegisterRequestDto
        {
            Email = "taken@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"

        };

        // Act
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already in use");

        // CreateAsync should never be called when email is taken
        _userManagerMock.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }
}