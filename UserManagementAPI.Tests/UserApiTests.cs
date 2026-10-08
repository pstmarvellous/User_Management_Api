using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserManagementAPI.Tests;

public class UserApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string ValidToken = "techhive-secret-token";

    public UserApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValidToken);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithListOfUsers()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<User>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsOkAndUser()
    {
        // Act
        var response = await _client.GetAsync("/api/users/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.FirstName);
        Assert.Equal("Johnson", user.LastName);
    }

    [Fact]
    public async Task GetUserById_NonExistingUser_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/users/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ValidRequest_Returns201CreatedWithLocationHeaderAndUser()
    {
        // Arrange
        var newUserRequest = new CreateUserRequest(
            FirstName: "Test",
            LastName: "Engineer",
            Email: "test.engineer@techhive.com",
            Department: "Quality Assurance",
            IsActive: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", newUserRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);
        Assert.True(createdUser.Id > 0);
        Assert.Equal("Test", createdUser.FirstName);
        Assert.Equal("test.engineer@techhive.com", createdUser.Email);
        Assert.Equal($"/api/users/{createdUser.Id}", response.Headers.Location.ToString());

        // Verify user can be fetched by ID
        var getResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ExistingUser_ReturnsOkAndUpdatedUser()
    {
        // Arrange: Create a user specifically to update
        var createRequest = new CreateUserRequest(
            FirstName: "PreUpdate",
            LastName: "User",
            Email: "preupdate@techhive.com",
            Department: "Support",
            IsActive: true
        );
        var createResponse = await _client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);

        var updateRequest = new UpdateUserRequest(
            FirstName: "PostUpdate",
            LastName: "User",
            Email: "postupdate@techhive.com",
            Department: "Operations",
            IsActive: false
        );

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/users/{createdUser.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(updatedUser);
        Assert.Equal("PostUpdate", updatedUser.FirstName);
        Assert.Equal("postupdate@techhive.com", updatedUser.Email);
        Assert.Equal("Operations", updatedUser.Department);
        Assert.False(updatedUser.IsActive);
    }

    [Fact]
    public async Task UpdateUser_NonExistingUser_Returns404NotFound()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest(
            FirstName: "Ghost",
            LastName: "User",
            Email: "ghost@techhive.com",
            Department: "Security",
            IsActive: true
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/9999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ExistingUser_Returns204NoContentAndRemovesUser()
    {
        // Arrange: Create a user specifically to delete
        var createRequest = new CreateUserRequest(
            FirstName: "DeleteMe",
            LastName: "User",
            Email: "deleteme@techhive.com",
            Department: "Temporary",
            IsActive: false
        );
        var createResponse = await _client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify user is no longer found
        var getResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_NonExistingUser_Returns404NotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/users/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
