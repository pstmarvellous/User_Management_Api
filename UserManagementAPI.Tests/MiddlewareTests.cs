using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserManagementAPI.Tests;

public class MiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string ValidToken = "techhive-secret-token";

    public MiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthenticationMiddleware_MissingAuthorizationHeader_Returns401WithExpectedJson()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var errorMessage = jsonDoc.RootElement.GetProperty("error").GetString();
        Assert.Equal("Unauthorized. Valid bearer token is required.", errorMessage);
    }

    [Fact]
    public async Task AuthenticationMiddleware_NonBearerAuthorizationHeader_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var errorMessage = jsonDoc.RootElement.GetProperty("error").GetString();
        Assert.Equal("Unauthorized. Valid bearer token is required.", errorMessage);
    }

    [Fact]
    public async Task AuthenticationMiddleware_InvalidBearerToken_Returns401WithInvalidTokenError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-secret-token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var errorMessage = jsonDoc.RootElement.GetProperty("error").GetString();
        Assert.Equal("Unauthorized. Invalid token.", errorMessage);
    }

    [Fact]
    public async Task AuthenticationMiddleware_ValidBearerToken_AllowsRequestThrough()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ValidToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationMiddleware_NonApiRoute_BypassesAuthentication()
    {
        // Act - request a route outside /api without authorization header
        var response = await _client.GetAsync("/");

        // Assert - should NOT return 401 Unauthorized
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_UnhandledException_Returns500WithStandardJson()
    {
        // Arrange - request endpoint that throws unhandled exception with valid token
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/debug/error");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ValidToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var errorMessage = jsonDoc.RootElement.GetProperty("error").GetString();
        Assert.Equal("Internal server error.", errorMessage);
    }
}
