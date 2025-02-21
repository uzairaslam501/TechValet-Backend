using ITValet.Models;
using ITValet.Services;
using ITValet.ViewModel;
using System.Net.NetworkInformation;
using System.Web;

namespace ITValet.HelpingClasses
{
    public class GeneralPurpose
    {
        public static DateTime DateTimeNow()
        {
            return DateTime.UtcNow;
        }

        public static string GetNextOccurrenceOfDay(string day)
        {
            DayOfWeek selectedDayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), day);
            DateTime nextOccurrence = DateTime.UtcNow.Date.AddDays((7 + selectedDayOfWeek - DateTime.UtcNow.DayOfWeek) % 7);
            return nextOccurrence.ToString();
        }

        public static ResponseDto GenerateResponseCode(bool status, string statusCode, string message, object? data = null)
        {
            ResponseDto obj = new ResponseDto()
            {
                Status = status,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };

            return obj;
        }

        public static string regionChanged(DateTime dat, string region = "")
        {
            try
            {
                DateTime dt = Convert.ToDateTime(dat);
                if (region.Contains("/"))
                {
                    dt = DateTimeHelper.GetZonedDateTimeFromUtc(dat, region);
                }
                else
                {
                    if (!string.IsNullOrEmpty(region))
                    {
                        if (region.Contains("-"))
                        {
                            string ss = region.Split('-')[1];
                            ss = ss.Replace(':', '.');
                            double s = Convert.ToDouble(ss);
                            dt = dt.AddHours(-s);
                        }
                        else
                        {
                            string ss = region;
                            ss = ss.Replace(':', '.');
                            double s = Convert.ToDouble(ss);
                            dt = dt.AddHours(+s);
                        }
                    }
                }
                return dt.ToString("G");
            }
            catch (Exception ex)
            {
                ProjectVariables projectVariables = new ProjectVariables();
                CreateLogger(projectVariables, ex);
                return "";
            }
        }

        public static string convertToUtc(DateTime dat, string region = "")
        {
            DateTime dt = Convert.ToDateTime(dat);
            if (!string.IsNullOrEmpty(region))
            {
                if (region.Contains("-"))
                {
                    string ss = region.Split('-')[1];
                    ss = ss.Replace(':', '.');
                    double s = Convert.ToDouble(ss);
                    dt = dt.AddHours(+s);
                }
                else
                {
                    string ss = region;
                    ss = ss.Replace(':', '.');
                    double s = Convert.ToDouble(ss);
                    dt = dt.AddHours(-s);
                }
            }
            return dt.ToString("G");
        }

        public bool CheckInternet()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static long CalculateSumOfDigits(long number)
        {
            long sum = 0;
            while (number != 0)
            {
                long digit = number % 10;
                sum += digit;
                number /= 10;
            }

            return sum;
        }

        public static decimal CalculatePrices(DateTime startDate, DateTime endDate)
        {
            TimeSpan duration = endDate - startDate;
            double totalHours = duration.TotalHours;
            decimal hourlyRate = 25;
            decimal price = (decimal)totalHours * hourlyRate;
            decimal transactionFee = CalculateTransactionFees(price);
            return price + transactionFee;
        }

        public static (decimal price, decimal fee) CalculatePrice(DateTime startDate, DateTime endDate,
            decimal hourlyRate)
        {
            TimeSpan duration = endDate - startDate;
            double totalHours = Math.Ceiling(duration.TotalHours); // Round up to the nearest hour
            decimal price = (decimal)totalHours * hourlyRate;
            decimal transactionFee = CalculateTransactionFees(price);
            return (price,  transactionFee);
        }

        public static (decimal OrderPrice, decimal TotalPrice) CalculatePricesss(DateTime startDate, DateTime endDate)
        {
            TimeSpan duration = endDate - startDate;
            double totalHours = duration.TotalHours;
            decimal hourlyRate = 25;

            // Round up to the nearest hour
            totalHours = Math.Ceiling(totalHours);

            decimal orderPrice = (decimal)totalHours * hourlyRate;
            decimal transactionFee = CalculateTransactionFees(orderPrice);
            decimal totalPrice = orderPrice + transactionFee;

            return (orderPrice, totalPrice);
        }
        
        public static decimal CalculateTransactionFees(decimal amount)
        {
            decimal feePercentage = 0.04m; // 4%
            decimal transactionFee = amount * feePercentage;
            return transactionFee;
        }

        public static DateTime CalculatePayPalTransferFundDate()
        {
            DateTime startDate = DateTime.Now;
            int numberOfDays = 14;

            TimeSpan duration = TimeSpan.FromDays(numberOfDays);
            DateTime transferDate = startDate + duration;

            return transferDate;
        }

        public static decimal CalculateHSTFee(decimal amount)
        {
            decimal hstPercentage = 0.13m; 
            decimal hstFee = amount * hstPercentage;
            return hstFee;
        }

        public static string CalculcateTimeDifference(string dateTime1, string dateTime2)
        {
            TimeSpan difference = Convert.ToDateTime(dateTime2) - Convert.ToDateTime(dateTime1);

            // Extract days, hours, and minutes
            int days = Math.Abs(difference.Days);
            int hours = Math.Abs(difference.Hours);
            int minutes = Math.Abs(difference.Minutes);
            return $"{days} days, {hours} hours, and {minutes} minutes";
        }

        #region UserRating

        public static double CalculateUserRatingPercentage(List<int> ratings)
        {
            if (ratings.Count == 0)
            {
                return 0.0; // No ratings yet.
            }

            // Calculate the average rating.
            double totalRating = ratings.Sum();
            double averageRating = totalRating / ratings.Count;
            double roundedAverageRating = Math.Round(averageRating, 1);
            // Convert to a percentage of 5 stars.
            //double percentageRating = (averageRating / 5.0) * 100.0; // for calculating percentage in future

            return roundedAverageRating;
        }

        public static async Task<int> CheckValuesNotEmpty(User userObj, IUserSkillRepo _userSkillRepo)
        {
            if (userObj == null) return 0;

            var requiredFields = new List<string>
            {
                userObj?.Description, userObj?.FirstName, userObj?.LastName, userObj?.UserName,
                userObj?.State, userObj?.City, userObj?.ZipCode, userObj?.Timezone,
                userObj?.Gender, userObj?.Country, userObj?.Email, userObj?.StripeId
            };

            if (requiredFields.Any(string.IsNullOrEmpty) || userObj?.BirthDate == null ||
                userObj?.IsVerify_StripeAccount != 1 || userObj?.IsPayPalAccount != 1)
            {
                return 0;
            }

            var userSkillCount = await _userSkillRepo.GetUserSkillCountByIdAsync(userObj.Id);
            return userSkillCount > 0 ? 1 : 0;
        }

        #endregion

        #region Responses 
        public static string ConversionEncryptedId(string encryptedId)
        {
            encryptedId = HttpUtility.UrlDecode(encryptedId);
            var lowerValue = "%2F";
            if (encryptedId.Contains(lowerValue))
            {
                encryptedId = encryptedId.Replace(lowerValue, "/");
            }
            return encryptedId;
        }

        public static string ConversionEncrypted(string encryptedId)
        {
            var lowerValue = "%2F";
            if (encryptedId.Contains(lowerValue))
            {
                encryptedId = encryptedId.Replace(lowerValue, "/");
            }
            return encryptedId;
        }

        public static ResponseViewModel NotFoundResponse(string message, Object? data = null)
        {
            return new ResponseViewModel()
            {
                IsSuccess = false,
                StatusCode = 404,
                Message = message,
                Data = default
            };
        }

        public static ResponseViewModel BadRequestResponse(string message, Object? data = null)
        {
            return new ResponseViewModel()
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = message,
                Data = default
            };
        }

        public static ResponseViewModel SuccessResponse(string message, Object? data)
        {
            return new ResponseViewModel()
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = message,
                Data = data
            };
        }

        public static ResponseViewModel OkResponse(string message, Object? data)
        {
            return new ResponseViewModel()
            {
                IsSuccess = true,
                StatusCode = 201,
                Message = message,
                Data = data
            };
        }

        public static ResponseDto GenerateResponse(bool Status, string StatusCode, string? Message = "", Object? data = null)
        {
            return new ResponseDto()
            {
                Status = Status,
                StatusCode = StatusCode,
                Message = Message,
                Data = data,
            };
        }

        #endregion

        #region Image Handling
        public static async Task<string> UploadFiles(IFormFile file, string? uploadedFiles = "")
        {
            try
            {
                string profileImagesPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\" + uploadedFiles);
                if (!Directory.Exists(profileImagesPath))
                {
                    DirectoryInfo di = Directory.CreateDirectory(profileImagesPath);
                }
                var getFileName = Path.GetFileNameWithoutExtension(file.FileName);
                if (getFileName.Contains(" "))
                {
                    getFileName = getFileName.Replace(" ", "-");
                }
                var getFileExtentions = Path.GetExtension(file.FileName);
                string imgName = getFileName + "_" + DateTime.Now.Ticks.ToString() + getFileExtentions;
                var rootDir = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\" + uploadedFiles, imgName);
                using (var stream = new FileStream(rootDir, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return uploadedFiles + "/" + imgName;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public static bool DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                if (File.Exists(filePath))
                    File.Delete(filePath);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if required
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Add User

        public static bool MatchPassword(string password, string confirmPassword)
        {
            return password == confirmPassword;
        }

        public static User MapUser(RegisterUserDto user, User obj)
        {
            if (string.IsNullOrEmpty(user.Timezone))
                user.Timezone = "Canada/Mountain";

            return obj = new User
            {
                FirstName = user.Firstname!.Trim(),
                LastName = user.Lastname!.Trim(),
                UserName = user.Username!.Trim(),
                Email = user.Email,
                Password = StringCipher.Encrypt(user.Password!),
                Country = user.Country,
                State = user.State,
                City = user.City,
                ZipCode = user.PostalCode,
                Timezone = user.Timezone,
                IsActive = 3,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

        public static User SetRoles(string userRole, User obj)
        {

            if (!Enum.TryParse<EnumRoles>(userRole, true, out var role))
                throw new ArgumentException("Invalid or missing role");

            obj.Role = (int)role;

            if (role == EnumRoles.Valet)
            {
                obj.PricePerHour = 24.99m;
                obj.HST = 13;
            }

            return obj;
        }

        #endregion

        #region Order Payment
        public static string OrderPaidBy(string? PaymentId, string? CaptureId, string? StripeChargeId, string? PackageBuyFrom)
        {
            string orderPaidBy = string.Empty;

            if (!string.IsNullOrEmpty(PackageBuyFrom))
            {
                orderPaidBy = PackageBuyFrom;
            }
            else if (!string.IsNullOrEmpty(PaymentId) && !string.IsNullOrEmpty(CaptureId))
            {
                orderPaidBy = "PAYPAL";
            }
            else if (!string.IsNullOrEmpty(StripeChargeId))
            {
                orderPaidBy = "STRIPE";
            }

            return orderPaidBy;
        }

        public static string EarnedAmountFromOrder(decimal OrderPrice)
        {
            var orderHstFee = CalculateHSTFee(OrderPrice);
            decimal earnedAmount = OrderPrice - orderHstFee;
            return earnedAmount.ToString("0.00");
        }
        #endregion
        public static async void CreateLogger(ProjectVariables _projectVariables, Exception ex)
        {
            await MailSender.SendErrorMessage($"URL: {_projectVariables.BaseUrl}<br/> Exception Message:  {ex.Message} <br/> Stack Trace: {ex.StackTrace}");
        }
    }
}
