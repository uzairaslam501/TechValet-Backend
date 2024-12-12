using ITValet.Models;
using ITValet.Services;
using ITValet.ViewModel;
using System.Net.NetworkInformation;

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

        public static (decimal price, decimal fee) CalculatePrice(DateTime startDate, DateTime endDate)
        {
            TimeSpan duration = endDate - startDate;
            double totalHours = Math.Ceiling(duration.TotalHours); // Round up to the nearest hour
            decimal hourlyRate = 25;
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
        public static async Task<int> CheckValuesNotEmpty(User UserRecord, IUserExperienceRepo _userExperienceRepo, IUserSkillRepo _userSkillRepo, IPayPalGateWayService payPalGateWayService, IUserEducationRepo _userEducationRepo)
        {
            int IsCompleteValetAccount = 1;
            if (
                string.IsNullOrEmpty(UserRecord.Description) ||
                string.IsNullOrEmpty(UserRecord.FirstName) ||
                string.IsNullOrEmpty(UserRecord.LastName) ||
                string.IsNullOrEmpty(UserRecord.UserName) ||
                UserRecord.BirthDate == null ||
                string.IsNullOrEmpty(UserRecord.State) ||
                string.IsNullOrEmpty(UserRecord.City) ||
                string.IsNullOrEmpty(UserRecord.ZipCode) ||
                string.IsNullOrEmpty(UserRecord.Timezone) ||
                string.IsNullOrEmpty(UserRecord.Gender) ||
                string.IsNullOrEmpty(UserRecord.Country) ||
                string.IsNullOrEmpty(UserRecord.Email) ||
                string.IsNullOrEmpty(UserRecord.StripeId) ||
                UserRecord.IsVerify_StripeAccount != 1
            )
            {
                IsCompleteValetAccount = 0;
            }
            if (IsCompleteValetAccount != 0)
            {
                var account = await payPalGateWayService.GetPayPalAccount(UserRecord.Id);
                int UserExperienceCount = await _userExperienceRepo.GetUserExperienceCountByUserId(UserRecord.Id);
                int UserEducationCount = await _userEducationRepo.GetUserEducationCountByUserId(UserRecord.Id);
                int UserSkillCount = (int)await _userSkillRepo.GetUserSkillCountByIdAsync(UserRecord.Id);
                UserSkillCount = UserSkillCount == 0 ? IsCompleteValetAccount = 0 : IsCompleteValetAccount = IsCompleteValetAccount;
                UserExperienceCount = UserExperienceCount == 0 ? IsCompleteValetAccount = 0 : IsCompleteValetAccount = IsCompleteValetAccount;
                UserEducationCount = UserEducationCount == 0 ? IsCompleteValetAccount = 0 : IsCompleteValetAccount = IsCompleteValetAccount;
                if (account == null)
                {
                    IsCompleteValetAccount = 0;
                }
            }
            return IsCompleteValetAccount;
        }
        #endregion

        #region Responses 
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

        #endregion
    }
}
