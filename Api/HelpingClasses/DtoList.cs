using ITValet.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace ITValet.HelpingClasses
{
    public class LoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class PostAddUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? BirthDate { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Timezone { get; set; }
        public string? Availability { get; set; }
        public string? Status { get; set; }
        public string? Gender { get; set; }
        public string? Language { get; set; }
        public string? StripeId { get; set; }
        public string? PricePerHour { get; set; }
        public string? SignUpOption { get; set; }
    }

    public class PostUpdateUserDto
    {
        public int? Id { get; set; }
        public string? UserEncId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? BirthDate { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Timezone { get; set; }
        public string? Availability { get; set; }
        public string? Status { get; set; }
        public string? Language { get; set; }
        public string? Gender { get; set; }
        public string? PricePerHour { get; set; }
        public string? Role { get; set; }
        public string? AvailabilityType { get; set; }
        public string? SelectedDays { get; set; }
        public string? AvailableTimeSlots { get; set; }
        public string? Description { get; set; }
    }

    public class UserListDto
    {
        public int? Id { get; set; }
        public string? UserEncId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? BirthDate { get; set; }
        public string? ProfilePicture { get; set; }
        public string? IsCompleteValetAccount { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Timezone { get; set; }
        public string? Availability { get; set; }
        public string? Status { get; set; }
        public string? Language { get; set; }
        public string? Gender { get; set; }
        public string? StripeId { get; set; }
        public string? PricePerHour { get; set; }
        public string? IsActive { get; set; }
        public string? AvailabilitySlots { get; set; }
        public string? Description { get; set; }
        public string? CurrentTime { get; set; }
        public string? UserRating { get; set; }
        public string? UserRatingCount { get; set; }
        public int? IsVerify_StripeAccount { get; set; }
        public int? IsBankAccountAdded { get; set; }
        public int? HST { get; set; }
        public string? AverageRating { get; set; }
        public int? StarsCount { get; set; }
        public List<UserEducationViewModel>? UserEducations { get; set;}
        public List<UserExperiencedViewModel>? UserExperienced { get; set; }
    }

    public class UserEducationViewModel
    {
        public string? DegreeName { get; set; }
        public string? InstituteName { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }

    public class UserExperiencedViewModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ExperienceFrom { get; set; }
        public string? ExperienceTo { get; set; }
        public string? Organization { get; set; }
        public string? WebSite { get; set; }
    }

    public class UserPackageListDto
    {
        public int? Id { get; set; }
        public string? UserPackageEncId { get; set; }
        public string? PackageName { get; set; }
        public string? Customer { get; set; }
        public int? PackageType { get; set; } // 1 For Yearly 2 For 2 Years
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int? TotalSessions { get; set; }
        public int? RemainingSessions { get; set; }
        public int? CustomerId { get; set; }
    }

    public class CustomerInfo
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ProfilePic { get; set; }
    }
    public class ValetRatingRecord
    {
        public string? Reviews { get; set; }
        public int? Stars { get; set; }
        public string? PublishDate { get; set; }   
        public CustomerInfo? Customer { get; set; }
    }    
    

    public class ValetRatingReviewRecord
    {
        public string? AverageStars { get; set; }
        public List<ValetRatingRecord>? Rating { get; set; }
    }

    public class UserPackageDto
    {
        public int Id { get; set; }
        public string? PackageName { get; set; }
        public int? PackageType { get; set; } // 1 For Yearly 2 For 2 Years
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string? PackagePaidBy { get; set; }
        public int? TotalSessions { get; set; }
        public int? RemainingSessions { get; set; }
        public int? CustomerId { get; set; }
    }

    public class ReceiveOrderMessageDto
    {
        public int? senderId { get; set; }
        public int? receiverId { get; set; }
        public string? userName { get; set; } = "";
        public string? userProfile { get; set; } = "";
        public string? message { get; set; } = "";
        public string? messageTime { get; set; } = "";
        public string? newOrderReasonId { get; set; } = "";
        public string? filePath { get; set; } = "";
        public string? IsDelivery { get; set; } = "";
        public string? reasonType { get; set; } = "";
        public string? JoinUrl { get; set; } = "";
        public string? StartUrl { get; set; } = "";
    }
    public class ZoomMeetingResponse
    {
        public string start_url { get; set; }
        public string join_url { get; set; }
    }

    public class UserClaims
    {
        public int? Id { get; set; }
        public string? UserEncId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? Role { get; set; }
        public string? BirthDate { get; set; }
        public string? IsCompleteValetAccount { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Timezone { get; set; }
        public int? Availability { get; set; }
        public string? Status { get; set; }
        public string? Language { get; set; }
        public string? Gender { get; set; }
        public string? StripeId { get; set; }
        public string? PricePerHour { get; set; }
        public string? Token { get; set; }
        public string? IsActive { get; set; }
    }

    public class ResponseDto
    {
        public string? Id { get; set; }
        public dynamic? Data { get; set; }
        public bool? Status { get; set; }
        public string? StatusCode { get; set; }
        public string? Message { get; set; }
        public string? OtherMessage1 { get; set; }
        public string? OtherMessage2 { get; set; }
        public string? OtherMessage3 { get; set; }
    }

    public class UpdatePasswordDto
    {
        public string? OldPassword { get; set; }
        public string? Password { get; set; }
    }

    public class BookedSlotTiming
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class ResetPasswordDto
    {
        public string? UserId { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class ForgotPasswordDto
    {
        public string? Email { get; set; }
    }

    public class SearchedUserList
    {
        public string? UserProfile { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? Status { get; set; }
        public string? UserDescription { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? EncUserId { get; set; }
        public decimal? PricePerHours { get; set; }
        public string? AverageStars { get; set; }
    }

    #region Education
    public class PostAddUserEducation
    {
        public string? DegreeName { get; set; }
        public string? InstituteName { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateUserEducation
    {
        public int? Id { get; set; }
        public string? UserEducationEncId { get; set; }
        public string? DegreeName { get; set; }
        public string? InstituteName { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? UserId { get; set; }
    }

    public class UserEducationDto
    {
        public int? Id { get; set; }
        public string? UserEducationEncId { get; set; }
        public string? DegreeName { get; set; }
        public string? InstituteName { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region Experience
    public class PostAddUserExperience
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ExperienceFrom { get; set; }
        public string? ExperienceTo { get; set; }
        public string? Organization { get; set; }
        public string? Website { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateUserExperience
    {
        public int? Id { get; set; }
        public string? UserExperienceEncId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ExperienceFrom { get; set; }
        public string? ExperienceTo { get; set; }
        public string? Organization { get; set; }
        public string? Website { get; set; }
        public int? UserId { get; set; }
    }

    public class UserExperienceDto
    {
        public int? Id { get; set; }
        public string? UserExperienceEncId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ExperienceFrom { get; set; }
        public string? ExperienceTo { get; set; }
        public string? Organization { get; set; }
        public string? Website { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region SocialProfile
    public class PostAddUserSocialProfile
    {
        public string? Title { get; set; }
        public string? Link { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateUserSocialProfile
    {
        public int? Id { get; set; }
        public string? UserSocialProfileEncId { get; set; }
        public string? Title { get; set; }
        public string? Link { get; set; }
        public int? UserId { get; set; }
    }

    public class UserSocialProfileDto
    {
        public int? Id { get; set; }
        public string? UserSocialProfileEncId { get; set; }
        public string? Title { get; set; }
        public string? Link { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region Skill
    public class PostAddUserSkill
    {
        public string[]? SkillName { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateUserSkill
    {
        public int? Id { get; set; }
        public string? UserSkillEncId { get; set; }
        public string? SkillName { get; set; }
        public int? UserId { get; set; }
    }

    public class UserSkillDto
    {
        public int? Id { get; set; }
        public string? UserSkillEncId { get; set; }
        public string? SkillName { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region Tag
    public class PostAddUserTag
    {
        public string? TagName { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateUserTag
    {
        public int? Id { get; set; }
        public string? UserTagEncId { get; set; }
        public string? TagName { get; set; }
        public int? UserId { get; set; }
    }

    public class UserTagDto
    {
        public int? Id { get; set; }
        public string? UserTagEncId { get; set; }
        public string? TagName { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region AvailableSlot
    public class PostAddUserAvailableSlot
    {
        public string? DateTimeOfDay { get; set; }
        public string? Slot1 { get; set; }
        public string? Slot2 { get; set; }
        public string? Slot3 { get; set; }
        public string? Slot4 { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateUserAvailableSlot
    {
        public int? Id { get; set; }
        public string? UserAvailableSlotEncId { get; set; }
        public string? DateTimeOfDay { get; set; }
        public string? Slot1 { get; set; }
        public string? Slot2 { get; set; }
        public string? Slot3 { get; set; }
        public string? Slot4 { get; set; }
        public int? UserId { get; set; }
    }

    public class UserAvailableSlotDto
    {
        public int? Id { get; set; }
        public string? UserAvailableSlotEncId { get; set; }
        public string? DateTimeOfDay { get; set; }
        public string? DayName { get; set; }
        public string? Slot1 { get; set; }
        public string? Slot2 { get; set; }
        public string? Slot3 { get; set; }
        public string? Slot4 { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region SearchLogs
    public class PostAddSearchLog
    {
        public string? SearchKeyword { get; set; }
        public string? Slot1 { get; set; }
        public string? Slot2 { get; set; }
        public string? Slot3 { get; set; }
        public string? Slot4 { get; set; }
        public int? UserId { get; set; }
    }

    public class PostUpdateSearchLog
    {
        public int? Id { get; set; }
        public string? SearchLogEncId { get; set; }
        public string? DateTimeOfDay { get; set; }
        public string? Slot1 { get; set; }
        public string? Slot2 { get; set; }
        public string? Slot3 { get; set; }
        public string? Slot4 { get; set; }
        public int? UserId { get; set; }
    }

    public class SearchLogDto
    {
        public int? Id { get; set; }
        public string? SearchLogEncId { get; set; }
        public string? DateTimeOfDay { get; set; }
        public string? Slot1 { get; set; }
        public string? Slot2 { get; set; }
        public string? Slot3 { get; set; }
        public string? Slot4 { get; set; }
        public int? UserId { get; set; }
    }
    #endregion

    #region Contact
    public class PostAddContact
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }

    public class ContactDto
    {
        public int? Id { get; set; }
        public string? UserContactEncId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }
    #endregion

    #region RequestServices
    public class PostAddRequestServices
    {
        public string? ServiceTitle { get; set; }
        public string? PrefferedServiceTime { get; set; }
        public string? CategoriesOfProblems { get; set; }
        public string? ServiceDescription { get; set; }
        public string? FromDateTime { get; set; }
        public string? ToDateTime { get; set; }
        public string? RequestServiceSkills { get; set; }
        public string? ServiceLanguage { get; set; } // It can be multiple by , seperated
        public string? RequestedServiceUserId { get; set; }
        public string? RequestServiceType { get; set; } // 1 Live Now 2 Schedule Later
    }

    public class PostUpdateRequestService
    {
        public string? Id { get; set; }
        public string? RequestServiceEncId { get; set; }
        public string? ServiceTitle { get; set; }
        public string? PrefferedServiceTime { get; set; }
        public string? CategoriesOfProblems { get; set; }
        public string? ServiceDescription { get; set; }
        public string? FromDateTime { get; set; }
        public string? ToDateTime { get; set; }
        public string? RequestServiceSkills { get; set; }
        public string? ServiceLanguage { get; set; } // It can be multiple by , seperated
        public string? RequestedServiceUserId { get; set; }
        public string? RequestServiceType { get; set; } // 1 Live Now 2 Schedule Later
        public string? RequestServiceSkill { get; set; }
    }

    public class RequestServicesDto
    {
        public int? Id { get; set; }
        public string? EncId { get; set; }
        public string? ServiceTitle { get; set; }
        public string? PrefferedServiceTime { get; set; }
        public string? CategoriesOfProblems { get; set; }
        public string? ServiceDescription { get; set; }
        public string? FromDateTime { get; set; }
        public string? ToDateTime { get; set; }
        public string? RequestServiceSkills { get; set; }
        public string? ServiceLanguage { get; set; } // It can be multiple by , seperated
        public string? RequestedServiceUserId { get; set; }
        public string? RequestedServiceUserName { get; set; }
        public string? RequestServiceType { get; set; } // 1 Live Now 2 Schedule Later
        public string? CreatedAt { get; set; }
        public string? AppointmentTime { get; set; }
    }

    public class RequestServiceDto
    {
        public int? Id { get; set; }
        public string? RequestServiceEncId { get; set; }
        public string? ServiceTitle { get; set; }
        public string? ServiceDescription { get; set; }
        public string? FromDateTime { get; set; }
        public string? ToDateTime { get; set; }
        public string? ServiceLanguage { get; set; }
        public int? RequestServiceUserId { get; set; }
        public int? RequestServiceType { get; set; }
        public string? RequestServiceSkill { get; set; }
    }

    public class PostAddRequestServiceSkill
    {
        public string? RequestServiceName { get; set; }
        public int? RequestServiceId { get; set; }
    }
    #endregion

    #region Messages
    public class PostAddMessage
    {
        public string? MessageDescription { get; set; }
        public string? IsRead { get; set; }
        public string? FilePath { get; set; }
        public IFormFile? IFilePath { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? OrderId { get; set; }
        //order related
        public string? OfferTitle { get; set; }
        public string? OfferDescription { get; set; }
        public string? OfferPrice { get; set; }
        public string? TransactionFee { get; set; }
        public string? StartedDateTime { get; set; }
        public string? EndedDateTime { get; set; }
        public string? CustomerId { get; set; }
        public string? ValetId { get; set; }
        public string? Way { get; set; }
    }

    public class PostUpdateMessage
    {
        public string? OfferDetailId { get; set; }
        public string? MessageDescription { get; set; }
        public string? IsRead { get; set; }
        public string? FilePath { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? OrderId { get; set; }
        public string? OfferTitle { get; set; }
        public string? OfferDescription { get; set; }
        public string? OfferPrice { get; set; }
        public string? StartedDateTime { get; set; }
        public string? EndedDateTime { get; set; }
        public string? CustomerId { get; set; }
        public string? ValetId { get; set; }
        public string? OfferStatus { get; set; }
    }
    
    public class ViewModelMessage
    {
        public string? Id { get; set; }
        public string? MessageEncId { get; set; }
        public string? MessageDescription { get; set; }
        public string? IsRead { get; set; }
        public string? FilePath { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? OrderId { get; set; }
        public string? LastMessageUsername { get; set; }
        public string? Username { get; set; }
        public string? UserEncId { get; set; }
        public string? UserDecId { get; set; }
        public string? UserImage { get; set; }
        public string? MessageTime { get; set; }
    }

    public class ViewModelMessageChatBox
    {
        public string? Id { get; set; }
        public string? MessageEncId { get; set; }
        public string? MessageDescription { get; set; }
        public string? IsRead { get; set; }
        public string? FilePath { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? OrderId { get; set; }
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? ProfileImage { get; set; }
        public string? MessageTime { get; set; }
        //order related
        public string? OfferTitleId { get; set; }
        public string? OfferTitle { get; set; }
        public string? OfferDescription { get; set; }
        public string? OfferPrice { get; set; }

        public string? TransactionFee { get; set; }
        public string? StartedDateTime { get; set; }
        public string? EndedDateTime { get; set; }
        public string? CustomerId { get; set; }
        public string? ValetId { get; set; }
        public string? OfferStatus { get; set; }
        public string? OrderReasonId { get; set; }
        public string? OrderReasonStatus { get; set; }
        public string? OrderReasonType { get; set; }
        public string? OrderReasonIsActive { get; set; }
        public string? StartUrl { get; set; }
        public string? JoinUrl { get; set; }
        public int? IsZoomMeeting { get; set; }
    }
    #endregion

    #region OrderViewModels
    public class OrderDtoList
    {
        public string? Id { get; set; }
        public string? EncId { get; set; }
        public string? OrderTitle { get; set; }
        public string? OrderDescription { get; set; }
        public string? OrderPrice { get; set; }
        public string? OrderStatus { get; set; } // 0 In-Progress, 1 Accept, 2 Completed, 3 PendingPayment, 4 Cancelled
        public string? StripeChargeId { get; set; }
        public string? CapturedId { get; set; }
        public string? StartDateTime { get; set; }
        public string? EndDateTime { get; set; }
        public string? IsDelivered { get; set; }
        public string? RequestId { get; set; }
        public string? CustomerId { get; set; }
        public string? ValetId { get; set; }
        public string? PackageId { get; set; }
        public string? OrderReasonId { get; set; }
        public string? OrderReasonType { get; set; }
        public string? OrderReasonExplanation { get; set; }
        public string? OrderReasonIsActive { get; set; }
        public string? UserName { get; set; }
        public string? CreatedAt { get; set; }
        public string? OrderTrackingId { get; set; }
        public string? ValetStatus { get; set; }
        public string? CustomerStatus { get; set; }
        public string? PackageBuyFrom { get; set; }
        public OrderRating? Rating { get; set; }
    }

    public class OrderRating
    {
        public string? Reviews { get; set; }
        public int? Stars { get; set; }
    }
    #endregion

    #region StripeCheckout

    public class StripeOrderDetailForAdminDb
    {
        public int? Id { get; set; }
        public string? CustomerName { get; set; }
        public string? ITValet { get; set; }
        public string? OrderEncId { get; set; }
        public string? OrderTitle { get; set; }
        public string? PaidByPackage { get; set; }
        public string? StripeId { get; set; }
        public string? OrderPrice { get; set; }
        public string? OrderStatus { get; set; }
        public string? IsDelivered { get; set; }
        public string? PaymentStatus { get; set; }
        public string? StripeStatus { get; set; }
    }

    public class CheckOutDTO
    {
        public string? Id { set; get; }
        public string? FromDateTime { set; get; }
        public string? ToDateTime { set; get; }
        public string? PaymentTitle { set; get; }
        public string? PaymentDescription { set; get; }
        public string? Duration { set; get; }
        public string? orderId { set; get; }
        public string? customerId { set; get; }
        public string? ValetId { set; get; }
        public string? HourlyRate { set; get; }
        public string? StripeFee { set; get; }
        public string? platformFee { set; get; }
        public string? WorkingHours { set; get; }
        public string? TotalWorkCharges { set; get; }
        public string? stripeEmail { set; get; }
        public string? stripeToken { set; get; }
        public int? OfferId { get; set; }
        public string? ActualOrderPrice { get; set; }
        public int? PackageId { get; set; }
       public string? PackagePaidBy { set; get; }
    }

    #endregion

    #region DeliverOrder

    public class FilePathResponseDto
    {
        public string? FilePath { get; set; }
        public bool? Status { get; set; }
        public string? StatusCode { get; set; }
        public string? Message { get; set; }
    }

    public class OrderDeliverDto
    {
        public string? ValetId { get; set; }
        public string? CustomerId { get; set; }
        public string? OrderId { get; set; }
        public string? Description { get; set; }
        public string? IsAccepted { get; set; }
        public string? Reviews { get; set; }
        public string? Stars { get; set; }

    }

    #endregion

    #region PayPalViewModel
    public class PackageCheckOutViewModel
    {
        public int ClientId { get; set; }
        public int UserPackageId { get; set; }
        public string? PackageType { get; set; }
        public decimal PackagePrice { get; set; }
        public string? PayableAmount { get; set; }
        public string? Currency { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentId { get; set; }
        public int IsActive { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? StartDate { get; set; }
    }
    public class OrderCheckOutViewModel
    {
        public int ClientId { get; set; }
        public int ValetId { get; set; }
        public string? CaptureId { get; set; }
        public string? AuthorizationId { get; set; }
        public string? Currency { get; set; }
        public int OrderId { get; set; }
        public decimal? OrderPrice { get; set; }
        public string? PayableAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentId { get; set; }
        public string? PayPalTransactionFee { get; set; }
        public bool IsRefund { get; set; }
    }
    public class PayPalFundToValetViewModel
    {
        public int OrderId { get; set; }
        public string? PaymentId { get; set; }
        public int ClientId { get; set; }
        public string? PayPalAccEmail { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal SentPayment { get; set; }
        public int ValetId { get; set; }
        public string? BatchId { get; set; }
        public string? PayOutItemId { get; set; }
        public string? TransactionStatus { get; set; }
        public int OrderCheckOutId { get; set; }
    }
    public class PaymentCancelViewModel
    {
        public bool CancelByAdmin { get; set; }
        public string? CancelationReason { get; set; }
        public string? CancelationStatus { get; set; }
        public string? ReturnedAmount { get; set; }
    }
    public class PayPalOrderCheckOutViewModel
    {
        public int ClientId { get; set; }
        public int ValetId { get; set; }
        public int OrderId { get; set; }
        public int OfferId { get; set; }
        public bool PayByPackage { get; set; }
        public int? OrderStatus { get; set; }
        public string? OrderTitle { get; set; }
        public string? OrderDescription { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? PaymentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PayPalEarningInCome
    {
        public string? TotalEarnedAmount { get; set; }
        public string? AvailableIncomeForWithDrawl { get; set; }
        public string? PendingClearance { get; set; }
        public string? StripeTotalBalance { get; set; }
        public string? StripeCompletedOrderBalance { get; set; }
    }
    public class EarningsApiResponse
    {
        public int? UserId { get; set; }
        public string? BalancePending { get; set; }
        public string? BalanceAvailable { get; set; }
        public PayPalEarningInCome? PayPalEarning { get; set; }
    }

    public class CaptureAmountViewModel
    {
        public string? PayableAmount { get; set; }
        public string? Currency { get; set; }
    }
    public class PayPalAccountViewModel
    {
        public int ValetId { get; set; }
        public string? PayPalEmail { get; set; }
        public bool IsPayPalAuthorized { get; set; }
        public int IsActive { get; set; }
    }
    public class AddPayPalResult
    {
        public bool Success { get; }
        public string Message { get; }
        public AddPayPalResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
    public class TransactionDetailViewModel
    {
        public string TransactionId { get; set; }
        public string State { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        // Add other relevant properties you want to display
    }
    public class PayPalCheckOutURL
    {
        public string Url { get; set; }
    }
    public class PackageCOutRequest
    {
        public int? ClientId { get; set; }
        public string? SelectedPackage { get; set; }
        public string? stripeEmail { set; get; }
        public string? stripeToken { set; get; }
    }
    public class CheckoutPaymentStatusPackage
    {
        public string? PaymentStatus { get; set; }
        public string? PaymentId { get; set; }
    }
    public class CheckPaymentStatusForOrder
    {
        public string? PaymentStatus { get; set; }
        public string? EncOrderId { set; get; }
        public string? PaymentId { get; set; }
        public string? AuthorizationId { get; set; }
        public string? CaptureId { get; set; }
    }
    public class PayPalOrderInsertion
    {
        public string? PaymentId { get; set; }
        public int? OrderStatus { get; set; }
        public string? OrderTitle { get; set; }
        public string? OrderDescription { get; set; }
        public decimal OrderPrice { get; set; }
    }
    public class OrderReasonViewModel
    {
        public string? ReasonExplanation { get; set; }
        public int? ReasonType { get; set; }
        public int? OrderId { get; set; }
    }

    public class AcceptOrder
    {
        public string? CustomerId { get; set; }
        public string? ValetId { get; set; }
        public string? OrderId { get; set; }
        public string? Description { get; set; }
        public string? IsAccepted { get; set; }
        public string? Stars { get; set; }
        public string? Reviews { get; set; }
    }

    public class OrderCheckOutAccepted
    {
        public int OrderId { get; set; }
        public string? PaymentId { get; set; }
        public decimal? OrderPrice { get; set; }
        public string? PayPalAccount { get; set; }
    }

    public class OrderAcceptedOfPackage
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int ValetId { get; set; }
        public bool PaidByPackage { get; set; }
        public string? PayPalAccount { get; set; }
        public decimal? OrderPrice { get; set; }
    }
    public class ValetFundRecord
    {
        public int OrderId { get; set; }
        public string? PaymentId { get; set; }
        public int ClientId { get; set; }
        public string? PayPalAccEmail { get; set; }
        public decimal? OrderPrice { get; set; }
        public int ValetId { get; set; }
        public int OrderCheckOutId { get; set; }
    }

    public class PayPalOrderDetailsForAdminDB
    {
        public int? Id { get; set; }
        public string? CustomerName { get; set; }
        public string? ITValet { get; set; }
        public string? OrderEncId { get; set; }
        public string? OrderTitle { get; set; }
        public bool? PaidByPackage { get; set; }
        public string? CaptureId { get; set; }
        public string? OrderPrice { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
    }

    public class PayPalTransactionDetailsForAdminDB
    {
        public string? CustomerName { get; set; }
        public string? ITValetName { get; set; }
        public string? PayPalEmailAccount { get; set; }
        public string? TransactionStatus { get; set; }
        public string? OrderEncId { get; set; }
        public string? PayOutItemId { get; set; }
        public string? OrderPrice { get; set; }
        public string? OrderTitle { get; set; }
        public string? SentAmount { get; set; }
        public string? PlatformFee { get; set; }
        public string? ExpectedDateToTransmitPayment { get; set; }
    }

    public class PayPalUnclaimedTransactionDetailsForAdminDB
    {
        public string? CustomerName { get; set; }
        public string? ITValetName { get; set; }
        public string? PayPalEmailAccount { get; set; }
        public string? TransactionStatus { get; set; }
        public string? Reason { get; set; }
        public string? UnclaimedAmountStatus { get; set; }
        public string? OrderTitle { get; set; }
        public string? OrderEncId { get; set; }
    }

    public class ValetAvailableSlots
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
    }
    #endregion

    #region CalendarsViewModal
    public class OrderEventsViewModal
    {
        public string? OrderEncId { get; set; }
        public string? OrderTitle { get; set; }
        public string? OrderDescription { get; set;}
        public string? OrderDetailUrl { get; set; }
        public int? OrderStatus { get; set; }
        public string? OrderStatusDescription { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
    }

    public class ActiveUsersNameDto
    {
        public string? UserName { get; set; }
        public string? UserId { get; set; }
    }
    #endregion
    public class UserRatingListDto
    {
        [Column(TypeName = " nvarchar(1000)")]
        public string? Reviews { get; set; }
        public int? Stars { get; set; }
        public string? CustomerId { get; set; }
        public string? ValetId { get; set; }
        public string? UserName { get; set; }
        public int? OrderId { get; set; }
    }
    public class StripeBankAccountDto
    {
        public string? Userid { get; set; }
        public string? stripeAccountId { get; set; }
        public string? bankAccountNumber { get; set; }
        public string? routingNo { get; set; }
        public string? accountHolderName { get; set; }
    }
    public class CreateNotificationDto
    {

        [Required]
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
    public class ViewNotificationDto
    {
        public string NotificationId { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public int IsRead { get; set; }
        public string CreatedAt { get; set; }
        public Nullable<int> NotificationType { get; set; }
    }

    public class CompletedOrderRecord
    {
        public string? EncOrderId { get; set; }
        public string? OrderTitle { get; set; }
        public string? OrderPaidBy { get; set; }
        public string? OrderPrice { get; set; }
        public string? EarnedFromOrder { get; set; }
        public string? CompletedAt { get; set; }
    }

}
