using Microsoft.Extensions.Logging;
using Utils.FileActions;

namespace SharedKernel.Bl.Exceptions;

public class BusinessLogicException : Exception
{
    public BusinessLogicException(ILogger logger, Exception ex, string customMessage = "") : base(customMessage, ex)
    {
        if (ex == null)
            throw new ArgumentNullException(nameof(ex));

        var methodName = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "UnknownMethod";

        customMessage = string.IsNullOrWhiteSpace(customMessage) ? "Api layer exception occurred." : customMessage;

        Exception? customException = new Exception(customMessage, ex);

        logger.LogError(methodName, customException);
        FileHelper.ErrorLogger(customException, true);
    }

    public BusinessLogicException(ILogger logger, string customMessage = "") : base(customMessage)
    {
        var methodName = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "UnknownMethod";
        customMessage = string.IsNullOrWhiteSpace(customMessage) ? "Api layer exception occurred." : customMessage;
        logger.LogError(methodName, customMessage);
    }

    public BusinessLogicException(Exception ex, string customMessage = "") : base(customMessage, ex)
    {

    }

    public BusinessLogicException(string customMessage = "") : base(customMessage)
    {

    }

}