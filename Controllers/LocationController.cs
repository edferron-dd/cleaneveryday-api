using Azure.Data.Tables;
using Datadog.Trace;
using DdCleanEverydayApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DdCleanEverydayApi.Controllers;

[ApiController]
[Route("api/v1/locations")]
public class LocationController : ControllerBase
{
    private readonly TableClient _locationTable;
    private readonly TableClient _userLocationTable;
    private readonly ILogger<LocationController> _logger;

    public LocationController(TableServiceClient tableService, ILogger<LocationController> logger)
    {
        _locationTable = tableService.GetTableClient("locationstable");
        _userLocationTable = tableService.GetTableClient("userlocationstable");
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        using var scope = Tracer.Instance.StartActive("location.list");

        _logger.LogInformation("GetLocations called");

        var locations = new List<object>();
        await foreach (var entity in _locationTable.QueryAsync<LocationEntity>())
        {
            locations.Add(new { id = entity.RowKey, name = entity.Name });
        }

        _logger.LogInformation("Returning {Count} locations", locations.Count);
        scope.Span.SetTag("location.count", locations.Count.ToString());
        return Ok(locations);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserLocation(string userId)
    {
        using var scope = Tracer.Instance.StartActive("location.get_user");
        scope.Span.ResourceName = userId;
        scope.Span.SetUser(new UserDetails { Id = userId });

        _logger.LogInformation("GetUserLocation called for userId: {UserId}", userId);

        await foreach (var entity in _userLocationTable.QueryAsync<UserLocationEntity>(
            TableClient.CreateQueryFilter($"userId eq {userId}")))
        {
            _logger.LogInformation("User location found: {Location} for userId: {UserId}", entity.location, userId);
            scope.Span.SetTag("location.name", entity.location);
            return Ok(new { userId = entity.userId, location = entity.location });
        }

        _logger.LogInformation("No location found for userId: {UserId}", userId);
        return Ok(new { userId, location = (string?)null });
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> SaveUserLocation(string userId, [FromBody] CreateLocationRequest request)
    {
        using var scope = Tracer.Instance.StartActive("location.save_user");
        scope.Span.ResourceName = userId;
        scope.Span.SetUser(new UserDetails { Id = userId });
        scope.Span.SetTag("location.name", request.Location);

        _logger.LogInformation("SaveUserLocation called for userId: {UserId}, location: {Location}", userId, request.Location);

        var entity = new UserLocationEntity
        {
            PartitionKey = "userlocation",
            RowKey = userId,
            userId = userId,
            location = request.Location
        };

        await _userLocationTable.UpsertEntityAsync(entity);
        _logger.LogInformation("User location saved for userId: {UserId}", userId);
        return Ok(new { userId, location = request.Location });
    }
}
