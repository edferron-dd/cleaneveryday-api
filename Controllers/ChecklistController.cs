using Azure.Data.Tables;
using Datadog.Trace;
using DdCleanEverydayApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DdCleanEverydayApi.Controllers;

[ApiController]
[Route("api/v1/checklist")]
public class ChecklistController : ControllerBase
{
    private readonly TableClient _checklistTable;
    private readonly TableClient _itemTable;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(TableServiceClient tableService, ILogger<ChecklistController> logger)
    {
        _checklistTable = tableService.GetTableClient("checklisttable");
        _itemTable = tableService.GetTableClient("checklistitemtable");
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetChecklist(string userId)
    {
        using var scope = Tracer.Instance.StartActive("checklist.get");
        scope.Span.ResourceName = userId;
        scope.Span.SetUser(new UserDetails { Id = userId });

        _logger.LogInformation("GetChecklist called for userId: {UserId}", userId);

        var checklists = new List<object>();
        await foreach (var cl in _checklistTable.QueryAsync<ChecklistEntity>(
            TableClient.CreateQueryFilter($"userId eq {userId}")))
        {
            var items = new List<object>();
            await foreach (var item in _itemTable.QueryAsync<ChecklistItemEntity>(
                TableClient.CreateQueryFilter($"checklistId eq {cl.id}")))
            {
                items.Add(new { id = item.id, checklistId = item.checklistId, text = item.text, status = item.status });
            }
            checklists.Add(new { id = cl.id, name = cl.name, userId = cl.userId, items });
        }

        _logger.LogInformation("Returning {Count} checklists for userId: {UserId}", checklists.Count, userId);
        scope.Span.SetTag("checklist.count", checklists.Count.ToString());
        return Ok(checklists);
    }

    [HttpPost("add")]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest request)
    {
        using var scope = Tracer.Instance.StartActive("checklist.create");
        scope.Span.SetUser(new UserDetails { Id = request.UserId });
        scope.Span.SetTag("checklist.name", request.Name);
        scope.Span.SetTag("checklist.item_count", (request.Items?.Count ?? 0).ToString());

        _logger.LogInformation("CreateChecklist called for userId: {UserId}, name: {Name}", request.UserId, request.Name);

        var checklistId = Guid.NewGuid().ToString();
        var entity = new ChecklistEntity
        {
            PartitionKey = "checklist",
            RowKey = checklistId,
            id = checklistId,
            name = request.Name,
            userId = request.UserId
        };

        await _checklistTable.UpsertEntityAsync(entity);
        _logger.LogInformation("Checklist created with id: {Id}", checklistId);

        if (request.Items != null)
        {
            foreach (var itemText in request.Items)
            {
                var itemId = Guid.NewGuid().ToString();
                var itemEntity = new ChecklistItemEntity
                {
                    PartitionKey = "checklistitem",
                    RowKey = itemId,
                    id = itemId,
                    checklistId = checklistId,
                    text = itemText,
                    status = false
                };
                await _itemTable.UpsertEntityAsync(itemEntity);
                _logger.LogInformation("Checklist item created with id: {Id}", itemId);
            }
        }

        scope.Span.SetTag("checklist.id", checklistId);
        return CreatedAtAction(nameof(GetChecklist), new { userId = request.UserId }, new { id = checklistId });
    }

    [HttpPost("{checklistId}/item")]
    public async Task<IActionResult> AddItem(string checklistId, [FromBody] CreateChecklistItemRequest request)
    {
        using var scope = Tracer.Instance.StartActive("checklist.add_item");
        scope.Span.ResourceName = checklistId;
        scope.Span.SetTag("checklist.id", checklistId);

        _logger.LogInformation("AddItem called for checklistId: {ChecklistId}, text: {Text}", checklistId, request.Text);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            _logger.LogWarning("AddItem failed: text is required");
            scope.Span.Error = true;
            scope.Span.SetTag(Tags.ErrorMsg, "text is required");
            return BadRequest(new { message = "text is required" });
        }

        var itemId = Guid.NewGuid().ToString();
        var entity = new ChecklistItemEntity
        {
            PartitionKey = "checklistitem",
            RowKey = itemId,
            id = itemId,
            checklistId = checklistId,
            text = request.Text,
            status = false
        };

        await _itemTable.UpsertEntityAsync(entity);
        _logger.LogInformation("Item created with id: {Id} for checklist: {ChecklistId}", itemId, checklistId);

        scope.Span.SetTag("item.id", itemId);
        return CreatedAtAction(nameof(GetChecklist), new { userId = checklistId }, new { id = itemId, checklistId, text = request.Text, status = false });
    }

    [HttpPut("{checklistId}/item/{id}")]
    public async Task<IActionResult> ToggleItem(string checklistId, string id)
    {
        using var scope = Tracer.Instance.StartActive("checklist.toggle_item");
        scope.Span.SetTag("checklist.id", checklistId);
        scope.Span.SetTag("item.id", id);

        _logger.LogInformation("ToggleItem called for checklistId: {ChecklistId}, itemId: {ItemId}", checklistId, id);

        ChecklistItemEntity? found = null;
        await foreach (var item in _itemTable.QueryAsync<ChecklistItemEntity>(
            TableClient.CreateQueryFilter($"id eq {id} and checklistId eq {checklistId}")))
        {
            found = item;
            break;
        }

        if (found == null)
        {
            _logger.LogWarning("ToggleItem: item not found, id: {Id}", id);
            scope.Span.Error = true;
            scope.Span.SetTag(Tags.ErrorMsg, "Item not found");
            return NotFound(new { message = "Item not found" });
        }

        found.status = !found.status;
        await _itemTable.UpsertEntityAsync(found);
        _logger.LogInformation("Item {Id} toggled to status: {Status}", id, found.status);

        scope.Span.SetTag("item.status", found.status.ToString());
        return Ok(new { id = found.id, checklistId = found.checklistId, text = found.text, status = found.status });
    }
}
