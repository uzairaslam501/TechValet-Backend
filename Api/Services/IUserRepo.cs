using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace ITValet.Services
{
    public interface IUserRepo
    {
        Task<User?> GetUserById(int id);
        Task<int> GetUserCount(int Role, EnumActiveStatus statuses);
        Task<int> GetUserCountPendingVerifications(int Role);
        Task<User> GetUserByLogin(string email, string password);
        Task<IEnumerable<User>> GetUserList(int Role);
        Task<IEnumerable<User>> GetOnlyActiveUserList(int Role);
        Task<IEnumerable<User>> GetAccountOnHold(int Role);
        Task<List<ActiveUsersNameDto>> FetchAllUsersName();
        Task<bool> AddUser(User user);
        Task<bool> UpdateUser(User user);
        Task<bool> UpdateUserWithoutSavingInDatabase(User user);
        Task<bool> UpdateUserAccountStatus(int id, EnumActiveStatus Status);
        Task<bool> UpdateUserAccountActivityStatus(int id, int status);
        Task<bool> UpdateUserAccountAvailabilityStatus(int id, int availability);
        Task<bool> ValidateEmail(string email, int id = -1);
        Task<bool> ValidateUsername(string username, int id = -1);
        Task<bool> UpdateUserForHold(int id);
        Task<bool> DeleteUser(int id);
        Task<User> GetUserByEmail(string email);
        Task<Dictionary<int, string>> GetUserNames(List<int> userIds);
        Task<IEnumerable<User>> GetUsersListByRequestSkills(string RequestSkills);
        Task<bool> TransferFunds(string destinationAccountId, decimal amount);
        Task<List<User>> GetValetRecord();
        Task<List<User?>> GetSkilledUsersByIds(List<int?> userIds);
        Task<List<User?>> GetUsersByName(string userName);
        Task<bool> CheckValetAvailability(int valetId, string StartDate, string EndDate);
        Task<bool> IsDateRangeAvailable(int valetId, DateTime startDate, DateTime endDate);
        Task<List<ValetAvailableSlots>> GetValetAvailableSlots(int valetId);
        Task<bool> SaveChanges();
        Task<List<CustomerInfo>> GetCustomerInfoRecord(List<int?> customerIds);

    }

    public class UserRepo : IUserRepo
    {
        private readonly AppDbContext _context;
        private readonly ProjectVariables _projectVariables;

        public UserRepo(AppDbContext _appDbContext, IOptions<ProjectVariables> projectVariable)
        {
            _context = _appDbContext;
            _projectVariables = projectVariable.Value;
        }

        public async Task<List<User?>> GetSkilledUsersByIds(List<int?> userIds)
        {
            try
            {
                List<User?> users = await _context.User
                    .Where(u => userIds.Contains(u.Id) && u.Role != 1 && u.IsBankAccountAdded == 1)
                    .ToListAsync<User?>();

                return users;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<User?>> GetUsersByName(string userName)
        {
            try
            {
                List<User?> users;
                List<string> searchTerms = userName.Split(' ').ToList();

                if (searchTerms.Count == 1)
                {
                    users = await _context.User
                        .Where(u => u.Role == 4 &&
                                    //u.IsBankAccountAdded == 1 && // Check for the value indicating the bank account is added
                                    u.IsActive == (int)EnumActiveStatus.Active &&
                                    (u.UserName == userName || u.FirstName == userName || u.LastName == userName || u.Email == userName))
                        .ToListAsync<User?>();

                    return users;
                }
                else if (searchTerms.Count > 1)
                {
                    users = await _context.User
                        .Where(u => u.Role != 1 &&
                                    //u.IsBankAccountAdded == 1 && // Check for the value indicating the bank account is added
                                    u.IsActive == (int)EnumActiveStatus.Active &&
                                    (u.UserName == searchTerms[1] || u.FirstName == searchTerms[1] || u.LastName == searchTerms[1] || u.Email == searchTerms[1]))
                        .ToListAsync<User?>();

                    return users;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> AddUser(User user)
        {
            try
            {
                _context.User.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<ActiveUsersNameDto>> FetchAllUsersName()
        {
            try
            {
                List<ActiveUsersNameDto> userRecords = new List<ActiveUsersNameDto>();
                // Query the database to fetch active users with Role 3 or 4
                var activeUsers = await _context.User
                    .Where(x => x.IsActive == (int)EnumActiveStatus.Active && (x.Role == 3 || x.Role == 4))
                    .ToListAsync();
                if (activeUsers.Any())
                {
                    // Map user name and user ID to ActiveUsersNameDto objects
                    userRecords = activeUsers.Select(user => new ActiveUsersNameDto
                    {
                        UserName = user.UserName, 
                        UserId = StringCipher.EncryptId(user.Id)
                    }).ToList();
                }
                return userRecords;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> UpdateUserAccountStatus(int id, EnumActiveStatus Status)
        {
            try
            {
                User? user = await GetUserById(id);
                
                if(user != null)
                {
                    user.IsActive = (int)Status;
                    if(EnumActiveStatus.Deleted.Equals(Status)) { 
                        user.DeletedAt = GeneralPurpose.DateTimeNow();
                    }
                    else
                    {
                        user.UpdatedAt = GeneralPurpose.DateTimeNow();
                    }
                    return await UpdateUser(user);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }      

        public async Task<bool> UpdateUserAccountActivityStatus(int id, int status)
        {
            try
            {
                User? user = await GetUserById(id);

                if (user != null)
                {
                    user.Status = status;
                    user.UpdatedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUser(user);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }
        
        public async Task<bool> UpdateUserAccountAvailabilityStatus(int id, int availability)
        {
            try
            {
                User? user = await GetUserById(id);

                if (user != null)
                {
                    user.Availability = availability;
                    user.UpdatedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateUser(user);
                }
                return false;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }
        
        public async Task<User> GetUserByEmail(string email)
        {
            try
            {
             return await _context.User.FirstOrDefaultAsync(x => x.Email.Trim().ToLower() == email.Trim().ToLower() && x.IsActive != (int)EnumActiveStatus.Deleted);

            }
            catch (Exception ex)
            {
             return null;
            }
        }
        
        public async Task<User?> GetUserById(int id)
        {
            return await _context.User.FindAsync(id);
        }
        
        public async Task<User> GetUserByLogin(string email, string password)
        {
            var getUser = await _context.User.FirstOrDefaultAsync(x => (x.Email!.ToLower() == email.Trim().ToLower() ||
            x.UserName!.ToLower() == email.Trim().ToLower()) && x.IsActive != (int)EnumActiveStatus.Deleted);
            var DecryptedPassword = "";
            if (getUser != null)
            {
                DecryptedPassword = StringCipher.Decrypt(getUser.Password!);
                if(DecryptedPassword == password)
                {
                    return getUser;
                }
            }
            return null;
        }
        
        public async Task<int> GetUserCount(int Role, EnumActiveStatus statuses)
        {
            return await _context.User.CountAsync(x => x.IsActive == (int)statuses && x.Role == Role);
        }
        
        public async Task<int> GetUserCountPendingVerifications(int Role)
        {
            return await _context.User.CountAsync(x => x.Role == Role && (x.IsActive == (int)EnumActiveStatus.EmailVerificationPending || x.IsActive == (int)EnumActiveStatus.AdminVerificationPending));
        }
        
        public async Task<bool> DeleteUser(int id)
        {
            try
            {
                User? user = await GetUserById(id);
                user.IsActive = 0;
                user.DeletedAt = GeneralPurpose.DateTimeNow();
                return await UpdateUser(user);
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }
        
        public async Task<List<User>> GetValetRecord()
        {
            return await _context.User
                .Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.Role == (int)EnumRoles.Valet)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<User>> GetUserList(int Role)
        {
            try
            {
                return await _context.User.Where(x => (x.IsActive == (int)EnumActiveStatus.Active || x.IsActive == (int)EnumActiveStatus.AdminVerificationPending || x.IsActive == (int)EnumActiveStatus.EmailVerificationPending) && x.Role == Role).OrderByDescending(x=>x.Id).ToListAsync();
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetOnlyActiveUserList(int Role)
        {
            try
            {
                return await _context.User.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.Role == Role && x.Status == 1 && x.Availability == 1 && x.StripeId != null).ToListAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetAccountOnHold(int Role)
        {
            return await _context.User.Where(x => x.Role == Role && (x.IsActive == (int)EnumActiveStatus.AccountOnHold)).OrderByDescending(x=>x.Id).ToListAsync();
        }
        
        public async Task<bool> UpdateUserForHold(int id)
        {
            var user = await _context.User.Where(x=>x.Id == id && x.Role == 4).FirstOrDefaultAsync();
            if (user != null)
            {
                user.IsActive = (int)EnumActiveStatus.AccountOnHold;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        
        public async Task<bool> UpdateUser(User user)
        {
            try
            {
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<bool> UpdateUserWithoutSavingInDatabase(User user)
        {
            try
            {
                _context.Entry(user).State =  EntityState.Modified;
                return true;
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }
        
        public async Task<bool> ValidateEmail(string email, int id = -1)
        {
            int emailCount = 0;

            if (id == -1)
            {
                emailCount = await _context.User.CountAsync(x => x.IsActive != (int)EnumActiveStatus.Deleted && x.Email!.ToLower() == email.ToLower().Trim());
            }
            else
            {
                emailCount = await _context.User.CountAsync(x => x.IsActive != (int)EnumActiveStatus.Deleted && x.Id != id && x.Email!.ToLower() == email.ToLower().Trim());
            }
            if (emailCount > 0)
            {
                return false;
            }
            return true;
        }
        
        public async Task<bool> ValidateUsername(string username, int id = -1)
        {
            int userCount = 0;

            if (id == -1)
            {
                userCount = await _context.User.CountAsync(x => x.IsActive != (int)EnumActiveStatus.Deleted && x.UserName!.ToLower() == username.ToLower().Trim());
            }
            else
            {
                userCount = await _context.User.CountAsync(x => x.IsActive != (int)EnumActiveStatus.Deleted && x.Id != id && x.UserName!.ToLower() == username.ToLower().Trim());
            }
            if (userCount > 0)
            {
                return false;
            }
            return true;
        }

        public async Task<IEnumerable<User>> GetUsersListByRequestSkills(string RequestSkills)
        {
            var skillsArray = RequestSkills.Split(',');

            var getUserSkills = await _context.UserSkill
                .Where(x => skillsArray.Contains(x.SkillName))
                .ToListAsync();

            var userIds = getUserSkills.Select(u => u.UserId).Distinct().ToList();
            var usersObj = await _context.User.Where(u => userIds.Contains(u.Id)).ToListAsync();

            // Filter users who are active, have both Stripe and PayPal accounts
            var usersWithBothAccounts = usersObj.Where(x =>
                x.IsActive == (int)EnumActiveStatus.Active &&
                !string.IsNullOrEmpty(x.StripeId) &&
                x.IsVerify_StripeAccount != 0 &&
                x.IsPayPalAccount == 1
            ).ToList();

            // Send email to users who do not have both Stripe and PayPal accounts asynchronously
            // We commented this part because it was taking too long. Now we will add notification work
            //await Task.Run(async () =>
            //{
            //    var usersWithoutBothAccounts = usersObj.Except(usersWithBothAccounts);
            //    foreach (var user in usersWithoutBothAccounts)
            //    {
            //        await MailSender.SendAlertForCompleteTheirProfile(user.Email, user.UserName);
            //    }
            //});

            return usersWithBothAccounts;
        }


        #region StripePayment Transfer

        public async Task<bool> TransferFunds(string destinationAccountId, decimal amount)
        {
            try
            {
                var calculatedAmount = (amount);
                var orderHstFee = GeneralPurpose.CalculateHSTFee(calculatedAmount);
                decimal earnedAmount = calculatedAmount - orderHstFee;
                var amountTransferToValet = earnedAmount;
                StripeConfiguration.ApiKey = GlobalMessages.StripeApiKey;
                // Perform the transfer to the connected account
                var TransferAmountToValet = amountTransferToValet * 100;
                var transferCreateOptions = new TransferCreateOptions
                {
                    Amount = (long)TransferAmountToValet, // Convert amount to cents
                    Currency = "USD",
                    Destination = destinationAccountId,
                };

                var transferService = new TransferService();
                var transfer = transferService.Create(transferCreateOptions);

                return true;

            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<List<CustomerInfo>> GetCustomerInfoRecord(List<int?> customerIds)
        {
            try
            {
                var customerRecord = await _context.User.Where(user => user.Role == 3 && customerIds.Contains(user.Id)).
                Select(customer => new CustomerInfo
                {
                    Id = customer.Id,
                    Name = customer.UserName,
                    ProfilePic = customer.ProfilePicture != null ? _projectVariables.BaseUrl + customer.ProfilePicture : null
                }).ToListAsync();

                return customerRecord;
            }
            catch (Exception ex)
            {
                return null;
            }   
        }
       
        public async Task<Dictionary<int, string>> GetUserNames(List<int> userIds)
        {
            var userNames = new Dictionary<int, string>();

            foreach (var userId in userIds)
            {
                userNames[userId] = await GetUserName(userId);
            }

            return userNames;
        }

        private async Task<string> GetUserName(int id)
        {
            try
            {
                var userName = await _context.User
                    .Where(x => x.Id == id)
                    .Select(x => x.UserName)
                    .FirstOrDefaultAsync();

                // Return the username if found
                return userName;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

        public async Task<bool> SaveChanges()
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<ValetAvailableSlots>> GetValetAvailableSlots(int valetId)
        {
            var availableSlots = await _context.UserAvailableSlot
                .Where(x => x.UserId == valetId && x.IsActive == 1).ToListAsync();

            List<ValetAvailableSlots> valetsAvailable = new List<ValetAvailableSlots>();

            if (availableSlots.Any())
            {
                foreach (var slot in availableSlots)
                {
                    var availableDate = slot.DateTimeOfDay.Value.Date;

                    if (slot.Slot1 == 1)
                    {
                        var slotStart = availableDate.AddHours(7);  // Morning slot start time (7 AM)
                        var slotEnd = availableDate.AddHours(12);    // Morning slot end time (12 PM)
                        valetsAvailable.Add(new ValetAvailableSlots
                        {
                            StartDateTime = slotStart,
                            EndDateTime = slotEnd
                        });
                    }
                    if (slot.Slot2 == 2)
                    {
                        var slotStart = availableDate.AddHours(12);  // Afternoon slot start time (12 PM)
                        var slotEnd = availableDate.AddHours(17);    // Afternoon slot end time (5 PM)
                        valetsAvailable.Add(new ValetAvailableSlots
                        {
                            StartDateTime = slotStart,
                            EndDateTime = slotEnd
                        });
                    }
                    if (slot.Slot3 == 3)
                    {
                        var slotStart = availableDate.AddHours(17);  // Evening slot start time (5 PM)
                        var slotEnd = availableDate.AddHours(22);    // Evening slot end time (10 PM)
                        valetsAvailable.Add(new ValetAvailableSlots
                        {
                            StartDateTime = slotStart,
                            EndDateTime = slotEnd
                        });
                    }
                    if (slot.Slot4 == 4)
                    {
                        var slotStart = availableDate.AddHours(0);  // Night slot start time (12 AM)
                        var slotEnd = availableDate.AddHours(7);    // Night slot end time (7 AM)
                        valetsAvailable.Add(new ValetAvailableSlots
                        {
                            StartDateTime = slotStart,
                            EndDateTime = slotEnd
                        });
                    }
                }
            }
            return valetsAvailable;
        }

        public async Task<bool> CheckValetAvailability(int valetId, string StartDate, string EndDate)
        {
            // Convert input date strings to DateTime
            if (!DateTime.TryParse(StartDate, out DateTime startDateTime) ||
                !DateTime.TryParse(EndDate, out DateTime endDateTime))
            {
                throw new ArgumentException("Invalid date format.");
            }

            // Get the current date
            DateTime currentDate = DateTime.Now.Date;

            // Query the UserAvailableSlot table for the valet's availability on the current date
            var availability = await _context.UserAvailableSlot
                .Where(slot => slot.UserId == valetId &&
                    slot.DateTimeOfDay == currentDate)
                .FirstOrDefaultAsync();

            if (availability != null)
            {
                // Check if the slots are within the given time range
                if (IsWithinTimeRange(availability, startDateTime, endDateTime))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWithinTimeRange(UserAvailableSlot slot, DateTime startTime, DateTime endTime)
        {
            // Define the time slots for the given start and end times
            var slot1StartTime = startTime.Date + TimeSpan.FromHours(7);
            var slot2StartTime = startTime.Date + TimeSpan.FromHours(12);
            var slot3StartTime = startTime.Date + TimeSpan.FromHours(17);
            var slot4StartTime = startTime.Date + TimeSpan.FromHours(0);

            var slot1EndTime = startTime.Date + TimeSpan.FromHours(12);
            var slot2EndTime = startTime.Date + TimeSpan.FromHours(17);
            var slot3EndTime = startTime.Date + TimeSpan.FromHours(22);
            var slot4EndTime = startTime.Date + TimeSpan.FromHours(7);

            return (slot.DateTimeOfDay >= startTime && slot.DateTimeOfDay <= endTime) &&
                   ((slot.DateTimeOfDay >= slot1StartTime && slot.DateTimeOfDay < slot1EndTime && slot.Slot1 == 1) ||
                    (slot.DateTimeOfDay >= slot2StartTime && slot.DateTimeOfDay < slot2EndTime && slot.Slot2 == 1) ||
                    (slot.DateTimeOfDay >= slot3StartTime && slot.DateTimeOfDay < slot3EndTime && slot.Slot3 == 1) ||
                    (slot.DateTimeOfDay >= slot4StartTime && slot.DateTimeOfDay < slot4EndTime && slot.Slot4 == 1));
        }

        public async Task<bool> IsDateRangeAvailable(int valetId, DateTime startDate, DateTime endDate)
        {
            var bookedSlots = await GetValetAvailableSlots(valetId);
            var startTimestamp = startDate.Ticks; 
            var endTimestamp = endDate.Ticks; 

            foreach (var slot in bookedSlots)
            {
                var slotStartTimestamp = slot.StartDateTime.Ticks; 
                var slotEndTimestamp = slot.EndDateTime.Ticks;

                if (startTimestamp >= slotStartTimestamp && endTimestamp <= slotEndTimestamp)
                {
                    return true;
                }
            }

            return false; 
        }


    }
}
