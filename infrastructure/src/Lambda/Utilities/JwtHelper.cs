using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;

namespace Lambda.Utilities;

public static class JwtHelper
{
    /// <summary>
    /// Extracts the user ID from API Gateway request context (when using Cognito authorizer).
    /// Falls back to parsing Authorization header if claims are not in context.
    /// </summary>
    /// <param name="request">The API Gateway proxy request</param>
    /// <returns>The user ID extracted from the request</returns>
    /// <exception cref="ArgumentException">Thrown when user ID cannot be extracted</exception>
    public static string ExtractUserId(APIGatewayProxyRequest request)
    {
        // First, try to get userId from request context (Cognito authorizer puts claims here)
        if (request.RequestContext?.Authorizer?.Claims != null)
        {
            var claims = request.RequestContext.Authorizer.Claims;
            
            // Try 'sub' claim first (standard JWT claim for user ID)
            if (claims.TryGetValue("sub", out var sub) && !string.IsNullOrWhiteSpace(sub))
            {
                return sub;
            }
            
            // Try 'cognito:username' as fallback
            if (claims.TryGetValue("cognito:username", out var username) && !string.IsNullOrWhiteSpace(username))
            {
                return username;
            }
        }
        
        // Fallback: try to extract from Authorization header (for direct Lambda invocation or testing)
        var authHeader = request.Headers?.GetValueOrDefault("Authorization") 
            ?? request.Headers?.GetValueOrDefault("authorization");
            
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            return ExtractUserIdFromToken(authHeader);
        }
        
        throw new ArgumentException("User ID not found in request context or authorization header");
    }

    /// <summary>
    /// Extracts the user ID from JWT token claims.
    /// Looks for 'sub' or 'cognito:username' claims.
    /// </summary>
    /// <param name="authorizationHeader">The Authorization header value (e.g., "Bearer token")</param>
    /// <returns>The user ID extracted from the token</returns>
    /// <exception cref="ArgumentException">Thrown when token is invalid or user ID cannot be extracted</exception>
    public static string ExtractUserIdFromToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new ArgumentException("Authorization header is missing");
        }

        // Remove "Bearer " prefix if present
        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader.Substring(7)
            : authorizationHeader;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Validate token can be read
            if (!handler.CanReadToken(token))
            {
                throw new ArgumentException("Invalid JWT token format");
            }
            
            var jwtToken = handler.ReadJwtToken(token);

            // Try to get 'sub' claim first (standard JWT claim)
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
            if (subClaim != null && !string.IsNullOrWhiteSpace(subClaim.Value))
            {
                return subClaim.Value;
            }

            // Try 'cognito:username' as fallback
            var cognitoUsernameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "cognito:username");
            if (cognitoUsernameClaim != null && !string.IsNullOrWhiteSpace(cognitoUsernameClaim.Value))
            {
                return cognitoUsernameClaim.Value;
            }

            throw new ArgumentException("User ID claim not found in token");
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Invalid JWT token: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts the email from API Gateway request context or JWT token.
    /// </summary>
    /// <param name="request">The API Gateway proxy request</param>
    /// <returns>The email extracted from the request</returns>
    /// <exception cref="ArgumentException">Thrown when email cannot be extracted</exception>
    public static string ExtractEmail(APIGatewayProxyRequest request)
    {
        // First, try to get email from request context (Cognito authorizer puts claims here)
        if (request.RequestContext?.Authorizer?.Claims != null)
        {
            var claims = request.RequestContext.Authorizer.Claims;
            
            if (claims.TryGetValue("email", out var email) && !string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
        }
        
        // Fallback: try to extract from Authorization header
        var authHeader = request.Headers?.GetValueOrDefault("Authorization") 
            ?? request.Headers?.GetValueOrDefault("authorization");
            
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            return ExtractEmailFromToken(authHeader);
        }
        
        throw new ArgumentException("Email not found in request context or authorization header");
    }

    /// <summary>
    /// Extracts the email from JWT token claims.
    /// </summary>
    /// <param name="authorizationHeader">The Authorization header value (e.g., "Bearer token")</param>
    /// <returns>The email extracted from the token</returns>
    /// <exception cref="ArgumentException">Thrown when token is invalid or email cannot be extracted</exception>
    public static string ExtractEmailFromToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new ArgumentException("Authorization header is missing");
        }

        // Remove "Bearer " prefix if present
        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader.Substring(7)
            : authorizationHeader;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Validate token can be read
            if (!handler.CanReadToken(token))
            {
                throw new ArgumentException("Invalid JWT token format");
            }
            
            var jwtToken = handler.ReadJwtToken(token);

            // Try to get 'email' claim
            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
            if (emailClaim != null && !string.IsNullOrWhiteSpace(emailClaim.Value))
            {
                return emailClaim.Value;
            }

            throw new ArgumentException("Email claim not found in token");
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Invalid JWT token: {ex.Message}", ex);
        }
    }
}
