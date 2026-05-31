namespace DdCleanEverydayApi.Models;

public record LoginRequest(string Username, string Password);

public record CreateChecklistRequest(string UserId, string Name, List<string>? Items);

public record CreateChecklistItemRequest(string Text);

public record CreateLocationRequest(string Location);
