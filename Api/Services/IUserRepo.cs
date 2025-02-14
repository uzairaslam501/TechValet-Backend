using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace ITValet.Services
{
    public interface IUserRepo
    {
        #region Refactor
        Task<User?> GetUserByLogin(string email, string password);
        Task<User?> GetUserInfoByNameOrEmail(string username);
        Task<IEnumerable<User>> GetUserList(int Role);
        Task<User?> GetUserRecordById(string userId);
        Task<ResponseDto> GetCountForAllUsers();

        Task<bool> AddUser(User user);
        Task<bool> UpdateUser(User user);
        Task<bool> DeleteUser(string userId);
        Task<bool> UpdateUserForHold(int id);
        Task<bool> SaveChanges();


        #endregion

        Task<User?> GetUserById(int id);
        Task<IEnumerable<User>> GetOnlyActiveUserList(int Role);
        
        Task<bool> UpdateUserAccountActivityStatus(int id, int status);
        Task<bool> UpdateUserAccountAvailabilityStatus(int id, int availability);
        Task<bool> ValidateEmail(string email, int id = -1);
        Task<bool> ValidateUsername(string username, int id = -1);
        
        Task<Dictionary<int, string>> GetUserNames(List<int> userIds);
        Task<IEnumerable<User>> GetUsersListByRequestSkills(string RequestSkills);


        Task<bool> TransferFunds(string destinationAccountId, decimal amount);
        Task<List<User>> GetValetRecord();
        Task<List<User?>> GetSkilledUsersByIds(List<int?> userIds);
        Task<List<User?>> GetUsersByName(string userName);
        
        Task<List<CustomerInfo>> GetCustomerInfoRecord(List<int?> customerIds);


        #region New
        Task<User> HandleUpdateProfileIsActive(string userId);
        #endregion

    }

    public class UserRepo : IUserRepo
    {
        private readonly AppDbContext _context;
        private readonly ProjectVariables _projectVariables;
        private readonly StripeApiKeys _stripeKeys;
        private readonly IUserSkillRepo _userSkillRepo;

        public UserRepo(AppDbContext _appDbContext, IOptions<ProjectVariables> projectVariable, IOptions<StripeApiKeys> stripeKeys,
            IUserSkillRepo userSkillRepo)
        {
            _context = _appDbContext;
            _projectVariables = projectVariable.Value;
            _stripeKeys = stripeKeys.Value;
            _userSkillRepo = userSkillRepo;
        }

        public async Task<List<User?>> GetSkilledUsersByIds(List<int?> userIds)
        {
            try
            {
                List<User?> users = await _context.User
                    .Where(u => userIds.Contains(u.Id) && u.Role != 1)
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
       
        public async Task<User?> GetUserById(int id)
        {
            return await _context.User.FindAsync(id);
        }
        
        

     
        
        
        public async Task<List<User>> GetValetRecord()
        {
            return await _context.User
                .Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.Role == (int)EnumRoles.Valet)
                .ToListAsync();
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
                StripeConfiguration.ApiKey = _stripeKeys.StripeApiKey;
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

        public async Task<User> HandleUpdateProfileIsActive(string userId)
        {
            var decrypt = StringCipher.DecryptionId(userId);
            var userObj = await GetUserById(decrypt);
            if (userObj == null)
                return null;

            var isCompleteValetAccount = userObj.Role == (int)EnumRoles.Valet
                ? await GeneralPurpose.CheckValuesNotEmpty(userObj, _userSkillRepo)
                : 0;

            if (isCompleteValetAccount == 1)
                userObj.IsActive = (int)EnumActiveStatus.Active;

            userObj.IsActive = 1;

            return userObj;
        }

        #region Refactor
        public async Task<User?> GetUserByLogin(string email, string password)
        {
            try
            {
                var userObj = await _context.User.FirstOrDefaultAsync(x => 
                                (x.Email!.ToLower() == email.Trim().ToLower() ||
                                x.UserName!.ToLower() == email.Trim().ToLower()) &&
                                x.IsActive != (int)EnumActiveStatus.Deleted);
                
                if (userObj == null)
                    return null;

                if(!StringCipher.ComparePassword(password, userObj.Password!))
                    return null;
                else
                    return userObj;
                
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return null;
            }
        }

        public async Task<User?> GetUserRecordById(string userId)
        {
            try
            {
                var decryptUserId = StringCipher.DecryptionId(userId);
                return await _context.User.FindAsync(decryptUserId);
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return null;
            }
        }

        public async Task<User?> GetUserInfoByNameOrEmail(string username)
        {
            try
            {
                var getUser = await _context.User.FirstOrDefaultAsync(x => x.IsActive != (int)EnumActiveStatus.Deleted &&
                                (x.Email!.ToLower() == username.Trim().ToLower() ||
                                    x.UserName!.ToLower() == username.Trim().ToLower()));
                return getUser;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetUserList(int Role)
        {
            try
            {
                return await _context.User.Where(x => x.IsActive != (int)EnumActiveStatus.Deleted &&
                                                       x.Role == Role).OrderByDescending(x => x.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return null;
            }
        }

        public async Task<ResponseDto> GetCountForAllUsers()
        {
            try
            {
                var getUsers = await _context.User.Where(x => x.IsActive != (int)EnumActiveStatus.Deleted).ToListAsync();

                var totalSeoUsers = getUsers.Where(x => x.Role == (int)EnumRoles.Seo);
                var totalValets = getUsers.Where(x => x.Role == (int)EnumRoles.Valet);
                var totalCustomers = getUsers.Where(x => x.Role == (int)EnumRoles.Customer);

                var activeSeo = totalSeoUsers.Count(x => x.IsActive == (int)EnumActiveStatus.Active);
                var activeValet = totalValets.Count(x => x.IsActive == (int)EnumActiveStatus.Active);
                var activeCustomer = totalCustomers.Count(x => x.IsActive == (int)EnumActiveStatus.Active);

                var valetsUnderReview = totalValets.Count(x => x.IsActive == (int)EnumActiveStatus.AccountOnHold);
                var customersUnderReview = totalCustomers.Count(x => x.IsActive == (int)EnumActiveStatus.AccountOnHold);


                var valetsPendingForVerification = totalValets.Count(x =>
                                                    x.IsActive == (int)EnumActiveStatus.EmailVerificationPending ||
                                                    x.IsActive == (int)EnumActiveStatus.AdminVerificationPending
                                                    );
                var customersPendingForVerification = totalCustomers.Count(x =>
                                                    x.IsActive == (int)EnumActiveStatus.EmailVerificationPending ||
                                                    x.IsActive == (int)EnumActiveStatus.AdminVerificationPending
                                                    );

                var response = new
                {
                    Valet = activeValet,
                    Customer = activeCustomer,
                    ValetUnderReview = valetsUnderReview,
                    CustomersUnderReview = customersUnderReview,
                    ValetVerificationPending = valetsPendingForVerification,
                    CustomersVerificationPending = customersPendingForVerification,
                };

                return GeneralPurpose.GenerateResponseCode(true, "200", "", response);
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound);
            }

        }

        public async Task<bool> AddUser(User user)
        {
            try
            {
                _context.User.Add(user);
                return await SaveChanges();
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        public async Task<bool> UpdateUser(User user)
        {
            try
            {
                _context.Entry(user).State = EntityState.Modified;
                return await SaveChanges();
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        public async Task<bool> DeleteUser(string userId)
        {
            try
            {
                var user = await GetUserRecordById(userId);
                if(user == null)
                    return false;

                user.IsActive = 0;
                user.DeletedAt = GeneralPurpose.DateTimeNow();

                return await UpdateUser(user);
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        public async Task<bool> UpdateUserForHold(int id)
        {
            try
            {
                var user = await _context.User.Where(x => x.Id == id && x.Role == (int)EnumRoles.Valet).FirstOrDefaultAsync();
                if (user != null)
                {
                    user.IsActive = (int)EnumActiveStatus.AccountOnHold;
                    return await SaveChanges();

                }
                return false;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        public async Task<bool> SaveChanges()
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return false;
            }
        }

        #endregion
    }
}
