namespace ITValet.HelpingClasses
{
    public class ProjectVariables
    {
        public static string FromEmail { get; set; } = null;
        public static string FromEmailPassword { get; set; } = null;
        public string JwtSecret { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public string SystemUrl { get; set; } = null!;

        public static string StripeAccountVerifySuccessUrl = "User/Account?message=Stripe Account Verification Success";
        public static string StripeAccountVerifyFailedUrl = "User/Account?messages=Account Verification Failed";
        public static string AccountUrl = "https://techvalet.ca/User/Account";
        public static string OrderDetailUrl = "https://techvalet.ca/User/OrderDetail?orderId=";
        public static string ForStripeUrl = "https://techvalet.ca/";
    }

    public static class GlobalMessages
    {
        public static string ZoomAccountId = "BWKIKMwhTTiCbxE_Hbuuhw";
        public static string ZoomClientId = "eFCpqz8STwOwm15dRqQVAg";
        public static string ZoomClientSecret = "r6k2lDM1yCzsWmGPNmWD51fyIOxhS3Se";
        public static string StripeApiKey = "sk_test_51LdJU1JGItIO6che6rYKSSzY2NEhOmMJtbUKUAxe1H95dl8oQPI6jWPmWHNBLfRsC8PdeqVi2TY1CFWjwsxWrlfp00D0eREv8W";
        public static string SuccessMessage = "The record has been added successfully.";
        public static string UpdateMessage = "The record has been updated successfully.";
        public static string DeletedMessage = "The record has been deleted successfully.";
        public static string EmailPassword = "Email/Password cannot be null or Empty.";
        public static string LoginNotFound = "Email/Password is incorrect";
        public static string RecordNotFound = "The record you're looking for may have been removed or relocated.";
        public static string DuplicateEmail = "This email is already in the system. Log in again or change the email address you can use to register your account.";
        public static string OldPassword = "The old password did not match.";
        public static string EmailNotFound = "The email address you supplied does not exist in our database. Please provide a valid email address.";
        public static string EmailSendFailed = "Mail sending failed, please try again.";
        public static string PasswordNotMatched = "The new password and the confirm password did not match.";
        public static string PasswordUpdated = "Your password has been updated. Please try login again";
        public static string SystemFailureMessage = "Oops! Something seems to have gone wrong. We apologize for the inconvenience. Please try again later.";
        public static string InsufficientRemainingSession = "You have Insufficient remaining sessions";
        public static string MessageSentFail = "Failed to Send Message";
        public static string OrderDeliverFail = "Failed to Deliver Order";

        #region CustomerController
        public static string DuplicateServiceTitle = "You have previously submitted a request under this service title, please use a different name for your new request.";
        #endregion
    }
}
