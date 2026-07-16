namespace SharedKernel.Bl.Exceptions;

public class MappingException : BusinessLogicException
{
    /// <summary>
    /// Initializes a new instance of the BusinessLogicException class.
    /// </summary>
    /// <param name="logger">The LogHelper instance for logging.</param>
    /// <param name="ex">The original exception that occurred.</param>
    /// <param name="customMessage">Custom message to include in the log.</param>
    /// <param name="isConcurrencyConflict">Flag indicating if this is a concurrency conflict</param>
    public MappingException(Exception ex, string customMessage = "") : base(ex, customMessage)
    {

    }

    public MappingException(string customMessage = "") : base(customMessage)
    {

    }

}