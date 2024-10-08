using ITValet.HelpingClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

public class LogApiRequestResponseFilter : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var startTime = DateTime.UtcNow; // Capture the start time

        var resultContext = await next();

        var endTime = DateTime.UtcNow;
        var actionName = context.ActionDescriptor.DisplayName;
        var response = resultContext.Result as ObjectResult;

        if (response != null)
        {
            var responseBody = response.Value as ResponseDto;
            string status = "Error";
            string statusCode = "500"; // Default to 500 if not found

            if (responseBody != null)
            {
                status = responseBody.Status.ToString();
                statusCode = responseBody.StatusCode;
            }

            var logEntry = new
            {
                ActionName = actionName,
                StartTime = startTime, // Use the captured start time
                EndTime = endTime,
                Status = status,
                StatusCode = statusCode
            };

            LogToJsonFile(logEntry);
        }
    }

    private void LogToJsonFile(object logEntry)
    {
        if (logEntry != null)
        {
            var logEntryJson = JsonConvert.SerializeObject(logEntry, Formatting.Indented);
            var logFileName = Path.Combine(Directory.GetCurrentDirectory(), "logger.json");

            // Check if the log file exists
            if (!File.Exists(logFileName))
            {
                // If the log file doesn't exist, create a new JSON array with the log entry
                logEntryJson = "[" + logEntryJson + "]";
            }
            else
            {
                // If the log file exists, remove the last ']' character, append the log entry, and add the ']' character
                var content = File.ReadAllText(logFileName);
                var lastIndex = content.LastIndexOf("]");
                content = content.Remove(lastIndex, 1) + "," + logEntryJson + "]";
                logEntryJson = content;
            }

            // Write the updated log content to the file
            File.WriteAllText(logFileName, logEntryJson);
        }
    }
}
