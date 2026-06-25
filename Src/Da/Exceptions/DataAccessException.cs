using System;
using Microsoft.Extensions.Logging;
using Utils.FileActions;

namespace SharedKernel.Da.Exceptions;

/// <summary>
/// Custom exception class for data access layer exceptions with built-in logging.
/// </summary>
public class DataAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DataAccessException class.
    /// </summary>
    /// <param name="logger">The LogHelper instance for logging.</param>
    /// <param name="ex">The original exception that occurred.</param>
    /// <param name="customMessage">Custom message to include in the log.</param>
    /// <param name="isConcurrencyConflict">Flag indicating if this is a concurrency conflict</param>
    public DataAccessException(Exception ex, string customMessage = "") : base(customMessage, ex)
    {
        if (ex == null)
            throw new ArgumentNullException(nameof(ex));

        var methodName = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "UnknownMethod";

        customMessage = string.IsNullOrWhiteSpace(customMessage) ? $"Data access layer exception occurred in {methodName}" : $"{customMessage} in {methodName}";

        Exception? customException = new Exception(customMessage, ex);

        FileHelper.ErrorLogger(customException, true);
    }


    public DataAccessException(string customMessage) : base(customMessage)
    {

    }
}