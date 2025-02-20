using ITValet.HelpingClasses;
using ITValet.Models;
using Stripe;
using System.Net;

namespace ITValet.Utils.Helpers
{
    public static class StripeHelper
    {
        public static UserPackage InitializePackage(PackageCOutRequest checkOut, out string packagePrice)
        {
            packagePrice = checkOut.SelectedPackage switch
            {
                "IYear" => "100",
                "2Year" => "200",
                _ => throw new ArgumentException("Invalid package selection")
            };

            var startDate = DateTime.Now;
            var endDate = startDate.AddYears(checkOut.SelectedPackage == "IYear" ? 1 : 2);

            return new UserPackage
            {
                StartDateTime = startDate,
                EndDateTime = endDate,
                PackageType = checkOut.SelectedPackage == "IYear" ? 1 : 2,
                PackageName = checkOut.SelectedPackage,
                TotalSessions = checkOut.SelectedPackage == "IYear" ? 6 : 12,
                RemainingSessions = checkOut.SelectedPackage == "IYear" ? 6 : 12,
                CustomerId = checkOut.ClientId
            };
        }

        public static async Task<ResponseDto> StripeAccountStatus(string accountId)
        {
            var response = new ResponseDto();
            try
            {
                var accountService = new AccountService();
                var account = await accountService.GetAsync(accountId);

                if (account?.StripeResponse?.StatusCode != HttpStatusCode.OK)
                    return GeneralPurpose.GenerateResponseCode(false, "400", $"Could not find Stripe Account");

                var requiredActions = new List<string>();
                if (account.Requirements != null)
                {
                    // Collect all required fields that are currently due
                    if (account.Requirements.CurrentlyDue != null && account.Requirements.CurrentlyDue.Count > 0)
                    {
                        requiredActions.AddRange(account.Requirements.CurrentlyDue);
                    }

                    // Collect any past due fields
                    if (account.Requirements.Errors != null && account.Requirements.Errors.Count > 0)
                    {
                        foreach (var error in account.Requirements.Errors)
                        {
                            requiredActions.Add($"{error.Code}: {error.Reason}");
                        }
                    }
                }

                // Check if the account is restricted or missing required information
                if (!string.IsNullOrEmpty(account.Requirements?.DisabledReason) || requiredActions.Count() > 0)
                    return GeneralPurpose.GenerateResponseCode(false, "400", $"Stripe Account is restricted: {account.Requirements.DisabledReason}", "Resctricted");

                // Check payment and payout capabilities
                var cardPaymentsCapability = account.Capabilities?.CardPayments;
                var payoutCapability = account.Capabilities?.Transfers;

                if (cardPaymentsCapability == "active" && payoutCapability == "active")
                    return GeneralPurpose.GenerateResponseCode(true, "200", $"Stripe Account is fully verified and operational.", "Completed");
                else if (cardPaymentsCapability == "inactive" || payoutCapability == "inactive")
                    return GeneralPurpose.GenerateResponseCode(false, "400", $"Stripe Account is missing required verifications", "Resctricted");
                else
                    return GeneralPurpose.GenerateResponseCode(false, "400", $"Stripe Account could not be determind", "Unknown");
            }
            catch (Exception ex)
            {
                return GeneralPurpose.GenerateResponseCode(false, "500", $"An error occured: {ex.Message}");
            }
        }

        public static async Task<Account> CreateStripeAccountUS(string email, string reactUrl)
        {
            var options = new AccountCreateOptions
            {
                Type = "custom",
                Country = "US",
                Email = email,
                DefaultCurrency = "USD",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions
                    {
                        Requested = true,
                    },
                    Transfers = new AccountCapabilitiesTransfersOptions
                    {
                        Requested = true,
                    },
                },
                Settings = new AccountSettingsOptions
                {
                    Payouts = new AccountSettingsPayoutsOptions
                    {
                        Schedule = new AccountSettingsPayoutsScheduleOptions
                        {
                            Interval = "manual",
                        },
                    },
                },
                BusinessType = "individual",
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    Url = reactUrl,
                },
                Individual = new AccountIndividualOptions // Include Individual email
                {
                    Email = email,
                }
            };

            var service = new AccountService();
            return await service.CreateAsync(options);
        }

        public static async Task<Account> CreateStripeAccountCA(string email, string reactUrl)
        {
            var options = new AccountCreateOptions
            {
                Type = "custom",
                Country = "CA",
                Email = email,
                DefaultCurrency = "CAD",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions
                    {
                        Requested = true,
                    },
                    Transfers = new AccountCapabilitiesTransfersOptions
                    {
                        Requested = true,
                    },
                },
                Settings = new AccountSettingsOptions
                {
                    Payouts = new AccountSettingsPayoutsOptions
                    {
                        Schedule = new AccountSettingsPayoutsScheduleOptions
                        {
                            Interval = "daily",
                            DelayDays = 14,
                        },
                    },
                },
                BusinessType = "individual",
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    Url = reactUrl,
                },
                Individual = new AccountIndividualOptions // Include Individual email
                {
                    Email = email,
                }
            };

            var service = new AccountService();
            return await service.CreateAsync(options);
        }

        public static async Task<string> VerifyAccount(string stripeAccountId, string reactUrl)
        {
            var accountLinkService = new AccountLinkService();
            var result = accountLinkService.Create(new AccountLinkCreateOptions
            {
                Account = stripeAccountId,
                RefreshUrl = $"{reactUrl}account-verification/failed",
                ReturnUrl = $"{reactUrl}account-verification/success",
                Type = "account_onboarding",
                Collect = "eventually_due",
            });
            return result.Url;
        }

        public static async Task<StripeEarnings> GetStripeEarnings(string stripeId, ProjectVariables _projectVariables)
        {
            try
            {
                var requestOptions = new RequestOptions { StripeAccount = stripeId };
                var balanceService = new BalanceService();
                var balance = balanceService.Get(requestOptions);

                if (balance == null || balance.Available == null || balance.Pending == null)
                    return new StripeEarnings { BalancePending = 0, BalanceAvailable = 0 };

                // Convert cents to dollars using decimal division
                var balanceAvailable = balance.Available.Sum(b => (decimal)b.Amount) / 100m;
                var balancePending = balance.Pending.Sum(b => (decimal)b.Amount) / 100m;

                return new StripeEarnings
                {
                    BalanceAvailable = balanceAvailable,
                    BalancePending = balancePending,
                };
            }
            catch (StripeException ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return new StripeEarnings { BalancePending = 0, BalanceAvailable = 0 };
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return new StripeEarnings { BalancePending = 0, BalanceAvailable = 0 };
            }
        }
    }

    public class CreateCheckoutSessionResponse
    {
        public string? SessionId { get; set; }
        public string? CheckOutURL { get; set; }
        public string? PublicKey { get; set; }
        public string? PaymentTimeTicks { get; set; }
    }
}
