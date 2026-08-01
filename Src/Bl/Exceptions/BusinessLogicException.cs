using Microsoft.Extensions.Logging;
using Utils.FileActions;

namespace SharedKernel.Bl.Exceptions;

/// <summary>
/// Custom exception class for data access layer exceptions with built-in logging.
/// </summary>
public class BusinessLogicException : Exception
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the BusinessLogicException class.
    /// </summary>
    /// <param name="logger">The LogHelper instance for logging.</param>
    /// <param name="ex">The original exception that occurred.</param>
    /// <param name="customMessage">Custom message to include in the log.</param>
    /// <param name="isConcurrencyConflict">Flag indicating if this is a concurrency conflict</param>
    public BusinessLogicException(ILogger logger, Exception ex, string customMessage = "") : base(customMessage, ex)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (ex == null)
            throw new ArgumentNullException(nameof(ex));

        var methodName = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "UnknownMethod";

        customMessage = string.IsNullOrWhiteSpace(customMessage) ? "Api layer exception occurred." : customMessage;

        Exception? customException = new Exception(customMessage, ex);

        _logger.LogError(methodName, customException);
        FileHelper.ErrorLogger(customException, true);
    }

    public BusinessLogicException(ILogger logger, string customMessage = "") : base(customMessage)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var methodName = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "UnknownMethod";
        customMessage = string.IsNullOrWhiteSpace(customMessage) ? "Api layer exception occurred." : customMessage;
        _logger.LogError(methodName, customMessage);
    }

    public BusinessLogicException(Exception ex, string customMessage = "") : base(customMessage, ex)
    {

    }

    public BusinessLogicException(string customMessage = "") : base(customMessage)
    {

    }

}