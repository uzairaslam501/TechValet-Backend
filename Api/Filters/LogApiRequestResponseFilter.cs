using ITValet.HelpingClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Collections.Concurrent;

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

            LoggingService.EnqueueLog(logEntry);
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


public class LoggingService
{
    private static readonly ConcurrentQueue<object> LogQueue = new ConcurrentQueue<object>();
    private static readonly string LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logger.json");
    private static readonly Timer LogTimer;

    static LoggingService()
    {
        // Initialize the timer to process logs every 30 seconds
        LogTimer = new Timer(ProcessLogs, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    public static void EnqueueLog(object logEntry)
    {
        if (logEntry != null)
        {
            LogQueue.Enqueue(logEntry);
        }
    }

    private static void ProcessLogs(object state)
    {
        if (!LogQueue.IsEmpty)
        {
            var logsToWrite = new List<string>();

            // Dequeue all log entries
            while (LogQueue.TryDequeue(out var logEntry))
            {
                logsToWrite.Add(JsonConvert.SerializeObject(logEntry, Formatting.Indented));
            }

            if (logsToWrite.Any())
            {
                // Write logs to the file
                lock (LogFilePath)
                {
                    if (!File.Exists(LogFilePath))
                    {
                        // Create a new JSON array if the file doesn't exist
                        File.WriteAllText(LogFilePath, "[" + string.Join(",", logsToWrite) + "]");
                    }
                    else
                    {
                        // Append to the existing JSON array
                        var content = File.ReadAllText(LogFilePath);
                        var lastIndex = content.LastIndexOf("]");
                        if (lastIndex > 0)
                        {
                            content = content.Remove(lastIndex, 1) + "," + string.Join(",", logsToWrite) + "]";
                            File.WriteAllText(LogFilePath, content);
                        }
                    }
                }
            }
        }
    }
}