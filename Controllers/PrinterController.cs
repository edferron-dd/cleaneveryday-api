using Azure.Data.Tables;
using Datadog.Trace;
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
        using var scope = Tracer.Instance.StartActive("printer.list");

        _logger.LogInformation("GetPrinters called");

        var printers = new List<object>();
        await foreach (var entity in _printerTable.QueryAsync<PrinterEntity>())
        {
            printers.Add(new { id = entity.id, name = entity.name, status = entity.status });
        }

        _logger.LogInformation("Returning {Count} printers", printers.Count);
        scope.Span.SetTag("printer.count", printers.Count.ToString());
        return Ok(printers);
    }

    [HttpPost("{id}")]
    public IActionResult AddPrinter(string id)
    {
        using var scope = Tracer.Instance.StartActive("printer.add");
        scope.Span.ResourceName = id;
        scope.Span.SetTag("printer.id", id);

        _logger.LogInformation("AddPrinter called with id: {Id}", id);

        // Intentional null reference exception — this endpoint is designed to throw
        string? nullValue = null;
        try
        {
            var length = nullValue!.Length;
            return Ok(length);
        }
        catch (Exception ex)
        {
            scope.Span.SetException(ex);
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> SelectPrinter(string id)
    {
        using var scope = Tracer.Instance.StartActive("printer.select");
        scope.Span.ResourceName = id;
        scope.Span.SetTag("printer.id", id);

        _logger.LogInformation("SelectPrinter called for id: {Id}", id);

        PrinterEntity? target = null;
        var allPrinters = new List<PrinterEntity>();

        await foreach (var entity in _printerTable.QueryAsync<PrinterEntity>())
        {
            allPrinters.Add(entity);
            if (entity.id == id) target = entity;
        }

        if (target == null)
        {
            _logger.LogWarning("SelectPrinter: printer not found, id: {Id}", id);
            scope.Span.Error = true;
            scope.Span.SetTag(Tags.ErrorMsg, "Printer not found");
            return NotFound(new { message = "Printer not found" });
        }

        foreach (var printer in allPrinters)
        {
            printer.status = printer.id == id ? "selected" : string.Empty;
            await _printerTable.UpsertEntityAsync(printer);
        }

        _logger.LogInformation("Printer {Id} set to selected, all others cleared", id);
        scope.Span.SetTag("printer.name", target.name);
        return Ok(new { id = target.id, name = target.name, status = "selected" });
    }
}
