namespace Fulfillment.Api.Middleware;

public class ErrorMessageOptions
{
    public const string SectionName = "ErrorMessages";

    public Dictionary<string, ErrorMessage> Messages { get; set; } = new();
    
    public ErrorMessage Get(string key) =>
        Messages.TryGetValue(key, out var message)
            ? message 
            : new ErrorMessage { Title = "An error occurred" };
}

public class ErrorMessage
{
    public string Title { get; set; } =  "An error occurred";
    public string? Detail { get; set; }
}