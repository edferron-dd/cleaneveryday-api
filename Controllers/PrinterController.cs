using Azure.Data.Tables;
using DdCleanEverydayApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DdCleanEverydayApi.Controllers;

[ApiController]
[Route("api/v1/printer")]
public class PrinterController : ControllerBase
{
    private readonly TableClient _printerTable;
    private readonly ILogger<PrinterController> _logger;

    public PrinterController(TableServiceClient tableService, ILogger<PrinterController> logger)
    {
        _printerTable = tableService.GetTableClient("printerstable");
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetPrinters()
    {
        _logger.LogInformation("GetPrinters called");

        var printers = new List<object>();
        await foreach (var entity in _printerTable.QueryAsync<PrinterEntity>())
        {
            printers.Add(new { id = entity.Id, name = entity.Name, status = entity.Status });
        }

        _logger.LogInformation("Returning {Count} printers", printers.Count);
        return Ok(printers);
    }

    [HttpPost("{id}")]
    public IActionResult AddPrinter(string id)
    {
        _logger.LogInformation("AddPrinter called with id: {Id}", id);

        // Intentional null reference exception — this endpoint is designed to throw
        string? nullValue = null;
        var length = nullValue!.Length;

        return Ok(length);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> SelectPrinter(string id)
    {
        _logger.LogInformation("SelectPrinter called for id: {Id}", id);

        PrinterEntity? target = null;
        var allPrinters = new List<PrinterEntity>();

        await foreach (var entity in _printerTable.QueryAsync<PrinterEntity>())
        {
            allPrinters.Add(entity);
            if (entity.Id == id) target = entity;
        }

        if (target == null)
        {
            _logger.LogWarning("SelectPrinter: printer not found, id: {Id}", id);
            return NotFound(new { message = "Printer not found" });
        }

        foreach (var printer in allPrinters)
        {
            printer.Status = printer.Id == id ? "selected" : string.Empty;
            await _printerTable.UpsertEntityAsync(printer);
        }

        _logger.LogInformation("Printer {Id} set to selected, all others cleared", id);
        return Ok(new { id = target.Id, name = target.Name, status = "selected" });
    }
}
