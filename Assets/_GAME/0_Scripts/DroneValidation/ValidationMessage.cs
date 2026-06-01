public sealed class ValidationMessage
{
    public ValidationSeverity Severity;
    public string Message;

    public ValidationMessage(
        ValidationSeverity severity,
        string message)
    {
        Severity = severity;
        Message = message;
    }
}