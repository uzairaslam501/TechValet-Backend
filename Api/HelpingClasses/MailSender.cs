using MimeKit;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Stripe;
using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Policy;

namespace ITValet.HelpingClasses
{
    public class MailSender
    {
        public async Task<bool> SendForgotEmail(string email, string baseUrl)
        {
            try
            {
                string Url = "";
                string Subject = "";
                try
                {
                    string Username = "";
                    Subject = "IT-Valet : Forgot Password";
                    string description = "You are getting this email because you have requested to reset " +
                        "your account password.<br/> Click the button below to change your " +
                        "password. <br/>" +
                        "If you did not request a password reset, Please ignore this email";
                    
                    Url = baseUrl + "&t=" + GeneralPurpose.DateTimeNow().AddHours(23).Ticks; ;
                    string ButtonText = "Reset Password";
                    string bod = PopulateBody(Subject, Username, description,
                        Url, ButtonText);
                    string MailBody = bod;

                    return SendEmail(email, Subject, MailBody);
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendErrorMessage(string msg = "")
        {
            try
            {
                try
                {
                    
                    string Subject = "IT-Valet : Exception Email";
                    string email = "muhammad.hassan93b@gmail.com";

                    return SendEmail(email, Subject, msg);
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendEmailForSetTimeAvailability(string email, string username)
        {
            try
            {
                string subject = "IT-Valet : Set Your New Time Availability";
                string description = $"Hello {username},<br/><br/>Your previous week's time availability has expired. Please set your new availability by clicking the link below:<br/><br/>";

                string url = ProjectVariables.AccountUrl; 
                string buttonText = "Set Availability";

                string mailBody = PopulateBody(subject, username, description, url, buttonText);

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendEmailForPaymentMaintenance(string email, string username)
        {
            try
            {
                string subject = "Payment Status Update: Maintenance in Progress";
                string description = $"Hello {username},<br/><br/>We would like to inform you that your payment is currently undergoing maintenance or is in process. We apologize for any inconvenience this may cause. Rest assured, we are working diligently to resolve this as soon as possible.<br/><br/>";

                string url = ProjectVariables.OrderDetailUrl; // You can replace this with the appropriate URL for your help center or support.
                string buttonText = "Contact Support";

                string mailBody = PopulateBody(subject, username, description, url, buttonText);

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendEmailForITValetAdminVerfication(string email, string username, int role)
        {
            try
            {

                string Role = "";
                if (role == 3)
                {
                    Role = "Customer";
                }
                else
                {
                    Role = "IT-Valet";
                }
                string subject = "IT-Valet : Sign Up Verification";
                string description = "We have successfully forwarded your request to our administrator for review. <br> Once your " + Role + " account has been approved, you will receive a verification email to finalize the process.";

                string url = "";
                string buttonText = "";
                string mailBody = PopulateBody(subject, username, description, url, buttonText);

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendEmailForITValetAdminVerified(string email, string username, int role)
        {
            try
            {

                string Role = "";
                if (role == 3)
                {
                    Role = "Customer";
                }
                else
                {
                    Role = "IT-Valet";
                }
                string subject = "IT-Valet : " + Role + " Account Verified";
                string description = "The admin has confirmed the verification of your account. <br> You can now effectively utilize your account as a " + Role;

                string url = "";
                string buttonText = "";
                string mailBody = PopulateBody(subject, username, description, url, buttonText);

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendAlertForCompleteTheirProfile(string email, string username)
        {
            try
            {
                string subject = "IT-Valet: Complete Your Profile to Access More Opportunities";
                string description = $"Hello {username},<br/><br/>We trust you're enjoying your time with IT-Valet!<br/><br/>To unlock a world of opportunities and maximize your experience on our platform, we kindly urge you to complete your profile. A fully filled-out profile not only boosts your visibility to potential clients but also enables us to connect you with projects that align with your skills and expertise. Please ensure your PayPal and Stripe accounts are linked to your profile, as this will increase your chances of receiving more orders. If your profile remains incomplete, it won't be visible to our valued customers. Therefore, we strongly recommend linking both accounts.<br/><br/>";
                string url = "";
                string buttonText = "";
                string mailBody = PopulateBody(subject, username, description, url, buttonText); 

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SendWarningForBadReviewsAnalysis(string email, string username)
        {
            try
            {
                string subject = "Warning: Profile Review Analysis";
                string description = $"Hello {username},<br/><br/>We hope you're enjoying your experience on IT-Valet!<br/><br/>We recently conducted an analysis of your profile and noticed that you have received some negative reviews. Maintaining a positive user experience is vital for both you and our platform.<br/><br/>This is a friendly reminder to take proactive steps to improve your performance and address any issues that may have led to these bad reviews. Your account status is important to us, and we want to see you succeed.<br/><br/>Continuing to receive bad reviews may result in actions taken on your account, such as temporary suspension or permanent blocking. We encourage you to strive for excellence in your transactions.<br/><br/>Thank you for your cooperation and understanding.";
                string url = "";
                string buttonText = "";
                string mailBody = PopulateBody(subject, username, description, url, buttonText);

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }
        public static async Task<bool> SendAccountBlockedNotification(string email, string username)
        {
            try
            {
                string subject = "Account Blocked: Action Required";
                string description = $"Hello {username},<br/><br/>We regret to inform you that your IT-Valet account has been temporarily blocked due to a high number of negative reviews on your recent transactions.<br/><br/>To resolve this matter and seek assistance, please contact our admin authority at [Admin Contact Email] or [Admin Contact Phone Number]. We are here to help you understand the situation and work towards a resolution.<br/><br/>Your account status is important to us, and we hope to see you back on the platform soon with improved performance and user experience.<br/><br/>Thank you for your understanding.";
                string url = "";
                string buttonText = "";
                string mailBody = PopulateBody(subject, username, description, url, buttonText);

                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> EmailAccountVerification(string userId, string UserName, string email, int role, string ProjectVariable)
        {
            try
            {
                string Role = "";
                if (role == 3)
                {
                    Role = "Customer";
                }
                else
                {
                    Role = "IT-Valet";
                }
                string SubjectBody = "";
                string description = "";
                long t = DateTime.Now.AddDays(1).Ticks;
                string subject = "IT-Valet : " + Role + " Account Verification";
                description = "You are getting this email because you have requested to create your account as " + Role + ".<br/>Click the button below to verify your account.</br>If you did not request a " + Role + " account, Please ignore this email<br/><p style='margin: 0px;'><a style='padding: 10px 30px 30px 30px; line-height: 25px; font-size: 18px; font-weight: 400; color: #1B75BB;'>Note: The Link Will Expire After 24 Hours</a><br/></p>";
                string Url = ProjectVariable + "Auth/ConfirmAccount?Id=" + userId + "&t=" + t;
                string ButtonText = "Confirm Account";
                string mailBody = PopulateBody(SubjectBody, UserName, description, Url, ButtonText);
                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> EmailForgetPassword(string userId, string UserName, string email, string ProjectVariable)
        {
            try
            {
                string SubjectBody = "";
                string description = "";
                long t = DateTime.Now.AddDays(1).Ticks;
                string subject = "IT-Valet : Account Password Recovery";
                description = "You are getting this email because you have requested to recover your account Password. Click the button below to verify Recover your password.</br>If you did not request a Account Password Recovery, Please ignore this email<br/><p style='margin: 0px;'><a style='padding: 10px 30px 30px 30px; line-height: 25px; font-size: 18px; font-weight: 400; color: #1B75BB;'>Note: The Link Will Expire After 24 Hours</a><br/></p>";
                string Url = ProjectVariable + "Auth/RenewPassword?Id=" + userId + "&t=" + t;
                string ButtonText = "Recover Password";
                string mailBody = PopulateBody(SubjectBody, UserName, description, Url, ButtonText);
                return SendEmail(email, subject, mailBody);
            }
            catch
            {
                return false;
            }
        }

        private static string PopulateBody(string subject, string name, string description,
            string url, string buttonText)
        {
            if (name != "")
            {
                name = "<strong>" + name + "</strong><br/>";
            }
            string urlSection = "";
            if (url != "")
            {
                urlSection = "<table id='u_content_button_1' style='font-family:'Raleway',sans-serif;' role='presentation' cellpadding='0' cellspacing='0' width='100%' border='0'>" +
                    "<tbody>" +
                     "<tr>" +
                        "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:10px;font-family:'Raleway',sans-serif;' align='left'>" +
                            "<div class='v-text-align' align='center'>" +
                                "<a href='" + url + "' target='_blank' class='v-button v-size-width v-font-size' style='box-sizing: border-box;display: inline-block;text-decoration: none;-webkit-text-size-adjust: none;text-align: center; color:#FFFFFF; background-color: #316b94; border-radius: 4px;-webkit-border-radius: 4px; -moz-border-radius: 4px; width:auto; max-width:100%; overflow-wrap: break-word; word-break: break-word; word-wrap:break-word; mso-border-alt: none;font-size: 14px;'>" +
                                  "<span style='display:block;padding:10px 20px;line-height:120%;'><span style='line-height: 16.8px;'>" + buttonText + "</span></span>" +
                                "</a>" +
                            "</div>" +
                        "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:10px;font-family:'Raleway',sans-serif;' align='left'>" +
                            "<div class='v-text-align' align='center'>" +
                                "<span style='overflow-wrap: break-word; color:red; display:block;padding:10px 20px;line-height:120%;'><span style='line-height: 16.8px;'>" +
                                 "Link will not work in spam. Please move this mail into your inbox.<br />" +
                                 "If <strong>Button</strong> is not Working. Copy and Paste the Link below in your browser." +
                                "</span></span>" +
                            "</div>" +
                        "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:10px;font-family:'Raleway',sans-serif;' align='left'>" +
                            "<div class='v-text-align' align='center'>" +
                                "<a href='" + url + "' target='_blank' style='text-decoration: none;-webkit-text-size-adjust: none;text-align: center; overflow-wrap: break-word; word-break: break-word; word-wrap:break-word; mso-border-alt: none;font-size: 14px;'>" +
                                url +
                            "</a>" +
                            "</div>" +
                        "</td>" +
                    "</tr>" +
                    "</tbody></table>";
            }
            else
            {
                urlSection = "<br/><br/><br/>";
            }

            #region MailBody
            string year = DateTime.Now.Year.ToString();
            string MailBody = "<!DOCTYPE html>" +
                "<html>" +
                "<head>" +
                    "<title>Zuptu Mail</title>" +
                    "<style>" +
                        "@media only screen and (min-width: 620px) {" +
                            ".u-row {" +
                                "width: 600px !important;" +
                            "}" +
                            ".u-row .u-col {" +
                                "vertical-align: top;" +
                            "}" +

                            ".u-row .u-col-33p33 {" +
                                "width: 199.98px !important;" +
                            "}" +

                            ".u-row .u-col-66p67 {" +
                                "width: 400.02px !important;" +
                            "}" +

                            ".u-row .u-col-100 {" +
                                "width: 600px !important;" +
                            "}" +
                        "}" +

                        "@media (max-width: 620px) {" +
                            ".u-row-container {" +
                                "max-width: 100% !important;" +
                                "padding-left: 0px !important;" +
                                "padding-right: 0px !important;" +
                            "}" +
                            ".u-row .u-col {" +
                                "min-width: 320px !important;" +
                                "max-width: 100% !important;" +
                                "display: block !important;" +
                            "}" +
                            ".u-row {" +
                                "width: 100% !important;" +
                            "}" +
                            ".u-col {" +
                                "width: 100% !important;" +
                            "}" +
                            ".u-col > div {" +
                                "margin: 0 auto;" +
                            "}" +
                        "}" +
                        ".btnColor{" +
                            "background: #04509d;" +
                        "}" +
                        ".btnColor:hover{" +
                            "background:#008049;" +
                        "}" +
                        "p{font-family: 'Lato', Helvetica, Arial, sans-serif;}" +
                        "a{font-family: 'Lato', Helvetica, Arial, sans-serif;}" +
                        "h2{font-family: 'Lato', Helvetica, Arial, sans-serif;}" +
                        "table, td { color: #000000; } #u_body a { color: #0000ee; text-decoration: underline; } @media (max-width: 480px) { #u_content_image_1 .v-container-padding-padding { padding: 40px 0px 0px !important; } #u_content_image_1 .v-src-width { width: auto !important; } #u_content_image_1 .v-src-max-width { max-width: 55% !important; } #u_content_heading_3 .v-font-size { font-size: 18px !important; } #u_content_heading_2 .v-container-padding-padding { padding: 5px 10px 40px !important; } #u_content_heading_4 .v-container-padding-padding { padding: 40px 10px 0px !important; } #u_content_heading_4 .v-text-align { text-align: center !important; } #u_content_divider_1 .v-container-padding-padding { padding: 10px 10px 10px 125px !important; } #u_content_text_2 .v-container-padding-padding { padding: 10px 10px 40px !important; } #u_content_text_2 .v-text-align { text-align: center !important; } #u_content_button_1 .v-size-width { width: 60% !important; } #u_content_image_4 .v-container-padding-padding { padding: 40px 10px 10px !important; } #u_content_image_4 .v-src-width { width: auto !important; } #u_content_image_4 .v-src-max-width { max-width: 30% !important; } #u_content_heading_7 .v-container-padding-padding { padding: 10px 10px 0px !important; } #u_content_heading_7 .v-text-align { text-align: center !important; } #u_content_text_5 .v-container-padding-padding { padding: 5px 10px 40px !important; } #u_content_text_5 .v-text-align { text-align: center !important; } #u_row_4.v-row-padding--vertical { padding-top: 0px !important; padding-bottom: 0px !important; } #u_content_image_2 .v-container-padding-padding { padding: 40px 10px 10px !important; } #u_content_image_2 .v-src-width { width: auto !important; } #u_content_image_2 .v-src-max-width { max-width: 30% !important; } #u_content_heading_1 .v-container-padding-padding { padding: 10px 10px 0px !important; } #u_content_heading_1 .v-text-align { text-align: center !important; } #u_content_text_1 .v-container-padding-padding { padding: 5px 10px 40px !important; } #u_content_text_1 .v-text-align { text-align: center !important; } #u_content_social_1 .v-container-padding-padding { padding: 30px 10px 10px !important; } #u_content_text_deprecated_1 .v-container-padding-padding { padding: 10px 10px 20px !important; } #u_content_image_5 .v-container-padding-padding { padding: 20px 10px 30px !important; } }" +
                    "</style>" +
                "</head>" +

                "<body class='clean-body u_body' style='margin: 0;padding: 0;-webkit-text-size-adjust: 100%;background-color: #ffffff;color: #000000'>" +
                    "<table  id='u_body' style='border-collapse: collapse;table-layout: fixed;border-spacing: 0;mso-table-lspace: 0pt;mso-table-rspace: 0pt;vertical-align: top;min-width: 320px;Margin: 0 auto;background-color: #ffffff;width:100%' cellpadding='0' cellspacing='0'>" +
                        "<tr style='vertical-align: top'>" +
                            "<td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top'>" +
                                "<div class='u-row-container v-row-padding--vertical' style='padding: 0px;background-color: transparent'>" +
                                    "<div class='u-row' style='margin: 0 auto;min-width: 320px;max-width: 600px;overflow-wrap: break-word;word-wrap: break-word;word-break: break-word;background-color: transparent;'>" +
                                        "<div style='border-collapse: collapse;display: table;width: 100%;height: 100%;background-color: transparent;'>" +
                                            "<div class='u-col u-col-100' style='max-width: 320px;min-width: 600px;display: table-cell;vertical-align: top;'>" +
                                                "<div style='padding:35px; background-color: #f0f5f6;height: 100%;width: 100% !important;border-radius: 0px;-webkit-border-radius: 0px; -moz-border-radius: 0px;'>" +
                                                    "<div style='box-sizing: border-box; height: 100%; padding: 0px;border-top: 0px solid transparent;border-left: 0px solid transparent;border-right: 0px solid transparent;border-bottom: 0px solid transparent;border-radius: 0px;-webkit-border-radius: 0px; -moz-border-radius: 0px;'>" +
                                                        "<table id='u_content_heading_4' style='font-family:'Raleway',sans-serif;' role='presentation' cellpadding='0' cellspacing='0' width='100%' border='0'>" +
                                                            "<tbody>" +
                                                            "<tr>" +
                                                                "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:50px 60px 0px;font-family:'Raleway',sans-serif;' align='left'>" +
                                                                "<h1 class='v-text-align v-font-size' style='margin: 0px; line-height: 140%; text-align: left; word-wrap: break-word; font-size: 20px; font-weight: 400;'><strong>Subject: " + subject + " </strong></h1>" +
                                                                "</td>" +
                                                            "</tr>" +
                                                            "</tbody>" +
                                                        "</table>" +

                                                        "<table id='u_content_divider_1' style='font-family:'Raleway',sans-serif;' role='presentation' cellpadding='0' cellspacing='0' width='100%' border='0'>" +
                                                            "<tbody>" +
                                                                "<tr>" +
                                                                    "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:10px 10px 10px 60px;font-family:'Raleway',sans-serif;' align='left'>" +
                                                                        "<table height='0px' align='left' border='0' cellpadding='0' cellspacing='0' width='38%' style='border-collapse: collapse;table-layout: fixed;border-spacing: 0;mso-table-lspace: 0pt;mso-table-rspace: 0pt;vertical-align: top;border-top: 2px solid #BBBBBB;-ms-text-size-adjust: 100%;-webkit-text-size-adjust: 100%'>" +
                                                                            "<tbody>" +
                                                                                "<tr style='vertical-align: top'>" +
                                                                                    "<td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;font-size: 0px;line-height: 0px;mso-line-height-rule: exactly;-ms-text-size-adjust: 100%;-webkit-text-size-adjust: 100%'>" +
                                                                                        "<span>&#160;</span>" +
                                                                                    "</td>" +
                                                                                "</tr>" +
                                                                            "</tbody>" +
                                                                        "</table>" +
                                                                    "</td>" +
                                                                "</tr>" +
                                                            "</tbody>" +
                                                        "</table>" +

                                                        "<table id='u_content_text_2' style='font-family:'Raleway',sans-serif;' role='presentation' cellpadding='0' cellspacing='0' width='100%' border='0'>" +
                                                            "<tbody>" +
                                                                "<tr>" +
                                                                    "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:10px 60px 50px;font-family:'Raleway',sans-serif;' align='left'>" +
                                                                        "<div class='v-text-align v-font-size' style='font-size: 14px; line-height: 140%; text-align: justify; word-wrap: break-word;'>" +
                                                                            "<p style='line-height: 140%;'> </p>" +
                                                                            "<p style='line-height: 140%;'>" + description + "</p>" +
                                                                            "<p style='line-height: 140%;'> </p>" +
                                                                        "</div>" +
                                                                    "</td>" +
                                                                "</tr>" +
                                                            "</tbody>" +
                                                        "</table>" +

                                                        "<table >" +
                                                        "<tbody>" +
                                                          urlSection +
                                                        "</tbody>" +
                                                      "</table>" +

                                                      "<table id='u_content_text_deprecated_1' style='font-family:'Raleway',sans-serif;' role='presentation' cellpadding='0' cellspacing='0' width='100%' border='0'>" +
                                                            "<tbody>" +
                                                                "<tr>" +
                                                                    "<td class='v-container-padding-padding' style='overflow-wrap:break-word;word-break:break-word;padding:10px 100px 30px;font-family:'Raleway',sans-serif;' align='left'>" +
                                                                        "<div class='v-text-align v-font-size' style='font-size: 14px; line-height: 170%; text-align: center; word-wrap: break-word;'>" +
                                                                            "<p style='font-size: 14px; line-height: 170%;'> PRIVACY POLICY   |   WEB</p>" +
                                                                            "<p style='font-size: 14px; line-height: 170%;'>Powered By IT Valet</p>" +
                                                                        "</div>" +
                                                                    "</td>" +
                                                                "</tr>" +
                                                            "</tbody>" +
                                                        "</table>" +
                                                    "</div>" +
                                                "</div>" +
                                            "</div>" +
                                        "</div>" +
                                    "</div>" +
                                "</div>" +
                            "</td>" +
                        "</tr>" +
                    "</table>" +
                "</body>" +
            "</html>";
            #endregion

            return MailBody;
        }

        //private static bool SendEmail(string email, string subject, string MailBody)
        //{
        //    try
        //    {
        //        RestClient client = new RestClient();
        //        var Url = "https://api.mailgun.net/v3";
        //        client = new RestClient(Url);

        //        RestRequest request = new RestRequest();
        //        //To Set the Authenticator In Latest Versions of Rest Client
        //        request.Authenticator = new HttpBasicAuthenticator("api", "496e6c6979cb786921579085c5b07222-8d821f0c-bde767e8");

        //        request.AddParameter("domain", "usmandev.ca", ParameterType.UrlSegment);
        //        request.Resource = "{domain}/messages";
        //        request.AddParameter("from", ProjectVariables.FromEmail);
        //        request.AddParameter("to", email);
        //        request.AddParameter("subject", "IT Valet | Password Reset");
        //        request.AddParameter("html", MailBody);

        //        string response = client.Post(request).Content.ToString();

        //        if (response.ToLower().Contains("queued"))
        //            return true;
        //        else
        //            return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        public static bool SendEmail(string receiverEmail, string SubjectTitle, string EmailBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SubjectTitle, "nodlaysnodlays@gmail.com"));
                message.To.Add(new MailboxAddress("", receiverEmail));
                message.Subject = SubjectTitle;
                message.Body = new TextPart("html")
                {
                    //Text = "Hello Word Mail"
                    Text = EmailBody
                };
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);

                    client.AuthenticationMechanisms.Remove("NTLM");

                    client.Authenticate("nodlaysnodlays@gmail.com", "orithgrwpkhopqev");

                    client.Send(message);

                    client.Disconnect(true);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
