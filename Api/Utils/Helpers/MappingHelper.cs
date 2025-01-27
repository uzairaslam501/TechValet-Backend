using ITValet.HelpingClasses;
using ITValet.Models;
using ITValet.Services;

namespace ITValet.Utils.Helpers
{
    public static class MappingHelper
    {
        public static List<OrderDtoList> MapOrdersToDtos(IEnumerable<Order> orders)
        {
            var orderDtos = new List<OrderDtoList>();

            foreach (var order in orders)
            {
                var orderDto = new OrderDtoList
                {
                    Id = order.Id.ToString(),
                    EncId = StringCipher.EncryptId(order.Id),
                    OrderTitle = order.OrderTitle,
                    StartDateTime = order.StartDateTime?.ToString() ?? "",
                    EndDateTime = order.EndDateTime?.ToString() ?? "",
                    OrderPrice = order.OrderPrice.ToString(),
                    IsDelivered = order.IsDelivered.ToString()
                };

                if (order.OrderReason != null && order.OrderReason.Any())
                {
                    var reason = order.OrderReason.FirstOrDefault();
                    if (reason != null)
                    {
                        orderDto.OrderReasonId = reason.Id.ToString();
                        orderDto.OrderReasonExplanation = reason.ReasonExplanation;
                        orderDto.OrderReasonType = reason.ReasonType.ToString();
                        orderDto.OrderReasonIsActive = reason.IsActive.ToString();
                    }
                }

                orderDtos.Add(orderDto);
            }

            return orderDtos;
        }
        
        public static List<RequestServicesDto> MapRequestServiceToDtos(IEnumerable<RequestService> requestServices, UserClaims? getUserFromToken)
        {
            List<RequestServicesDto> requestServicesDtos = requestServices.Select(rs =>
            {
                string userRegionRequestedTime = GeneralPurpose.regionChanged(Convert.ToDateTime(rs.CreatedAt), getUserFromToken.Timezone);
                string userRegionRequestStartTime = rs.FromDateTime.HasValue
                    ? GeneralPurpose.regionChanged(Convert.ToDateTime(rs.FromDateTime), getUserFromToken.Timezone)
                    : "";
                string userRegionRequestEndTime = rs.ToDateTime.HasValue
                    ? GeneralPurpose.regionChanged(Convert.ToDateTime(rs.ToDateTime), getUserFromToken.Timezone)
                    : "";

                return new RequestServicesDto
                {
                    Id = rs.Id,
                    EncId = StringCipher.EncryptId(rs.Id),
                    PrefferedServiceTime = rs.PrefferedServiceTime,
                    CategoriesOfProblems = rs.CategoriesOfProblems,
                    ServiceDescription = rs.ServiceDescription,
                    FromDateTime = rs.FromDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    ToDateTime = rs.ToDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AppointmentTime = $"{userRegionRequestStartTime} - {userRegionRequestEndTime}",
                    ServiceLanguage = rs.ServiceLanguage,
                    RequestServiceType = rs.RequestServiceType.ToString(),
                    RequestServiceSkills = rs.RequestServiceSkills,
                    CreatedAt = userRegionRequestedTime
                };
            }).ToList();

            return requestServicesDtos;
        }

        public static UserPackageListViewModel MapUserPackageToDtos(UserPackage packageObj)
        {
            return new UserPackageListViewModel
            {
                Id = packageObj.Id,
                PackageName = packageObj.PackageName,
                PackageType = packageObj.PackageType.ToString(),
                TotalSessions = packageObj.TotalSessions.ToString(),
                RemainingSessions = packageObj.RemainingSessions.ToString(),
                StartDateTime = packageObj.StartDateTime!.Value.ToString("yyyy-MMM-dd HH:mm"),
                EndDateTime = packageObj.EndDateTime!.Value.ToString("yyyy-MMM-dd HH:mm"),
                CustomerId = packageObj.CustomerId.ToString(),
            };
        }

        public static List<OrderDtoList> MapOrders_ByPackageId_ToDtos(IEnumerable<Order> orders)
        {
            var orderDtos = new List<OrderDtoList>();
            foreach (var order in orders)
            {
                var orderDto = new OrderDtoList
                {
                    Id = order.Id.ToString(),
                    EncId = StringCipher.EncryptId(order.Id),
                    OrderTitle = order.OrderTitle,
                    StartDateTime = order.StartDateTime?.ToString() ?? "",
                    EndDateTime = order.EndDateTime?.ToString() ?? "",
                    OrderPrice = order.OrderPrice.ToString(),
                    IsDelivered = order.IsDelivered.ToString(),
                };

                if (order.OrderReason != null && order.OrderReason.Count > 0)
                {
                    foreach (var reason in order.OrderReason)
                    {
                        orderDto.OrderReasonId = reason.Id.ToString();
                        orderDto.OrderReasonExplanation = reason.ReasonExplanation;
                        orderDto.OrderReasonType = reason.ReasonType.ToString();
                        orderDto.OrderReasonIsActive = reason.IsActive.ToString();
                    }
                }

                orderDtos.Add(orderDto);
            }

            return orderDtos;
        }

