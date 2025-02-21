namespace ITValet.HelpingClasses
{
    public class ProjectVariables
    {
        public string? FromEmail { get; set; }
        public string? FromEmailPassword { get; set; }
        public string JwtSecret { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public string FrontEnd { get; set; } = null!;
        public string ReactUrl { get; set; } = null!;
    }

    public class ReturnUrls
    {
        public string? StripeAccountSuccessUrl { get; set; }
        public string? StripeAccountFailedUrl { get; set; }
        public string? AccountUrl { get; set; }
        public string? OrderDetailUrl { get; set; }
    }

    public class StripeApiKeys
    {
        public string? StripeApiKey { get; set; } //Secret Key
        public string? StripeClientId { get; set; } // Published Key
    }

    public class Zoom
    {
        public string AccountId { get; set; } = string.Empty!;
        public string ClientId { get; set; } = string.Empty!;
        public string ClientSecret { get; set; } = string.Empty!;
    }

    public class GoogleAuth
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }

    public static class GlobalMessages
    {
        public static string SuccessMessage = "The record has been added successfully.";
        public static string UpdateMessage = "The record has been updated successfully.";
        public static string DeletedMessage = "The record has been deleted successfully.";
        public static string EmailPassword = "Email/Password cannot be null or Empty.";
        public static string LoginNotFound = "Email/Password is incorrect";
        public static string RecordFound = "Record Found!";
        public static string RecordNotFound = "The record you're looking for may have been removed or relocated.";
        public static string DuplicateEmail = "This email is already in use. Please use a different email to register your account.";
        public static string DuplicateUsername = "This username is already taken. Please choose a different username.";
        public static string OldPassword = "The old password did not match.";
        public static string EmailNotFound = "The email address you supplied does not exist in our database. Please provide a valid email address.";
        public static string EmailSendFailed = "Mail sending failed, please try again.";
        public static string PasswordNotMatched = "The new password and the confirm password did not match.";
        public static string PasswordUpdated = "Your password has been updated. Please try login again";
        public static string InsufficientRemainingSession = "You have Insufficient remaining sessions";
        public static string MessageSentFail = "Failed to Send Message";
        public static string OrderDeliverFail = "Failed to Deliver Order";
        public static string InvalidEmail = "Email Not Found";
        public static string AccountRemoved = "Account has been removed successfully!";
        public static string SystemFailureMessage = "The operation could not be completed as the system is currently processing. Please try again later or contact the support team for assistance.";

        #region CustomerController
        public static string DuplicateServiceTitle = "You have previously submitted a request under this service title, please use a different name for your new request.";
        #endregion
    }
}
