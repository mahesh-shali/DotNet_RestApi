public class ApiErrorResponse
{
    public int Status { get; set; }          // HTTP Status Code
    public string Message { get; set; }      // Human-readable error message
    public string Detail { get; set; }       // Additional detailed information (optional)
    public string Instance { get; set; }     // A unique identifier for the error instance (useful for tracing)
    public List<string> Errors { get; set; } // List of specific errors (optional, for validation errors)

    // Constructor
    public ApiErrorResponse(int status, string message, string detail, string instance, List<string> errors = null)
    {
        Status = status;
        Message = message;
        Detail = detail;
        Instance = instance;
        Errors = errors ?? new List<string>(); // If no errors are provided, initialize with an empty list
    }
}
