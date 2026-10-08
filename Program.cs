var builder = WebApplication.CreateBuilder(args);

const string ValidBearerToken = "techhive-secret-token";

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal server error."
        });
    });
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized. Valid bearer token is required."
            });
            return;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        if (!string.Equals(token, ValidBearerToken, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized. Invalid token."
            });
            return;
        }
    }

    await next();
});

app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    await next();

    var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
    Console.WriteLine($"[AUDIT] {context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode} in {elapsedMs:F0} ms");
});

var users = new List<User>
{
    new(1, "Alice", "Johnson", "alice.johnson@techhive.com", "Engineering", true),
    new(2, "Marcus", "Lee", "marcus.lee@techhive.com", "Human Resources", true),
    new(3, "Priya", "Patel", "priya.patel@techhive.com", "IT Support", false)
};

app.MapGet("/api/users", () => Results.Ok(users));

app.MapGet("/api/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPost("/api/users", (CreateUserRequest request) =>
{
    var user = new User
    (
        Id: users.Count == 0 ? 1 : users.Max(u => u.Id) + 1,
        FirstName: request.FirstName,
        LastName: request.LastName,
        Email: request.Email,
        Department: request.Department,
        IsActive: request.IsActive
    );

    users.Add(user);
    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapPut("/api/users/{id:int}", (int id, UpdateUserRequest request) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
    {
        return Results.NotFound();
    }

    var updatedUser = user with
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        Department = request.Department,
        IsActive = request.IsActive
    };

    var index = users.IndexOf(user);
    users[index] = updatedUser;

    return Results.Ok(updatedUser);
});

app.MapDelete("/api/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
    {
        return Results.NotFound();
    }

    users.Remove(user);
    return Results.NoContent();
});

app.MapGet("/api/debug/error", (HttpContext context) =>
{
    throw new InvalidOperationException("Simulated unhandled exception.");
});

app.Run();

public record User(int Id, string FirstName, string LastName, string Email, string Department, bool IsActive);

public record CreateUserRequest(string FirstName, string LastName, string Email, string Department, bool IsActive = true);

public record UpdateUserRequest(string FirstName, string LastName, string Email, string Department, bool IsActive = true);

public partial class Program { }