        public static List<UserListDto> MapUsersToDtos(IEnumerable<User> users)
        {

            var usersListDto = users.Select(u => new UserListDto
            {
                Id = u.Id,
                UserEncId = StringCipher.EncryptId(u.Id),
                FirstName = u.FirstName,
                LastName = u.LastName,
                UserName = u.UserName,
                Contact = u.Contact,
                Email = u.Email,
                Gender = u.Gender,
                ProfilePicture = u.ProfilePicture,
                Country = u.Country,
                State = u.State,
                City = u.City,
                Timezone = u.Timezone,
                Availability = u.Availability.ToString(),
                Status = u.Status.ToString(),
                BirthDate = u.BirthDate.ToString(),
                Role = Enum.GetName(typeof(EnumRoles), u.Role),
                IsActive = Enum.GetName(typeof(EnumActiveStatus), u.IsActive)
            }).ToList();

            return usersListDto;
        }

        public static List<PayPalOrderDetailsForAdminDB> MapPaypalOrdersToDtos(IEnumerable<PayPalOrderDetailsForAdminDB> paypalOrdersRecord)
        {
            var paypalRecordDto = paypalOrdersRecord.Select(orderObj => new PayPalOrderDetailsForAdminDB
            {
                Id = orderObj.Id,
                ITValet = orderObj.ITValet,
                OrderTitle = orderObj.OrderTitle,
                OrderEncId = orderObj.OrderEncId,
                OrderPrice = orderObj.OrderPrice,
                OrderStatus = orderObj.OrderStatus,
                PaymentStatus = orderObj.PaymentStatus,
                CustomerName = orderObj.CustomerName,
                CaptureId = orderObj.CaptureId,
                PaidByPackage = orderObj.PaidByPackage,
            }).ToList();
            return paypalRecordDto;
        }

        public static List<PayPalUnclaimedTransactionDetailsForAdminDB> MapPaypalUnclaimedPaymentRecordToDtos(IEnumerable<PayPalUnclaimedTransactionDetailsForAdminDB> unclaimedPaymentRecord)
        {
            var paypalRecordDto = unclaimedPaymentRecord.Select(unclaimedObj => new PayPalUnclaimedTransactionDetailsForAdminDB
            {
                ITValetName = unclaimedObj.ITValetName,
                OrderTitle = unclaimedObj.OrderTitle,
                Reason = unclaimedObj.Reason,
                TransactionStatus = unclaimedObj.TransactionStatus,
                UnclaimedAmountStatus = unclaimedObj.UnclaimedAmountStatus,
                CustomerName = unclaimedObj.CustomerName,
                OrderEncId = unclaimedObj.OrderEncId,
                PayPalEmailAccount = unclaimedObj.PayPalEmailAccount,
            }).ToList();
            return paypalRecordDto;
        }

        public static List<PayPalTransactionDetailsForAdminDB> MapPaypalTransactionRecordsToDtos(IEnumerable<PayPalTransactionDetailsForAdminDB> paypalTransactionRecords)
        {
            var paypalRecordDto = paypalTransactionRecords.Select(transactionObj => new PayPalTransactionDetailsForAdminDB
            {
                ITValetName = transactionObj.ITValetName,
                OrderTitle = transactionObj.OrderTitle,
                OrderPrice = transactionObj.OrderPrice,
                TransactionStatus = transactionObj.TransactionStatus,
                PlatformFee = transactionObj.PlatformFee,
                CustomerName = transactionObj.CustomerName,
                OrderEncId = transactionObj.OrderEncId,
                PayOutItemId = transactionObj.PayOutItemId,
                SentAmount = transactionObj.SentAmount,
                PayPalEmailAccount = transactionObj.PayPalEmailAccount,
                ExpectedDateToTransmitPayment = transactionObj.ExpectedDateToTransmitPayment,
            }).ToList();
            return paypalRecordDto;
        }

        public static List<StripeOrderDetailForAdminDb> MapStripeOrderRecordsToDtos(IEnumerable<StripeOrderDetailForAdminDb> stripeOrdersRecords)
        {
            var stripeRecordDto = stripeOrdersRecords.Select(orderObj => new StripeOrderDetailForAdminDb
            {
                Id = orderObj.Id,
                ITValet = orderObj.ITValet,
                OrderTitle = orderObj.OrderTitle,
                OrderEncId = orderObj.OrderEncId,
                OrderPrice = orderObj.OrderPrice,
                OrderStatus = orderObj.OrderStatus,
                PaymentStatus = orderObj.PaymentStatus,
                CustomerName = orderObj.CustomerName,
                StripeStatus = orderObj.StripeStatus,
                StripeId = orderObj.StripeId,
                IsDelivered = orderObj.IsDelivered,
                PaidByPackage = orderObj.PaidByPackage,
            }).ToList();
            return stripeRecordDto;
        }

