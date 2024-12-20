using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.Extensions.Options;
using Stripe;
using System.Net;

namespace ITValet.Utils.Helpers
{
    public static class StripeHelper
    {
        public static Order InitializeOrder(CheckOutDTO checkOutData)
        {
            return new Order
            {
                OrderTitle = checkOutData.PaymentTitle,
                OrderDescription = checkOutData.PaymentDescription,
                StartDateTime = DateTime.Parse(checkOutData.FromDateTime),
                EndDateTime = DateTime.Parse(checkOutData.ToDateTime),
                ValetId = int.Parse(checkOutData.ValetId),
                CustomerId = int.Parse(checkOutData.customerId),
                OfferId = checkOutData.OfferId,
                PackageId = checkOutData.PackageId,
                IsActive = 0,
                OrderStatus = 0,
                IsDelivered = 0,
                OrderPrice = 0,
                TotalAmountIncludedFee = 0,
                CreatedAt = GeneralPurpose.DateTimeNow()
            };
        }

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
                    throw new Exception(GlobalMessages.SystemFailureMessage);
                
                var cardPaymentsCapability = account.Capabilities?.CardPayments;

                response.Status = true;

                switch (cardPaymentsCapability)
                {
                    case "active":
                        response.Data = "Completed";
                        response.Message = "Stripe Account Verified";
                        break;

                    case "inactive":
                        response.Data = "Restricted";
                        response.Message = "Stripe Account Not Verified";
                        break;

                    default:
                        response.Data = "Unknown";
                        response.Message = "Card payments capability status is unknown.";
                        response.Status = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.StatusCode = "500";
                response.Message = $"An error occurred: {ex.Message}";
            }

            return response;
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
                RefreshUrl = $"{reactUrl}/verification-failed",
                ReturnUrl = $"{reactUrl}/account-verified",
                Type = "account_onboarding",
                Collect = "eventually_due",
            });
            return result.Url;
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
