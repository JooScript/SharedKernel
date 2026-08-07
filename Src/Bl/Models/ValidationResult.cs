namespace SharedKernel.Bl.Models;

public class EntityValidationResult
{
    public EntityValidationResult(bool success, string msg)
    {
        Success = success;
        Message = msg;
    }

    public bool Success { get; set; } = false;
    public string Message { get; set; } = null!;
}
