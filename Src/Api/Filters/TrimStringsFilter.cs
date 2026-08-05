using Microsoft.AspNetCore.Mvc.Filters;

namespace SharedKernel.Api.Filters;

public class TrimStringsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            TrimObject(argument);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static void TrimObject(object? obj)
    {
        if (obj is null) return;

        var properties = obj.GetType().GetProperties();

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            if (property.PropertyType == typeof(string))
            {
                var value = (string?)property.GetValue(obj);
                property.SetValue(obj, value?.Trim());
            }
        }
    }
}