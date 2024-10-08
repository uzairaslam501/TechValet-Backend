using ITValet.HelpingClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace ITValet.Filters
{
    public class ExceptionHandlerAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            Exception e = filterContext.Exception;
            filterContext.ExceptionHandled = true;

            int line = (new StackTrace(e, true)).GetFrame(0).GetFileLineNumber();
            var message = e.Message + ", at line # " + line;
            MailSender mailSender = new MailSender();
            //mailSender.SendErrorEmail(message);
            filterContext.Result = new BadRequestObjectResult(GeneralPurpose.GenerateResponseCode(false, "500", message));
        }
    }
}
