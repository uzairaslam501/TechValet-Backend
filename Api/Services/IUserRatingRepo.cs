using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Stripe;

namespace ITValet.Services
{
    public interface IUserRatingRepo
    {
        Task<UserRating?> GetUserRatingById(int id);
        Task<IEnumerable<UserRating>> GetUserRatingList();
        Task<List<int>> GetUserRatingStarsListByUserId(int userId);
        Task<IEnumerable<UserRating>> GetUserRatingListByUserId(int userId);
        Task<bool> AddUserRating(UserRating userRating);
        Task<bool> UpdateUserRating(UserRating userRating);
        Task<bool> IsValetHasMoreBadReviews(int id);
        Task<bool> DeleteUserRating(int id);
        Task<OrderRating?> GetOrderRatingByOrderId(int orderId);
        Task<ResponseDto> GetValetRatingRecords(string userId);
        string? CalculateAverageStars(int id);
        Task<double> GetValetStars(string endId);
        Task<string> GetValetStarsRatingForPayment(string encId);
    }

    public class UserRatingRepo : IUserRatingRepo
    {
        private readonly AppDbContext _context;
        private readonly IUserRepo _userRepo;
        private readonly ProjectVariables _projectVariables;
        public UserRatingRepo(AppDbContext _appDbContext, IUserRepo userRepo, IOptions<ProjectVariables> options)
        {
            _context = _appDbContext;
            _userRepo = userRepo;
            _projectVariables = options.Value;
        }
        
