using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Lambda.Utilities;

namespace Lambda.Tests.Utilities;

public class JwtHelperTests
{
    private string CreateTestToken(params Claim[] claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new byte[32]; // Dummy key for testing
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public void ExtractUserId_WithSubClaim_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = "user-123";
        var token = CreateTestToken(new Claim("sub", expectedUserId));
        var authHeader = $"Bearer {token}";

        // Act
        var actualUserId = JwtHelper.ExtractUserId(authHeader);

        // Assert
        Assert.Equal(expectedUserId, actualUserId);
    }

    [Fact]
    public void ExtractUserId_WithCognitoUsernameClaim_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = "cognito-user-456";
        var token = CreateTestToken(new Claim("cognito:username", expectedUserId));
        var authHeader = $"Bearer {token}";

        // Act
        var actualUserId = JwtHelper.ExtractUserId(authHeader);

        // Assert
        Assert.Equal(expectedUserId, actualUserId);
    }

    [Fact]
    public void ExtractUserId_WithBothClaims_PrefersSubClaim()
    {
        // Arrange
        var expectedUserId = "sub-user-789";
        var token = CreateTestToken(
            new Claim("sub", expectedUserId),
            new Claim("cognito:username", "other-user")
        );
        var authHeader = $"Bearer {token}";

        // Act
        var actualUserId = JwtHelper.ExtractUserId(authHeader);

        // Assert
        Assert.Equal(expectedUserId, actualUserId);
    }

    [Fact]
    public void ExtractUserId_WithoutBearerPrefix_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = "user-no-bearer";
        var token = CreateTestToken(new Claim("sub", expectedUserId));

        // Act
        var actualUserId = JwtHelper.ExtractUserId(token);

        // Assert
        Assert.Equal(expectedUserId, actualUserId);
    }

    [Fact]
    public void ExtractUserId_WithNullHeader_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.ExtractUserId(null));
        Assert.Contains("Authorization header is missing", exception.Message);
    }

    [Fact]
    public void ExtractUserId_WithEmptyHeader_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.ExtractUserId(""));
        Assert.Contains("Authorization header is missing", exception.Message);
    }

    [Fact]
    public void ExtractUserId_WithMalformedToken_ThrowsArgumentException()
    {
        // Arrange
        var malformedToken = "Bearer not-a-valid-jwt-token";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.ExtractUserId(malformedToken));
        Assert.Contains("Invalid JWT token", exception.Message);
    }

    [Fact]
    public void ExtractUserId_WithMissingUserIdClaim_ThrowsArgumentException()
    {
        // Arrange
        var token = CreateTestToken(new Claim("email", "test@example.com"));
        var authHeader = $"Bearer {token}";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.ExtractUserId(authHeader));
        Assert.Contains("User ID claim not found in token", exception.Message);
    }

    [Fact]
    public void ExtractEmail_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var expectedEmail = "test@example.com";
        var token = CreateTestToken(new Claim("email", expectedEmail));
        var authHeader = $"Bearer {token}";

        // Act
        var actualEmail = JwtHelper.ExtractEmail(authHeader);

        // Assert
        Assert.Equal(expectedEmail, actualEmail);
    }

    [Fact]
    public void ExtractEmail_WithNullHeader_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.ExtractEmail(null));
        Assert.Contains("Authorization header is missing", exception.Message);
    }

    [Fact]
    public void ExtractEmail_WithMissingEmailClaim_ThrowsArgumentException()
    {
        // Arrange
        var token = CreateTestToken(new Claim("sub", "user-123"));
        var authHeader = $"Bearer {token}";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.ExtractEmail(authHeader));
        Assert.Contains("Email claim not found in token", exception.Message);
    }
}
