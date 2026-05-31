using Azure.Data.Tables;
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
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        if (request.Password != DemoPassword)
        {
            _logger.LogWarning("Login failed for user {Username}: invalid password", request.Username);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var entities = _profileTable.QueryAsync<UserProfileEntity>(
            e => e.Username == request.Username);

        UserProfileEntity? profile = null;
        await foreach (var entity in entities)
        {
            profile = entity;
            break;
        }

        if (profile == null)
        {
            _logger.LogWarning("Login failed: user {Username} not found", request.Username);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogInformation("Login successful for user: {Username}, id: {Id}", request.Username, profile.Id);
        return Ok(new
        {
            userId = profile.Id,
            fullname = profile.Fullname,
            username = profile.Username,
            location = profile.Location
        });
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        _logger.LogInformation("GetProfile called for userId: {UserId}", userId);

        var entities = _profileTable.QueryAsync<UserProfileEntity>(e => e.Id == userId);

        await foreach (var entity in entities)
        {
            _logger.LogInformation("Profile found for userId: {UserId}", userId);
            return Ok(new
            {
                id = entity.Id,
                fullname = entity.Fullname,
                username = entity.Username,
                location = entity.Location
            });
        }

        _logger.LogWarning("Profile not found for userId: {UserId}", userId);
        return NotFound(new { message = "Profile not found" });
    }
}
