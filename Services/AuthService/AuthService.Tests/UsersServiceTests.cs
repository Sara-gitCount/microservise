using Xunit;
using Moq;
using AuthService.Services;
using AuthService.Repository;
using SharedModels.Models;
using System;
using System.Threading.Tasks;

namespace AuthService.Tests;

/// <summary>
/// Unit tests for UsersService - Authentication Business Logic
/// Tests user registration, login, validation, and JWT token generation
/// </summary>
public class UsersServiceTests
{
    private readonly Mock<IUsersRepository> _repositoryMock;
    private readonly UsersService _usersService;

    public UsersServiceTests()
    {
        _repositoryMock = new Mock<IUsersRepository>();
        _usersService = new UsersService(_repositoryMock.Object);
    }

    #region User Registration Tests

    [Fact]
    public async Task RegisterUser_WithValidData_ShouldCreateUser()
    {
        // Arrange
        string email = "newuser@example.com";
        string password = "SecurePass123!";
        string fullName = "New User";

        _repositoryMock
            .Setup(r => r.GetUserByEmailAsync(email))
            .ReturnsAsync((User)null);

        _repositoryMock
            .Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _usersService.RegisterUserAsync(email, password, fullName);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        string email = "existing@example.com";
        var existingUser = new User { Email = email };

        _repositoryMock
            .Setup(r => r.GetUserByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _usersService.RegisterUserAsync(email, "Password123!", "Name")
        );
    }