        public static List<UserEducationDto> MapUserEducationRecordsToDtos(IEnumerable<UserEducation> userEducations)
        {
            var educationDto = userEducations.Select(educationList => new UserEducationDto
            {
                Id = educationList.Id,
                UserEducationEncId = StringCipher.EncryptId(educationList.Id),
                DegreeName = educationList.DegreeName,
                InstituteName = educationList.DegreeName,
                StartDate = educationList.StartDate.ToString(),
                EndDate = educationList.EndDate.ToString(),
                UserId = educationList.UserId
            }).ToList();
            return educationDto;
        }

        public static List<UserExperienceDto> MapUserExperienceRecordsToDtos(IEnumerable<UserExperience> userExperiences)
        {
            var experienceDto = userExperiences.Select(experienceList => new UserExperienceDto
            {
                Id = experienceList.Id,
                UserExperienceEncId = StringCipher.EncryptId(experienceList.Id),
                Title = experienceList.Title,
                Description = experienceList.Description,
                ExperienceFrom = experienceList.ExperienceFrom.ToString(),
                ExperienceTo = experienceList.ExperienceTo.ToString(),
                Organization = experienceList.Organization,
                Website = experienceList.Website,
                UserId = experienceList.UserId
            }).ToList();
            return experienceDto;
        }

        public static List<UserSocialProfileDto> MapUserSocialProfileRecordsToDtos(IEnumerable<UserSocialProfile> userSocialProfiles)
        {
            var socialProfileDto = userSocialProfiles.Select(socialProfileList => new UserSocialProfileDto
            {
                Id = socialProfileList.Id,
                UserSocialProfileEncId = StringCipher.EncryptId(socialProfileList.Id),
                Title = socialProfileList.Title,
                Link = socialProfileList.Link,
                UserId = socialProfileList.UserId
            }).ToList();
            return socialProfileDto;
        }

        public static List<UserSkillDto> MapUserSkillRecordsToDtos(IEnumerable<UserSkill> userSkills)
        {
            var userSkillDto = userSkills.Select(skillList => new UserSkillDto
            {
                Id = skillList.Id,
                UserSkillEncId = StringCipher.EncryptId(skillList.Id),
                SkillName = skillList.SkillName,
                UserId = skillList.UserId
            }).ToList();
            return userSkillDto;
        }

        public static List<UserTagDto> MapUserTagRecordsToDtos(IEnumerable<UserTag> userTags)
        {
            var tagListDto = userTags.Select(userTagList => new UserTagDto
            {
                Id = userTagList.Id,
                UserTagEncId = StringCipher.EncryptId(userTagList.Id),
                TagName = userTagList.TagName,
                UserId = userTagList.UserId
            }).ToList();
            return tagListDto;
        }

        public static List<CompletedOrderRecord> MapCompletedOrderRecordsToDtos(List<Order> completedOrders)
        {
            List<CompletedOrderRecord> completedOrdersDto = new List<CompletedOrderRecord>();
            foreach (var order in completedOrders)
            {
                CompletedOrderRecord obj = new CompletedOrderRecord()
                {
                    EncOrderId = StringCipher.EncryptId(order.Id),
                    OrderPrice = order.OrderPrice.ToString(),
                    OrderPaidBy = GeneralPurpose.OrderPaidBy(order.PayPalPaymentId, order.CapturedId, order.StripeChargeId, order.PackageBuyFrom),
                    EarnedFromOrder = "$"+ GeneralPurpose.EarnedAmountFromOrder((decimal)order.OrderPrice!),
                    OrderTitle = order.OrderTitle,
                    CompletedAt = order.EndDateTime!.Value.ToString("yyyy-MMM-dd HH:mm"),
                };
                completedOrdersDto.Add(obj);
            }
            return completedOrdersDto;
        }

        public static List<User> FilterUsersList(List<User> ulist, string? Name = "",
            string? Email = "", string? Contact = "", string? Country = "",
            string? State = "", string? City = "", string? IsActive = "")
        {
            if (!string.IsNullOrEmpty(Name))
            {
                ulist = ulist.Where(x => x.FirstName.ToLower().Contains(Name.ToLower()) || x.LastName.ToLower().Contains(Name.ToLower()) || x.UserName.ToLower().Contains(Name.ToLower())).ToList();
            }

            if (!string.IsNullOrEmpty(IsActive))
            {
                ulist = ulist.Where(x => x.IsActive == Convert.ToInt16(IsActive)).ToList();
            }

            if (!string.IsNullOrEmpty(Email))
            {
                ulist = ulist.Where(x => x.Email.ToLower().Contains(Email.ToLower())).ToList();
            }

            if (!string.IsNullOrEmpty(Contact))
            {
                ulist = ulist.Where(x => x.Contact.ToLower().Contains(Contact.ToLower())).ToList();
            }

            if (!string.IsNullOrEmpty(Country))
            {
                ulist = ulist.Where(x => x.Country.ToLower().Contains(Country.ToLower())).ToList();
            }

            if (!string.IsNullOrEmpty(State))
            {
                ulist = ulist.Where(x => x.State.ToLower().Contains(State.ToLower())).ToList();
            }

            if (!string.IsNullOrEmpty(City))
            {
                ulist = ulist.Where(x => x.City.ToLower().Contains(City.ToLower())).ToList();
            }
            return ulist;
        }

    }
}
