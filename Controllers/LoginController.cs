using Azure.Data.Tables;
using Datadog.Trace;
using DdCleanEverydayApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DdCleanEverydayApi.Controllers;

[ApiController]
[Route("api/v1")]
public class LoginController : ControllerBase
{
    private const string DemoPassword = "omed-4*";
    private readonly TableClient _profileTable;
    private readonly ILogger<LoginController> _logger;

    public LoginController(TableServiceClient tableService, ILogger<LoginController> logger)
    {
        _profileTable = tableService.GetTableClient("profiletable");
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        using var scope = Tracer.Instance.StartActive("user.login");
        scope.Span.ResourceName = request.Username;
        scope.Span.SetTag("usr.name", request.Username);

        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        if (request.Password != DemoPassword)
        {
            _logger.LogWarning("Login failed for user {Username}: invalid password", request.Username);
            scope.Span.Error = true;
            scope.Span.SetTag(Tags.ErrorMsg, "Invalid password");
            scope.Span.SetTag("login.success", "false");
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var entities = _profileTable.QueryAsync<UserProfileEntity>(
            TableClient.CreateQueryFilter($"username eq {request.Username}"));

        UserProfileEntity? profile = null;
        await foreach (var entity in entities)
        {
            profile = entity;
            break;
        }

        if (profile == null)
        {
            _logger.LogWarning("Login failed: user {Username} not found", request.Username);
            scope.Span.Error = true;
            scope.Span.SetTag(Tags.ErrorMsg, "User not found");
            scope.Span.SetTag("login.success", "false");
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogInformation("Login successful for user: {Username}, id: {Id}", request.Username, profile.id);
        scope.Span.SetTag("login.success", "true");
        scope.Span.SetUser(new UserDetails { Id = profile.id, Name = profile.username });

        return Ok(new
        {
            userId = profile.id,
            fullname = profile.fullname,
            username = profile.username,
            location = profile.location
        });
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        using var scope = Tracer.Instance.StartActive("user.get_profile");
        scope.Span.ResourceName = userId;
        scope.Span.SetUser(new UserDetails { Id = userId });

        _logger.LogInformation("GetProfile called for userId: {UserId}", userId);

        var entities = _profileTable.QueryAsync<UserProfileEntity>(
            TableClient.CreateQueryFilter($"id eq {userId}"));

        await foreach (var entity in entities)
        {
            _logger.LogInformation("Profile found for userId: {UserId}", userId);
            return Ok(new
            {
                id = entity.id,
                fullname = entity.fullname,
                username = entity.username,
                location = entity.location
            });
        }

        _logger.LogWarning("Profile not found for userId: {UserId}", userId);
        scope.Span.Error = true;
        scope.Span.SetTag(Tags.ErrorMsg, "Profile not found");
        return NotFound(new { message = "Profile not found" });
    }
}
