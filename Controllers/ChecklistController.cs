using Azure.Data.Tables;
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
        _logger.LogInformation("GetChecklist called for userId: {UserId}", userId);

        var checklists = new List<object>();
        await foreach (var cl in _checklistTable.QueryAsync<ChecklistEntity>(e => e.UserId == userId))
        {
            var items = new List<object>();
            await foreach (var item in _itemTable.QueryAsync<ChecklistItemEntity>(e => e.ChecklistId == cl.Id))
            {
                items.Add(new { id = item.Id, checklistId = item.ChecklistId, text = item.Text, status = item.Status });
            }
            checklists.Add(new { id = cl.Id, name = cl.Name, userId = cl.UserId, items });
        }

        _logger.LogInformation("Returning {Count} checklists for userId: {UserId}", checklists.Count, userId);
        return Ok(checklists);
    }

    [HttpPost("add")]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest request)
    {
        _logger.LogInformation("CreateChecklist called for userId: {UserId}, name: {Name}", request.UserId, request.Name);

        var checklistId = Guid.NewGuid().ToString();
        var entity = new ChecklistEntity
        {
            PartitionKey = "checklist",
            RowKey = checklistId,
            Id = checklistId,
            Name = request.Name,
            UserId = request.UserId
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
                    Id = itemId,
                    ChecklistId = checklistId,
                    Text = itemText,
                    Status = false
                };
                await _itemTable.UpsertEntityAsync(itemEntity);
                _logger.LogInformation("Checklist item created with id: {Id}", itemId);
            }
        }

        return CreatedAtAction(nameof(GetChecklist), new { userId = request.UserId }, new { id = checklistId });
    }

    [HttpPost("{checklistId}/item")]
    public async Task<IActionResult> AddItem(string checklistId, [FromBody] CreateChecklistItemRequest request)
    {
        _logger.LogInformation("AddItem called for checklistId: {ChecklistId}, text: {Text}", checklistId, request.Text);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            _logger.LogWarning("AddItem failed: text is required");
            return BadRequest(new { message = "text is required" });
        }

        var itemId = Guid.NewGuid().ToString();
        var entity = new ChecklistItemEntity
        {
            PartitionKey = "checklistitem",
            RowKey = itemId,
            Id = itemId,
            ChecklistId = checklistId,
            Text = request.Text,
            Status = false
        };

        await _itemTable.UpsertEntityAsync(entity);
        _logger.LogInformation("Item created with id: {Id} for checklist: {ChecklistId}", itemId, checklistId);
        return CreatedAtAction(nameof(GetChecklist), new { userId = checklistId }, new { id = itemId, checklistId, text = request.Text, status = false });
    }

    [HttpPut("{checklistId}/item/{id}")]
    public async Task<IActionResult> ToggleItem(string checklistId, string id)
    {
        _logger.LogInformation("ToggleItem called for checklistId: {ChecklistId}, itemId: {ItemId}", checklistId, id);

        ChecklistItemEntity? found = null;
        await foreach (var item in _itemTable.QueryAsync<ChecklistItemEntity>(e => e.Id == id && e.ChecklistId == checklistId))
        {
            found = item;
            break;
        }

        if (found == null)
        {
            _logger.LogWarning("ToggleItem: item not found, id: {Id}", id);
            return NotFound(new { message = "Item not found" });
        }

        found.Status = !found.Status;
        await _itemTable.UpsertEntityAsync(found);
        _logger.LogInformation("Item {Id} toggled to status: {Status}", id, found.Status);
        return Ok(new { id = found.Id, checklistId = found.ChecklistId, text = found.Text, status = found.Status });
    }
}
