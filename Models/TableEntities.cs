using Azure;
using Azure.Data.Tables;

namespace DdCleanEverydayApi.Models;

public class UserProfileEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "profile";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Id { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class LocationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "location";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PrinterEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "printer";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ChecklistEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "checklist";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class ChecklistItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "checklistitem";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Id { get; set; } = string.Empty;
    public string ChecklistId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Status { get; set; } = false;
}

public class UserLocationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "userlocation";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