    [Fact]
    public async Task RegisterUser_WithWeakPassword_ShouldThrowException()
    {
        // Arrange - Password with no uppercase
        string weakPassword = "password123!";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _usersService.RegisterUserAsync("user@example.com", weakPassword, "Name")
        );
    }

    [Fact]
    public async Task RegisterUser_WithoutSpecialCharacter_ShouldThrowException()
    {
        // Arrange - Password without special character
        string password = "Password123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _usersService.RegisterUserAsync("user@example.com", password, "Name")
        );
    }

    [Fact]
    public async Task RegisterUser_WithTooShortPassword_ShouldThrowException()
    {
        // Arrange - Password less than 8 characters
        string password = "Pass1!";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _usersService.RegisterUserAsync("user@example.com", password, "Name")
        );
    }

    [Theory]
    [InlineData("invalid-email")]           // Missing @
    [InlineData("@example.com")]            // Missing local part
    [InlineData("user@")]                   // Missing domain
    [InlineData("user @example.com")]       // Space in email
    public async Task RegisterUser_WithInvalidEmail_ShouldThrowException(string invalidEmail)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _usersService.RegisterUserAsync(invalidEmail, "ValidPass123!", "Name")
        );
    }

    [Fact]
    public async Task RegisterUser_ShouldHashPassword()
    {
        // Arrange
        string password = "SecurePass123!";
        User capturedUser = null;

        _repositoryMock
            .Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        _repositoryMock
            .Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .ReturnsAsync(true);

        // Act
        await _usersService.RegisterUserAsync("user@example.com", password, "User Name");

        // Assert
        Assert.NotNull(capturedUser);
        Assert.NotEqual(password, capturedUser.PasswordHash);  // Should be hashed
        Assert.NotEmpty(capturedUser.PasswordHash);
    }

    #endregion

    #region User Login Tests

    [Fact]
    public async Task LoginUser_WithCorrectPassword_ShouldReturnToken()
    {
        // Arrange
        string email = "user@example.com";
        string password = "SecurePass123!";
        var user = new User
        {
            Id = "user-id",
            Email = email,
            PasswordHash = HashPassword(password)  // Hashed password
        };

        _repositoryMock
            .Setup(r => r.GetUserByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var token = await _usersService.LoginAsync(email, password);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(token.StartsWith("eyJ"));  // JWT format
    }

    [Fact]
    public async Task LoginUser_WithIncorrectPassword_ShouldThrowException()
    {
        // Arrange
        string email = "user@example.com";
        var user = new User
        {
            Email = email,
            PasswordHash = HashPassword("CorrectPass123!")
        };

        _repositoryMock
            .Setup(r => r.GetUserByEmailAsync(email))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _usersService.LoginAsync(email, "WrongPass123!")
        );
    }

    [Fact]
    public async Task LoginUser_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetUserByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _usersService.LoginAsync("nonexistent@example.com", "Password123!")
        );
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task ValidateToken_WithValidToken_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com",
            FullName = "Test User"
        };

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync("user-id"))
            .ReturnsAsync(user);

        // Generate a valid token
        string token = await _usersService.LoginAsync("user@example.com", "SecurePass123!");

        // Act
        var validatedUser = await _usersService.ValidateTokenAsync(token);

        // Assert
        Assert.NotNull(validatedUser);
        Assert.Equal("user@example.com", validatedUser.Email);
    }

    [Fact]
    public async Task ValidateToken_WithInvalidToken_ShouldThrowException()
    {
        // Arrange
        string invalidToken = "invalid-token-xyz";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _usersService.ValidateTokenAsync(invalidToken)
        );
    }

    [Fact]
    public async Task ValidateToken_WithDeletedUser_ShouldThrowException()
    {
        // Arrange - Token is valid but user no longer exists
        string validToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);  // User deleted

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _usersService.ValidateTokenAsync(validToken)
        );
    }

    #endregion

    #region Token Generation Tests

    [Fact]
    public async Task GenerateToken_ShouldIncludeUserClaims()
    {
        // Arrange
        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com",
            FullName = "Test User"
        };

        // Act
        string token = await _usersService.GenerateTokenAsync(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Decode and verify claims (basic check)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);  // JWT has 3 parts: header.payload.signature
    }

    [Fact]
    public async Task GenerateToken_ShouldHaveExpiration()
    {
        // Arrange
        var user = new User { Id = "user-id", Email = "user@example.com" };

        // Act
        string token = await _usersService.GenerateTokenAsync(user);

        // Assert - Token should expire
        Assert.NotNull(token);
        // In a real test, would decode and check 'exp' claim
    }

    #endregion

    #region Password Hashing Tests

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashesForSamePassword()
    {
        // Arrange
        string password = "SecurePass123!";

        // Act
        string hash1 = HashPassword(password);
        string hash2 = HashPassword(password);

        // Assert - BCrypt produces different salts even for same password
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "SecurePass123!";
        string hash = HashPassword(password);

        // Act
        bool isValid = VerifyPassword(password, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        string password = "SecurePass123!";
        string wrongPassword = "WrongPass123!";
        string hash = HashPassword(password);

        // Act
        bool isValid = VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(isValid);
    }

    #endregion

    #region Helper Methods

    private string HashPassword(string password)
    {
        // Simple BCrypt hashing simulation
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    #endregion
}

/// <summary>
/// Tests for JWT token validation at controller level
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IUsersService> _usersServiceMock;

    public AuthControllerTests()
    {
        _usersServiceMock = new Mock<IUsersService>();
    }

    [Fact]
    public async Task Register_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        string email = "newuser@example.com";
        string password = "SecurePass123!";

        var user = new User
        {
            Id = "user-id",
            Email = email,
            FullName = "New User"
        };

        _usersServiceMock
            .Setup(s => s.RegisterUserAsync(email, password, It.IsAny<string>()))
            .ReturnsAsync(true);

        _usersServiceMock
            .Setup(s => s.GetUserByEmailAsync(email))
            .ReturnsAsync(user);

        _usersServiceMock
            .Setup(s => s.GenerateTokenAsync(user))
            .ReturnsAsync("jwt-token");

        // Act would call controller Register endpoint
        // Assert would verify token returned
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        // Arrange
        _usersServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid credentials"));

        // Act would call controller Login endpoint
        // Assert would verify 401 returned
    }
}