        public async Task<bool> AddUserRating(UserRating UserRating)
        {
            try
            {
                _context.UserRating.Add(UserRating);
                await _context.SaveChangesAsync();
              /*  if (UserRating.ValetId.HasValue)
                {
                    await IsValetHasMoreBadReviews(UserRating.ValetId.Value);
                }*/
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<OrderRating?> GetOrderRatingByOrderId(int orderId)
        {
            try
            {
                var orderRatingObj =  await _context.UserRating.FirstOrDefaultAsync(x => x.OrderId == orderId);
                if (orderRatingObj != null)
                {
                    OrderRating obj = new OrderRating();
                    obj.Reviews = orderRatingObj.Reviews;
                    obj.Stars = orderRatingObj.Stars;
                    return obj;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string? CalculateAverageStars(int id)
        {
            try
            {
                var ratingList = _context.UserRating.Where(x => x.ValetId == id).ToList();
                if (ratingList.Count > 0)
                {
                    var sumOfRating = ratingList.Sum(x => x.Stars);
                    double averageRating = (double)sumOfRating / ratingList.Count;
                    string formattedRating = averageRating.ToString("F1");
                    return formattedRating;
                }
                return "0";
            }
            catch (Exception ex)
            {
                return "0";
            }
        }

        public async Task<bool> IsValetHasMoreBadReviews(int id)
        {
            try
            {
                var checkRatingRecord = await _context.UserRating.Where(x => x.ValetId == id).ToListAsync();
                var IsMoreBadReviews = checkRatingRecord.Where(x => x.Stars <= 2).ToList();
                var user = await _userRepo.GetUserById(id);
                if (IsMoreBadReviews.Count == 3)
                {
                   await MailSender.SendWarningForBadReviewsAnalysis(user.Email, user.UserName);
                }
                else if(IsMoreBadReviews.Count >= 5)
                {
                    if (user != null)
                    {
                        bool accoundOnHold = await _userRepo.UpdateUserForHold(id);
                        await MailSender.SendAccountBlockedNotification(user.Email, user.UserName);
                        return true;

                    }
                }
                return false;
            }
            catch 
            {
                return false;
            }
        }

        public async Task<List<int>> GetUserRatingStarsListByUserId(int userId)
        {
            return await _context.UserRating.Where(x => x.ValetId == userId).Select(x => x.Stars.Value).ToListAsync();
        }
        
        public async Task<bool> DeleteUserRating(int id)
        {
            try
            {
                UserRating? UserRating = await GetUserRatingById(id);

                if (UserRating != null)
                {
                    UserRating.IsActive = 0;
                    UserRating.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUserRating(UserRating);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<UserRating?> GetUserRatingById(int id)
        {
            return await _context.UserRating.FindAsync(id);
        }

        public async Task<IEnumerable<UserRating>> GetUserRatingList()
        {
            return await _context.UserRating.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<IEnumerable<UserRating>> GetUserRatingListByUserId(int userId)
        {
            return await _context.UserRating.Where(x => (x.ValetId == userId || x.CustomerId == userId)).ToListAsync();
        }

        public async Task<bool> UpdateUserRating(UserRating UserRating)
        {
            try
            {
                _context.Entry(UserRating).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<ResponseDto> GetValetRatingRecords(string userId)
        {
            try
            {
                var Id = DecryptionId(userId);
                var ratingList = await _context.UserRating.Where(x => x.ValetId == Id).ToListAsync();

                // If no ratings exist, return early to avoid further processing
                if (!ratingList.Any())
                {
                    throw new Exception();
                }

                int? sumOfRatingStars = ratingList.Sum(x => x.Stars);
                double averageRating = (double)sumOfRatingStars / ratingList.Count;

                string formattedRating = averageRating.ToString("F1");

                var valetRatingReviewRecord = new ValetRatingReviewRecord
                {
                    AverageStars = formattedRating,
                    Rating = new List<ValetRatingRecord>()
                };

                if (ratingList.Any())
                {
                    List<int?> customerIds = ratingList.Select(x => x.CustomerId).ToList();
                    var customerRecords = await _userRepo.GetCustomerInfoRecord(customerIds);

                    foreach (var rating in ratingList)
                    {
                        var valetRating = new ValetRatingRecord
                        {
                            Reviews = rating.Reviews,
                            Stars = rating.Stars,
                            PublishDate = rating.CreatedAt.Value.Date.ToString("yyyy-MM-dd"),
                        };

                        var customerRecord = customerRecords.FirstOrDefault(c => c.Id == rating.CustomerId);
                        valetRating.Customer = customerRecord;

                        valetRatingReviewRecord.Rating.Add(valetRating);
                    }
                }
                return GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", valetRatingReviewRecord);
            }
            catch (Exception ex)
            {
                CreateLogger(ex);
                var record = new ValetRatingReviewRecord
                {
                    AverageStars = "0.0", // You can set a default value for average stars here
                    Rating = new List<ValetRatingRecord>()
                };
                return GeneralPurpose.GenerateResponseCode(true, "200", "Record Found", record);
            }
        }
        
        public async Task<double> GetValetStars(string endId)
        {
            try
            {
                int valetId = StringCipher.DecryptId(endId);
                var valetRatingList = await _context.UserRating.Where(x => x.ValetId == valetId).ToListAsync();
                int? sumOfRatingStars = valetRatingList.Sum(x => x.Stars);
                double averageRating = (double)sumOfRatingStars / valetRatingList.Count;
                return averageRating;
                
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task<string> GetValetStarsRatingForPayment(string encId)
        {
            try
            {
                int valetId = StringCipher.DecryptId(encId);
                var valetRatingList = await _context.UserRating.Where(x => x.ValetId == valetId).ToListAsync();

                int totalRatings = valetRatingList.Count;
                int totalStars = (int)valetRatingList.Sum(x => x.Stars);
                double averageRating = (double)totalStars / totalRatings;

                bool hasFiveStarReviews = valetRatingList.Count(x => x.Stars == 5) >= 5;
                bool hasTenStarReviews = valetRatingList.Count(x => x.Stars == 5) >= 10;

                // Determine the hourly rate and commission based on the rating criteria
                double hourlyRate = 24.99;
                double commissionPercentage = 20; // 20% commission

                if (hasFiveStarReviews && averageRating >= 4.8)
                {
                    hourlyRate = 29.99;
                    commissionPercentage = 17; // 17% commission
                }
                if (hasTenStarReviews && averageRating >= 4.8)
                {
                    hourlyRate = 34.99;
                    commissionPercentage = 15; // 15% commission
                }
                

                // Calculate the commission amount
                double commission = hourlyRate * (commissionPercentage / 100);

                // Final hourly rate after deducting commission
                double finalHourlyRate = hourlyRate - commission;
                var result = new
                {
                    hourlyRate = finalHourlyRate,
                    commission = commission
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<List<ValetRatingRecord>> GetValetRatingRecord(string endId)
        {
            try
            {
                int valetId = StringCipher.DecryptId(endId);
                var valetRatingList = await _context.UserRating.Where(x => x.ValetId == valetId).ToListAsync();
                int? sumOfRatingStars = valetRatingList.Sum(x => x.Stars);
                double averageRating = (double)sumOfRatingStars /valetRatingList.Count;
                string formattedRating = averageRating.ToString("F1");
                if (valetRatingList.Any())
                {
                    List<int?> customerIds = valetRatingList.Select(x => x.CustomerId).ToList();
                    var customerRecords = await _userRepo.GetCustomerInfoRecord(customerIds);

                    var valetRatingRecordData = new List<ValetRatingRecord>();
                    foreach (var rating in valetRatingList)
                    {
                        var valetRating = new ValetRatingRecord();
                        valetRating.Reviews = rating.Reviews;
                        valetRating.Stars = rating.Stars;
                        valetRating.PublishDate = rating.CreatedAt.Value.Date.ToString();
                        var customerRecord = customerRecords.FirstOrDefault(c => c.Id == rating.CustomerId);
                        valetRating.Customer = customerRecord;

                        valetRatingRecordData.Add(valetRating);
                    }

                    return valetRatingRecordData;
                }

                return new List<ValetRatingRecord>();
            } 
            catch(Exception ex)
            {
                return new List<ValetRatingRecord>();
            }     
        }

        private int DecryptionId(string userId)
        {
            userId = GeneralPurpose.ConversionEncryptedId(userId);
            var decrypt = StringCipher.DecryptId(userId);
            return decrypt;
        }

        private async void CreateLogger(Exception ex)
        {
            await MailSender.SendErrorMessage($"URL: {_projectVariables.BaseUrl}<br/> Exception Message:  {ex.Message} <br/> Stack Trace: {ex.StackTrace}");
        }
    }
}
