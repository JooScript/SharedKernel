namespace SharedKernel.Bl.Exceptions;

/// <summary>
/// Custom exception class for data access layer exceptions with built-in logging.
/// </summary>
public class BusinessLogicException : Exception
{
    /// <summary>
    /// Initializes a new instance of the BusinessLogicException class.
    /// </summary>
    /// <param name="logger">The LogHelper instance for logging.</param>
    /// <param name="ex">The original exception that occurred.</param>
    /// <param name="customMessage">Custom message to include in the log.</param>
    /// <param name="isConcurrencyConflict">Flag indicating if this is a concurrency conflict</param>
    public BusinessLogicException(Exception ex, string customMessage = "") : base(customMessage, ex)
    {

    }

    public BusinessLogicException(string customMessage = "") : base(customMessage)
    {

    }

}