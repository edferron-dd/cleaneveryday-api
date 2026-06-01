using Azure;
using Azure.Data.Tables;

namespace DdCleanEverydayApi.Models;

public class UserProfileEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "profile";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string id { get; set; } = string.Empty;
    public string fullname { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
}

public class LocationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "location";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string name { get; set; } = string.Empty;
}

public class PrinterEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "printer";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
}

public class ChecklistEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "checklist";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string userId { get; set; } = string.Empty;
}

public class ChecklistItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "checklistitem";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string id { get; set; } = string.Empty;
    public string checklistId { get; set; } = string.Empty;
    public string text { get; set; } = string.Empty;
    public bool status { get; set; } = false;
}

public class UserLocationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "userlocation";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string userId { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
}
