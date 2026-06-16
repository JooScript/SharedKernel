using Microsoft.Extensions.Logging;
using Utils.FileActions;

namespace SharedKernel.Api.Exceptions;

/// <summary>
/// Represents a custom API layer exception that logs error details
/// both to the configured logger and to a file-based logging system.
/// </summary>
/// <remarks>
/// This exception wraps the original exception, enriches it with an optional
/// custom message, and automatically logs the error along with the calling method name.
/// </remarks>
public class ApiException : Exception
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record the exception details.</param>
    /// <param name="ex">The original exception that occurred.</param>
    /// <param name="customMessage">
    /// An optional custom message providing additional context for the exception.
    /// If not provided, a default message is used.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> or <paramref name="ex"/> is null.
    /// </exception>
    public ApiException(ILogger logger, Exception ex, string customMessage = "") : base(customMessage, ex)
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
}