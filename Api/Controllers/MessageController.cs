using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Stripe;
using System.Text.RegularExpressions;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;
        private readonly IUserRepo userRepo;
        private readonly IMessagesRepo messagesRepo;
        private readonly ProjectVariables projectVariables;
        private readonly IOfferDetailsRepo offerDetailsRepo;
        private readonly IFundTransferService _fundTransferService;
        private readonly IJwtUtils jwtUtils;
        private readonly IOrderRepo orderRepo;
        private readonly IOrderReasonRepo orderReasonRepo;
        private readonly IUserRatingRepo userRatingRepo;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly INotificationService userPackageService;
        private readonly INotificationRepo _notificationService;

        public MessageController(IHubContext<NotificationHubSocket> notificationHubSocket,
            IUserRepo _userRepo, IMessagesRepo _messagesRepo, IOfferDetailsRepo _offerDetailsRepo,
            IOptions<ProjectVariables> options, IJwtUtils _jwtUtils, IOrderRepo _orderRepo,
            IOrderReasonRepo _orderReasonRepo, IFundTransferService fundTransferService,
            IUserRatingRepo _userRatingRepo, IPayPalGateWayService payPalGateWayService, INotificationService _userPackageService, INotificationRepo notificationService)
        {
            _notificationHubSocket = notificationHubSocket;
            userRepo = _userRepo;
            messagesRepo = _messagesRepo;
            projectVariables = options.Value;
            offerDetailsRepo = _offerDetailsRepo;
            jwtUtils = _jwtUtils;
            orderRepo = _orderRepo;
            orderReasonRepo = _orderReasonRepo;
            _fundTransferService = fundTransferService;
            userRatingRepo = _userRatingRepo;
            userPackageService = _userPackageService;
            _payPalGateWayService = payPalGateWayService;
            _notificationService = notificationService;
        }

        [HttpPost("SendMessageToClients")]
        public async Task<IActionResult> SendMessageToClients(string userId, string message)
        {
            await _notificationHubSocket.Clients.All.SendAsync("ReceiveMessage", userId, message);
            return Ok(message);
        }

        #region Messages
        [HttpPost("PostAddMessage")]
        public async Task<IActionResult> PostAddMessage(PostAddMessage postAddMessage)
        {
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(postAddMessage.SenderId));
            var getOneUser = await userRepo.GetUserById(Convert.ToInt32(postAddMessage.ReceiverId));
            var getChatMessages = new List<Message>();
            var message = new Message();
            bool IsWayUserProfile = postAddMessage.Way == "ViewUserProfile" ? true : false;
            if (IsWayUserProfile)
            {
                getChatMessages = (List<Message>)await messagesRepo.GetMessageBySenderIdAndRecieverId(getLoggedInUser.Id, Convert.ToInt32(getOneUser.Id));
                if (getChatMessages != null && getChatMessages.Count > 0)
                {
                    return Ok(new ResponseDto() { Id = StringCipher.EncryptId(Convert.ToInt32(postAddMessage.ReceiverId)), Status = true, StatusCode = "200", Message = "Exist" });
                }
                postAddMessage.MessageDescription = getLoggedInUser.Role == 4 ? "Hi! I am interested in your services! Please reach me back asap. Thanks!" : "Hi! I am interested in your project! Please reach me back asap. Thanks!";
                message.MessageDescription = postAddMessage.MessageDescription;
                message.SenderId = Convert.ToInt32(postAddMessage.SenderId);
                message.ReceiverId = Convert.ToInt32(postAddMessage.ReceiverId);
                message.IsRead = 0;
                message.IsActive = 1;
                message.CreatedAt = GeneralPurpose.DateTimeNow();
                await messagesRepo.AddMessage(message);
                if (!await messagesRepo.saveChangesFunction())
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "500", Message = "User unavailable. Try again after some time." });
                }
                Notification notificationObj = new Notification
                {
                    UserId = message.ReceiverId,
                    Title = "Message Received",
                    IsRead = 0,
                    IsActive = (int)EnumActiveStatus.Active,
                    Url = $"{projectVariables.BaseUrl}Home/Messages",
                    CreatedAt = GeneralPurpose.DateTimeNow(),
                    Description = "You just received a message.",
                    NotificationType = (int)NotificationType.OrderCancellationRequested
                };
                bool isNotification = await _notificationService.AddNotification(notificationObj);
                await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.ReceiverId.ToString());
                return Ok(new ResponseDto() { Id = StringCipher.EncryptId(Convert.ToInt32(postAddMessage.ReceiverId)), Status = true, StatusCode = "200", Message = "Message Sent Successfully." });
            }
            else if (!string.IsNullOrEmpty(postAddMessage.MessageDescription))
            {
                message.MessageDescription = postAddMessage.MessageDescription;
                message.SenderId = Convert.ToInt32(postAddMessage.SenderId);
                message.ReceiverId = Convert.ToInt32(postAddMessage.ReceiverId);
                message.IsRead = 0;
                message.IsActive = 1;
                message.CreatedAt = GeneralPurpose.DateTimeNow();
                await messagesRepo.AddMessage(message);
                if (!await messagesRepo.saveChangesFunction())
                {
                    return Ok("Failed to send/add message.");
                }
                Notification notificationObj = new Notification
                {
                    UserId = message.ReceiverId,
                    Title = "Message Received",
                    IsRead = 0,
                    IsActive = (int)EnumActiveStatus.Active,
                    Url = $"{projectVariables.BaseUrl}Home/Messages",
                    CreatedAt = GeneralPurpose.DateTimeNow(),
                    Description = "You just received a message.",
                    NotificationType = (int)NotificationType.OrderCancellationRequested
                };
                bool isNotification = await _notificationService.AddNotification(notificationObj);
                await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.ReceiverId.ToString());
                if (message.Id != 0)
                {
                    string msgTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone);
                    string userName = getLoggedInUser.UserName;
                    string profile = getLoggedInUser.ProfilePicture;

                    //order related
                    if (!string.IsNullOrEmpty(postAddMessage.OfferTitle))
                    {
                        DateTime orderStartTime = Convert.ToDateTime(postAddMessage.StartedDateTime);
                        DateTime orderEndTime = Convert.ToDateTime(postAddMessage.EndedDateTime);
                        var getTotalHours = GeneralPurpose.CalculatePrice(orderStartTime, orderEndTime);
                        if (getLoggedInUser.Role != 3)
                        {
                            postAddMessage.CustomerId = postAddMessage.ReceiverId.ToString();
                            postAddMessage.ValetId = postAddMessage.CustomerId;
                            postAddMessage.OfferPrice = getTotalHours.price.ToString();
                            postAddMessage.TransactionFee = getTotalHours.fee.ToString();
                        }
                        else
                        {
                            postAddMessage.ValetId = postAddMessage.ReceiverId.ToString();
                            postAddMessage.OfferPrice = getTotalHours.price.ToString();
                            postAddMessage.TransactionFee = getTotalHours.fee.ToString();
                        }

                        var offer = new OfferDetail();
                        offer.OfferTitle = postAddMessage.OfferTitle;
                        offer.OfferDescription = postAddMessage.OfferDescription;
                        offer.OfferPrice = Convert.ToDouble(postAddMessage.OfferPrice);
                        offer.StartedDateTime = Convert.ToDateTime(postAddMessage.StartedDateTime);
                        offer.EndedDateTime = Convert.ToDateTime(postAddMessage.EndedDateTime);
                        offer.OfferStatus = 1;
                        offer.TransactionFee = postAddMessage.TransactionFee;
                        offer.CustomerId = Convert.ToInt32(postAddMessage.CustomerId);
                        offer.ValetId = Convert.ToInt32(postAddMessage.ValetId);
                        offer.MessageId = message.Id;
                        if (await offerDetailsRepo.AddOfferDetail(offer))
                        {

                            // send according to order detail
                            await _notificationHubSocket.Clients.All.SendAsync("ReceiveOffer", offer, message.SenderId, message.ReceiverId, userName, profile, msgTime);
                            return Ok(new { offer, SenderId = message.SenderId, ReceiverId = message.ReceiverId, UserName = userName, Profile = profile,
                                MessageTime = msgTime });
                        }
                        return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                    }
                    //end

                    await _notificationHubSocket.Clients.All.SendAsync("ReceiveMessage", message.SenderId, message.ReceiverId, userName, profile, message.MessageDescription, msgTime);

                    return Ok(new { UserName = userName, Profile = profile, Message = message.MessageDescription, MessageTime = msgTime });
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Message cannot be Empty" });
        }

        [HttpPut("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus(PostUpdateMessage postAddMessage)
        {
            var offerDetails = await offerDetailsRepo.GetOfferDetailById(Convert.ToInt32(postAddMessage.OfferDetailId));
            string offerStatus = "";
            if (postAddMessage.MessageDescription == "Accept")
            {
                offerDetails.OfferStatus = 2;
                offerStatus = "accepted";
            }
            if (postAddMessage.MessageDescription == "Reject")
            {
                offerDetails.OfferStatus = 3;
                offerStatus = "rejected";
            }
            offerDetails.UpdatedAt = GeneralPurpose.DateTimeNow();
            if (!await offerDetailsRepo.UpdateOfferDetail(offerDetails))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            var message = await messagesRepo.GetMessageById((int)offerDetails.MessageId);
            message.MessageDescription = postAddMessage.MessageDescription;
            message.UpdatedAt = GeneralPurpose.DateTimeNow();

            if (!await messagesRepo.UpdateMessage(message))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
            var getReceiverUser = await userRepo.GetUserById(Convert.ToInt32(message.ReceiverId));
            await _notificationHubSocket.Clients.All.SendAsync("ChangeOfferStatus", offerDetails.Id, offerDetails.OfferTitle, message.SenderId, message.ReceiverId, getReceiverUser.UserName, offerStatus);
            return Ok(new ResponseDto() { Id = offerDetails.Id.ToString(), Status = true, StatusCode = "200", Message = GlobalMessages.SuccessMessage });
        }

        [HttpGet("GetMessageSideBarList")]
        public async Task<IActionResult> GetMessageSideBarList(string? loggedInUserId, string? Name = "", string? GetUserChatOnTop = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(loggedInUserId))
                {
                    var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(loggedInUserId));
                    var Sender = new Models.User();
                    var Receiver = new Models.User();
                    var getMessages = await messagesRepo.GetMessageByUserId(getLoggedInUser.Id);
                    List<ViewModelMessage> messagesList = new List<ViewModelMessage>();
                    foreach (Message message in getMessages)
                    {
                        ViewModelMessage viewModelMessage = new ViewModelMessage()
                        {
                            Id = message.Id.ToString(),
                            MessageEncId = StringCipher.EncryptId(message.Id),
                            MessageDescription = message.MessageDescription,
                            IsRead = message.IsRead != null ? message.IsRead.ToString() : null,
                            FilePath = message.FilePath != null ? message.FilePath.ToString() : null,
                            MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone),
                            SenderId = message.SenderId.ToString(),
                            ReceiverId = message.ReceiverId.ToString(),
                        };

                        var Id = message.SenderId != getLoggedInUser.Id ? message.SenderId : message.ReceiverId;
                        Sender = await userRepo.GetUserById((int)Id);
                        viewModelMessage.Username = Sender.FirstName + " " + Sender.LastName;
                        viewModelMessage.UserImage = Sender.ProfilePicture;
                        viewModelMessage.UserEncId = StringCipher.EncryptId(Sender.Id);
                        viewModelMessage.UserDecId = Sender.Id.ToString();
                        if (message.SenderId == getLoggedInUser.Id)
                        {
                            viewModelMessage.LastMessageUsername = "";
                        }
                        else
                        {
                            viewModelMessage.LastMessageUsername = Sender.FirstName + " " + Sender.LastName;
                        }
                        messagesList.Add(viewModelMessage);

                    }
                    if (!string.IsNullOrEmpty(GetUserChatOnTop) && GetUserChatOnTop != "null")
                    {
                        // ID to move to the top
                        string idToMoveToTop = StringCipher.DecryptId(GetUserChatOnTop).ToString();
                        // Reordering the list
                        messagesList = messagesList.OrderBy(user => user.SenderId == idToMoveToTop ? 0 : 1)
                                                  .ThenBy(user => user.Id)
                                                  .ToList();
                    }
                    if (!String.IsNullOrWhiteSpace(Name))
                    {
                        messagesList = messagesList.Where(a => a.Username.ToLower().Contains(Name.ToLower())).ToList();
                    }
                    // messagesList = messagesList.OrderByDescending(x=> x.MessageTime).ToList();

                    messagesList = messagesList.OrderByDescending(msg => DateTime.Parse(msg.MessageTime)).ToList();



                    return Ok(messagesList);
                }
                return null;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        [HttpGet("GetMessagesForUser")]
        public async Task<IActionResult> GetMessagesForUser(string? loggedInUserId, string? userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }
                var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(loggedInUserId));
                var getUser = await userRepo.GetUserById(Convert.ToInt32(userId));
                if (loggedInUserId != null)
                {
                    var getMessages = await messagesRepo.GetMessageBySenderIdAndRecieverId(getLoggedInUser.Id, Convert.ToInt32(userId));
                    List<ViewModelMessageChatBox> messagesList = new List<ViewModelMessageChatBox>();
                    foreach (var message in getMessages)
                    {
                        var viewModelMessage = new ViewModelMessageChatBox
                        {
                            Id = message.Id.ToString(),
                            MessageEncId = StringCipher.EncryptId(message.Id),
                            MessageDescription = message.MessageDescription,
                            IsRead = message.IsRead?.ToString(),
                            FilePath = message.FilePath,
                            MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone),
                            SenderId = message.SenderId.ToString(),
                        };
                        //order wprk
                        if (message.OfferDetails != null)
                        {
                            viewModelMessage.OfferTitleId = message.OfferDetails.Id.ToString();
                            viewModelMessage.OfferTitle = message.OfferDetails.OfferTitle;
                            viewModelMessage.TransactionFee = message.OfferDetails.TransactionFee;
                            viewModelMessage.OfferDescription = message.OfferDetails.OfferDescription;
                            viewModelMessage.OfferPrice = message.OfferDetails.OfferPrice.ToString();
                            viewModelMessage.StartedDateTime = message.OfferDetails.StartedDateTime.ToString();
                            viewModelMessage.EndedDateTime = message.OfferDetails.EndedDateTime.ToString();
                            viewModelMessage.CustomerId = message.OfferDetails.CustomerId.ToString();
                            viewModelMessage.ValetId = message.OfferDetails.ValetId.ToString();
                            viewModelMessage.OfferStatus = message.OfferDetails.OfferStatus.ToString();
                        }
                        //end

                        if (getLoggedInUser.Id == message.SenderId)
                        {
                            viewModelMessage.Name = $"{getLoggedInUser.FirstName} {getLoggedInUser.LastName}";
                            viewModelMessage.Username = getLoggedInUser.UserName;
                            viewModelMessage.ProfileImage = getLoggedInUser.ProfilePicture;
                        }
                        else
                        {
                            viewModelMessage.Name = $"{getUser.FirstName} {getUser.LastName}";
                            viewModelMessage.Username = getUser.UserName;
                            viewModelMessage.ProfileImage = getUser.ProfilePicture;
                        }
                        messagesList.Add(viewModelMessage);
                    }
                    return Ok(messagesList);
                }
                return null;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }


        [HttpGet("GetReceiverStatus")]
        public async Task<IActionResult> GetReceiverStatus(string? userId)
        {
            try
            {
                 var user = await userRepo.GetUserById(Convert.ToInt32(userId));

                if (user == null)
                {
                    return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
                }
                return Ok(user.Status);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpGet("GetMessageSideBarLists")]
        public async Task<IActionResult> GetMessageSideBarLists(string? loggedInUserId, string? Name = "", string? GetUserChatOnTop = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(loggedInUserId))
                {
                    var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(loggedInUserId));
                    var Sender = new Models.User();
                    var Receiver = new Models.User();
                    var getMessages = await messagesRepo.GetMessageByUserId(getLoggedInUser.Id);
                    List<ViewModelMessage> messagesList = new List<ViewModelMessage>();
                    foreach (Message message in getMessages)
                    {
                        ViewModelMessage viewModelMessage = new ViewModelMessage()
                        {
                            Id = message.Id.ToString(),
                            MessageEncId = StringCipher.EncryptId(message.Id),
                            MessageDescription = message.MessageDescription,
                            IsRead = message.IsRead != null ? message.IsRead.ToString() : null,
                            FilePath = message.FilePath != null ? message.FilePath.ToString() : null,
                            MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone),
                            SenderId = message.SenderId.ToString(),
                            ReceiverId = message.ReceiverId.ToString(),
                        };

                        var Id = message.SenderId != getLoggedInUser.Id ? message.SenderId : message.ReceiverId;
                        Sender = await userRepo.GetUserById((int)Id);
                        viewModelMessage.Username = Sender.FirstName + " " + Sender.LastName;
                        viewModelMessage.UserImage = projectVariables.BaseUrl + Sender.ProfilePicture;
                        viewModelMessage.UserEncId = StringCipher.EncryptId(Sender.Id);
                        viewModelMessage.UserDecId = Sender.Id.ToString();
                        if (message.SenderId == getLoggedInUser.Id)
                        {
                            viewModelMessage.LastMessageUsername = "";
                        }
                        else
                        {
                            viewModelMessage.LastMessageUsername = Sender.FirstName + " " + Sender.LastName;
                        }
                        messagesList.Add(viewModelMessage);

                    }
                    if (!string.IsNullOrEmpty(GetUserChatOnTop) && GetUserChatOnTop != "null")
                    {
                        // ID to move to the top
                        string idToMoveToTop = StringCipher.DecryptId(GetUserChatOnTop).ToString();
                        // Reordering the list
                        messagesList = messagesList.OrderBy(user => user.SenderId == idToMoveToTop ? 0 : 1)
                                                  .ThenBy(user => user.Id)
                                                  .ToList();
                    }
                    if (!String.IsNullOrWhiteSpace(Name))
                    {
                        messagesList = messagesList.Where(a => a.Username.ToLower().Contains(Name.ToLower())).ToList();
                    }
                    // messagesList = messagesList.OrderByDescending(x=> x.MessageTime).ToList();

                    messagesList = messagesList.OrderByDescending(msg => DateTime.Parse(msg.MessageTime)).ToList();


                    return Ok(new ResponseDto()
                    {
                        Status = true,
                        StatusCode = "200",
                        Data = messagesList
                    });
                }
                return null;
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }


        #endregion

        #region OrderZone
        [HttpGet("GetMessagesForOrder")]
        public async Task<IActionResult> GetMessagesForOrder(string? orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return null;
                }
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var getLoggedInUser = await userRepo.GetUserById((int)getUserFromToken.Id);
                var getOrders = await orderRepo.GetOrderById(Convert.ToInt32(orderId));
                var getUser = new Models.User();
                var getMessages = await messagesRepo.GetMessageListByOrdrId(getOrders.Id);
                List<ViewModelMessageChatBox> messagesList = new List<ViewModelMessageChatBox>();
                foreach (var message in getMessages)
                {

                    string StartUrl = "";
                    string EndUrl = "";

                    if (message.IsZoomMessage == 1)
                    {
                        StartUrl = ExtractPart(message.MessageDescription, 1);
                        EndUrl = ExtractPart(message.MessageDescription, 2);
                    }


                    OrderReason OrderReasonOfTheMessage = new OrderReason();
                    if (message.OrderReasonId != null)
                    {
                        OrderReasonOfTheMessage = await orderReasonRepo.GetOrderReasonByOrderReasonId((int)message.OrderReasonId);
                    }
                    if (getUser.Id == 0)
                    {
                        var Ids = getLoggedInUser.Id == message.SenderId ? message.ReceiverId : message.SenderId;
                        getUser = await userRepo.GetUserById((int)Ids);
                    }
                    var viewModelMessage = new ViewModelMessageChatBox
                    {
                        Id = message.Id.ToString(),
                        MessageEncId = StringCipher.EncryptId(message.Id),
                        MessageDescription = message.MessageDescription,
                        IsRead = message.IsRead?.ToString(),
                        FilePath = message.FilePath,
                        MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone),
                        SenderId = message.SenderId.ToString(),
                        OrderReasonId = message.OrderReasonId != null ? message.OrderReasonId.ToString() : "",
                    };
                    if (OrderReasonOfTheMessage != null && OrderReasonOfTheMessage.ReasonType != 0)
                    {
                        if (OrderReasonOfTheMessage.ReasonType != null)
                        {
                            viewModelMessage.OrderReasonType = Enum.GetName(typeof(OrderReasonType), OrderReasonOfTheMessage.ReasonType);
                        }
                        if (OrderReasonOfTheMessage.IsActive != null)
                        {
                            viewModelMessage.OrderReasonIsActive = OrderReasonOfTheMessage.IsActive.ToString();
                        }
                    }
                    if (message.OfferDetails != null)
                    {
                        viewModelMessage.OfferTitleId = message.OfferDetails.Id.ToString();
                        viewModelMessage.OfferTitle = message.OfferDetails.OfferTitle;
                        viewModelMessage.OfferDescription = message.OfferDetails.OfferDescription;
                        viewModelMessage.OfferPrice = message.OfferDetails.OfferPrice.ToString();
                        viewModelMessage.StartedDateTime = message.OfferDetails.StartedDateTime.ToString();
                        viewModelMessage.EndedDateTime = message.OfferDetails.EndedDateTime.ToString();
                        viewModelMessage.CustomerId = message.OfferDetails.CustomerId.ToString();
                        viewModelMessage.ValetId = message.OfferDetails.ValetId.ToString();
                        viewModelMessage.OfferStatus = message.OfferDetails.OfferStatus.ToString();
                    }
                    if (getLoggedInUser.Id == message.SenderId)
                    {
                        if (message.IsZoomMessage == 1)
                        {
                            viewModelMessage.StartUrl = StartUrl;
                            viewModelMessage.IsZoomMeeting = string.IsNullOrEmpty(StartUrl) ? 0 : 1;
                            viewModelMessage.MessageDescription = "Zoom Meeting Created";
                            viewModelMessage.OrderReasonType = "Zoom";
                        }

                        viewModelMessage.Username = getLoggedInUser.UserName;
                        viewModelMessage.ProfileImage = getLoggedInUser.ProfilePicture;
                    }
                    else
                    {
                        if (message.IsZoomMessage == 1)
                        {
                            viewModelMessage.JoinUrl = EndUrl;
                            viewModelMessage.IsZoomMeeting = string.IsNullOrEmpty(StartUrl) ? 0 : 1;
                            viewModelMessage.MessageDescription = "Zoom Meeting Created";
                            viewModelMessage.OrderReasonType = "Zoom";
                        }

                        viewModelMessage.Username = getUser.UserName;
                        viewModelMessage.ProfileImage = getUser.ProfilePicture;
                    }
                    messagesList.Add(viewModelMessage);
                }
                return Ok(messagesList);
            }
            catch (Exception ex)
            {
                var x = ex.Message.ToString();
                return null;
            }
        }

        [HttpPost("PostAddOrderMessage")]
        public async Task<IActionResult> PostAddOrderMessage([FromForm] PostAddMessage postAddMessage)
        {
            try
            {
                var getSender = await userRepo.GetUserById(Convert.ToInt32(postAddMessage.SenderId));
                var getReciever = await userRepo.GetUserById(Convert.ToInt32(postAddMessage.ReceiverId));
                var message = new Message();
                message.MessageDescription = String.IsNullOrEmpty(postAddMessage.MessageDescription) ? "" : postAddMessage.MessageDescription;
                message.SenderId = Convert.ToInt32(postAddMessage.SenderId);
                message.ReceiverId = Convert.ToInt32(postAddMessage.ReceiverId);
                message.OrderId = Convert.ToInt32(postAddMessage.OrderId);
                message.IsRead = 0;
                message.IsActive = 1;
                message.CreatedAt = GeneralPurpose.DateTimeNow();
                
                var filePath = "";
                if (postAddMessage.IFilePath != null)
                {
                    filePath = await UploadFiles(postAddMessage.IFilePath, "OrderDeliverable");
                    message.FilePath = filePath;
                }
                if (postAddMessage.Way == "2")
                {
                    var OrderId = Convert.ToInt32(postAddMessage.OrderId);
                    var getOrder = await orderRepo.GetOrderById(OrderId);
                    getOrder.IsDelivered = 1;
                    getOrder.UpdatedAt = GeneralPurpose.DateTimeNow();
                    var updateOrder = await orderRepo.UpdateOrder(getOrder);
                }

                await messagesRepo.AddMessage(message);
                if (!await messagesRepo.saveChangesFunction())
                {
                    return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = postAddMessage.Way != "2" ? GlobalMessages.MessageSentFail : GlobalMessages.OrderDeliverFail });
                }

                if (message.Id != 0)
                {
                    string msgTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getSender.Timezone);
                    string userName = getSender.UserName;
                    string profile = getSender.ProfilePicture;

                    ReceiveOrderMessageDto receiveOrderMessageDto = new ReceiveOrderMessageDto()
                    {
                        senderId = message.SenderId,
                        receiverId = message.ReceiverId,
                        userName = userName,
                        userProfile = profile,
                        message = message.MessageDescription,
                        messageTime = msgTime,
                        newOrderReasonId = "",
                        filePath = filePath,
                        IsDelivery = postAddMessage.Way
                    };
                    Notification notificationObj = new Notification
                    {
                        UserId = message.ReceiverId,
                        Title = "Message Received",
                        IsRead = 0,
                        IsActive = (int)EnumActiveStatus.Active,
                        Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)message.OrderId)}",
                        CreatedAt = GeneralPurpose.DateTimeNow(),
                        Description = "You just received a message for your order.",
                        NotificationType = (int)NotificationType.OrderCancellationRequested
                    };
                    bool isNotification = await _notificationService.AddNotification(notificationObj);
                    await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.ReceiverId.ToString());
                    await _notificationHubSocket.Clients.All.SendAsync("ReceiveOrderMessage", receiveOrderMessageDto);

                    return Ok(new { UserName = userName, Profile = profile, Message = message.MessageDescription, MessageTime = msgTime, FilePath = filePath });
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = postAddMessage.Way != "2" ? GlobalMessages.MessageSentFail : GlobalMessages.OrderDeliverFail });
            }
            catch (Exception ex)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = postAddMessage.Way != "2" ? GlobalMessages.MessageSentFail : GlobalMessages.OrderDeliverFail });
            }
        }

        [HttpPut("PostOrderStatus")]
        public async Task<IActionResult> PostOrderStatus(string orderId, 
            string orderStatus, string senderId, string receiverId, string? explanation = "",
            string? dateExtension = "")
        {
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(senderId));

            var getOrder = await orderRepo.GetOrderById(Convert.ToInt32(orderId));
            var getMessage = new Message();

            getMessage.SenderId = Convert.ToInt32(senderId);
            getMessage.ReceiverId = Convert.ToInt32(receiverId);
            getMessage.OrderId = Convert.ToInt32(orderId);

            var OrderReason = new OrderReason();
            OrderReason.OrderId = getOrder.Id;
            OrderReason.ReasonExplanation = explanation;
            if (orderStatus == "Extend")
            {
                if (dateExtension != "")
                {
                    DateTime dateOfExtension = DateTime.Parse(dateExtension);
                    DateTime currentDate = DateTime.Now;
                    // Calculate the difference in days
                    TimeSpan difference = dateOfExtension - currentDate;
                    int daysDifference = (int)difference.TotalDays;
                    OrderReason.ReasonExplanation = explanation + " Requested days are " + daysDifference;

                }
                OrderReason.ReasonType = 1;
                getMessage.MessageDescription = OrderReason.ReasonExplanation + ". <br> The Extended Date is " + dateExtension;

                Notification notificationObj = new Notification
                {
                    UserId = Convert.ToInt32(receiverId),
                    Title = "Extend Date",
                    IsRead = 0,
                    IsActive = (int)EnumActiveStatus.Active,
                    Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)getOrder.Id)}",
                    CreatedAt = GeneralPurpose.DateTimeNow(),
                    Description = "Order Date Extension Requested.",
                    NotificationType = (int)NotificationType.DateExtensionRequested
                };
                bool isNotification = await _notificationService.AddNotification(notificationObj);
            }
            if (orderStatus == "Cancel")
            {
                OrderReason.ReasonType = 3;
                getMessage.MessageDescription = OrderReason.ReasonExplanation;

                Notification notificationObj = new Notification
                {
                    UserId = Convert.ToInt32(receiverId),
                    Title = "Cancel Order",
                    IsRead = 0,
                    IsActive = (int)EnumActiveStatus.Active,
                    Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)getOrder.Id)}",
                    CreatedAt = GeneralPurpose.DateTimeNow(),
                    Description = "Order cancellation Requested.",
                    NotificationType = (int)NotificationType.OrderCancellationRequested
                };
                bool isNotification = await _notificationService.AddNotification(notificationObj);
            }
            OrderReason.IsActive = 2;
            OrderReason.CreatedAt = GeneralPurpose.DateTimeNow();
            var orderReasons = await orderReasonRepo.AddOrderReason(OrderReason);
            getMessage.OrderReasonId = orderReasons.Id;
            var message = await PostAddOrderReasonMessage(getMessage);

            string msgTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone);
            string userName = getLoggedInUser.UserName;
            string profile = getLoggedInUser.ProfilePicture;

            string newOrderReasonId = orderReasons.Id.ToString();
            ReceiveOrderMessageDto receiveOrderMessageDto = new ReceiveOrderMessageDto()
            {
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                userName = userName,
                userProfile = profile,
                message = message.MessageDescription,
                messageTime = msgTime,
                newOrderReasonId = newOrderReasonId,
                reasonType = orderStatus
            };
            await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.ReceiverId.ToString());
            await _notificationHubSocket.Clients.All.SendAsync("ReceiveOrderMessage", receiveOrderMessageDto);

            return Ok(new { UserName = userName, Profile = profile, Message = message.MessageDescription, MessageTime = msgTime, NewOrderReasonId = orderReasons.Id.ToString() });
        }

        [HttpPut("AcceptOrRejectOrderReason")]
        public async Task<IActionResult> AcceptOrRejectOrderReason(string orderId,
         string orderStatus, string reasonStatus, string senderId, string receiverId)
        {
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(senderId));

            Order getOrder = await orderRepo.GetOrderById(Convert.ToInt32(orderId));
            OrderReason OrderReason = await orderReasonRepo.GetOrderReasonByOrderId(Convert.ToInt32(orderId));
            string generalStatusReason = "";
            string generalOrderReason = "";

            if (orderStatus == "Extend")
            {
                int ExtendedDay = GetLastNumberOfString(OrderReason.ReasonExplanation);
                if (ExtendedDay != -1)
                {
                    DateTime updatedEndDate = Convert.ToDateTime(getOrder.EndDateTime).AddDays(ExtendedDay);
                    getOrder.EndDateTime = updatedEndDate;
                }


            }
            generalStatusReason = reasonStatus == "Accept" ? generalStatusReason = "Accepted" : generalStatusReason = "Rejected";
            generalOrderReason = orderStatus == "Cancel" ? "Request to Cancel this order has been " + generalStatusReason : "Request to Extend date of this order has been " + generalStatusReason;

            if (orderStatus == "Cancel" && reasonStatus == "Accept")
            {

                if (getOrder.PackageId == null)
                {
                    if (getOrder.CapturedId != null)
                    {
                        bool isRefundStatus = await _fundTransferService.RefundPayment(getOrder.CapturedId, getOrder.Id);
                    }
                    else
                    {
                        var RefundOrderPayment = await RefundPayment(getOrder.StripeChargeId, getOrder.Id);
                        if (RefundOrderPayment)
                        {
                            bool updateCancelOrderStatus = await orderRepo.UpdateOrderStatusForCancel(getOrder.Id);
                        }         
                    }
                }
                else
                {
                    if(getOrder.PackageBuyFrom == "PAYPAL")
                    {
                        bool cancelOrderInCheckOut = await _payPalGateWayService.DeleteCheckOutOrderOfPackages(getOrder.Id);
                    }
                    await postUpdatePackage(getOrder.Id, getOrder.PackageId.Value, getOrder.CustomerId, getOrder.StartDateTime, getOrder.EndDateTime);
                }
            }
            if (reasonStatus == "Accept")
            {
                OrderReason.IsActive = 1;
            }
            else
            {
                OrderReason.IsActive = 3;
            }
            OrderReason.UpdatedAt = GeneralPurpose.DateTimeNow();
            var orderReasons = await orderReasonRepo.UpdateOrderReason(OrderReason);
            var chkOrderUpdated = await orderRepo.UpdateOrder(getOrder);
            ViewModelMessageChatBox orderDeliverObj = new ViewModelMessageChatBox
            {
                MessageDescription = generalOrderReason,
                OrderReasonId = OrderReason.Id.ToString(),
                OrderReasonStatus = generalStatusReason,
                OrderReasonType = orderStatus,
            };
            int TypeOfNotification = 0;
            string NotificationTitle = "";
            string NotificationDescription = "";

            Dictionary<(string, string), (int, string, string)> notificationMappings = new Dictionary<(string, string), (int, string, string)>
            {
                {("Cancel", "Accept"), ((int)NotificationType.OrderCancelled, "Order Cancelled", "Your Request to cancel this order has been accepted")},
                {("Cancel", "Reject"), ((int)NotificationType.OrderCancellationRejected, "Cancellation Rejected", "Your Request to cancel this order has been declined")},
                {("Extend", "Accept"), ((int)NotificationType.DateExtended, "Date Extended", "Your Request to extend this order date has been accepted")},
                {("Extend", "Reject"), ((int)NotificationType.DateExtensionRejected, "Date Extension Rejected", "Your Request to cancel this order has been accepted")}
            };

            if (notificationMappings.TryGetValue((orderStatus, reasonStatus), out var notificationValues))
            {
                TypeOfNotification = notificationValues.Item1;
                NotificationTitle = notificationValues.Item2;
                NotificationDescription = notificationValues.Item3;
            }


            Notification notificationObj2 = new Notification
            {
                UserId = Convert.ToInt32(receiverId),
                Title = NotificationTitle,
                IsRead = 0,
                IsActive = (int)EnumActiveStatus.Active,
                Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)getOrder.Id)}",
                CreatedAt = GeneralPurpose.DateTimeNow(),
                Description = NotificationDescription,
                NotificationType = TypeOfNotification
            };
            bool isNotification = await _notificationService.AddNotification(notificationObj2);
            if (isNotification)
            {
                await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", receiverId);
            }

            await _notificationHubSocket.Clients.All.SendAsync("UpdateOrderStatus", senderId.ToString(), OrderReason.Id.ToString(), generalStatusReason, orderStatus, "");

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Data = orderDeliverObj });
        }

        [HttpPut("PostExtendDeadline")]
        public async Task<IActionResult> PostExtendDeadline(string orderId, string orderReasonId,
            string orderStatus, string senderId, string receiverId, string datetime)
        {
            var datetimes = Convert.ToDateTime(datetime);
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(senderId));

            var getOrderReason = await orderReasonRepo.GetOrderReasonById(Convert.ToInt32(orderReasonId));
            var getMessage = new Message();
            getMessage.SenderId = Convert.ToInt32(senderId);
            getMessage.ReceiverId = Convert.ToInt32(receiverId);
            getMessage.OrderId = Convert.ToInt32(orderId);
            getOrderReason.IsActive = 2;

            if (orderStatus == "Accept")
            {
                getMessage.MessageDescription = "Extention Date Has been Approved ";
                await PostExtendOrderDate((int)getOrderReason.OrderId, datetimes.ToString());
            }
            else
            {
                getMessage.MessageDescription = "Extention Has Not been Approved ";
            }
            getOrderReason.UpdatedAt = GeneralPurpose.DateTimeNow();
            await orderReasonRepo.UpdateOrderReason(getOrderReason);
            getMessage.OrderReasonId = getOrderReason.Id;
            var message = await PostAddOrderReasonMessage(getMessage);

            string msgTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone);
            string userName = getLoggedInUser.UserName;
            string profile = getLoggedInUser.ProfilePicture;

            ReceiveOrderMessageDto receiveOrderMessageDto = new ReceiveOrderMessageDto()
            {
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                userName = userName,
                userProfile = profile,
                message = message.MessageDescription,
                messageTime = msgTime,
            };
            await _notificationHubSocket.Clients.All.SendAsync("ReceiveOrderMessage", receiveOrderMessageDto);

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = receiveOrderMessageDto.message });
        }

        private static int GetLastNumberOfString(string input)
        {
            string pattern = @"\d+$";

            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                if (int.TryParse(match.Value, out int result))
                {
                    return result;
                }
            }
            return -1;
        }

        private async Task<bool> PostExtendOrderDate(int orderId, string EndDate)
        {
            var getOrder = await orderRepo.GetOrderById(orderId);
            getOrder.EndDateTime = Convert.ToDateTime(EndDate);
            await orderRepo.UpdateOrder(getOrder);
            return true;
        }

        private async Task<Message> PostAddOrderReasonMessage(Message message)
        {
            var messages = new Message();
            messages.MessageDescription = message.MessageDescription;
            messages.SenderId = message.SenderId;
            messages.OrderId = message.OrderId;
            messages.ReceiverId = message.ReceiverId;
            messages.OrderReasonId = message.OrderReasonId;
            messages.IsZoomMessage = message.IsZoomMessage;
            messages.IsActive = 1;
            messages.IsRead = 0;
            messages.CreatedAt = GeneralPurpose.DateTimeNow();
            await messagesRepo.AddMessage(messages);
            await messagesRepo.saveChangesFunction();
            return messages;
        }

        private string ExtractPart(string inputString, int partNumber)
        {
            int index = inputString.IndexOf(":ZoomLink:", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                if (partNumber == 1)
                {
                    return inputString.Substring(0, index);
                }
                else if (partNumber == 2)
                {
                    return inputString.Substring(index + ":ZoomLink:".Length);
                }
            }
            // Return the original string if ":ZoomLink:" is not found or partNumber is invalid
            return "";
        }

        #endregion

        #region Zoom
        private async Task<string> AuthorizationHeader(string CliendId, string SecretKey)
        {
            var AuthorizationHeader = System.Text.Encoding.UTF8.GetBytes($"{CliendId}:{SecretKey}");
            var encoded = Convert.ToBase64String(AuthorizationHeader);
            return $"Basic {encoded}";
        }

        [HttpGet("GetLoginWithAccountId")]
        public async Task<string> GetLoginWithAccountId(string AccountId, string ClientId, string ClientSecret)
        {
            RestClient client = new RestClient();
            var request = new RestRequest();
            var Url = "https://zoom.us/oauth/token?grant_type=account_credentials&account_id=" + AccountId;

            client = new RestClient(Url);
            var AuthorizationHeaders = await AuthorizationHeader(ClientId, ClientSecret);
            request.AddHeader("Authorization", string.Format(AuthorizationHeaders));
            var response = client.Post(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var getToken = response.Content.Split('"');
                var getTokens = getToken[3];
                return getTokens.ToString();
            }
            return null;
        }

        [HttpPost("CreateZoomMeeting")]
        public async Task<IActionResult> CreateZoomMeeting(string ReceiverId = "", string SenderId = "", string OrderId = "")
        {
            var getToken = await GetLoginWithAccountId(GlobalMessages.ZoomAccountId, GlobalMessages.ZoomClientId, GlobalMessages.ZoomClientSecret);
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(SenderId));
            if (getToken != null)
            {
                var getDateTimeForZoom = GeneralPurpose.DateTimeNow();
                var setMeeting = new JObject();
                setMeeting["topic"] = "Zoom Meeting";
                setMeeting["start_time"] = getDateTimeForZoom;
                setMeeting["duration"] = "40";
                setMeeting["type"] = "2";
                setMeeting["waiting_room"] = true;
                setMeeting["join_before_host"] = false;

                var modal = JsonConvert.SerializeObject(setMeeting);

                RestRequest request = new RestRequest();
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", string.Format("Bearer {0}", getToken));
                request.AddParameter("application/json", modal, ParameterType.RequestBody);

                RestClient client = new RestClient();
                //var Url = string.Format("https://api.zoom.us/v2/users/" + GetUserDetail + "/meetings");
                var Url = string.Format("https://api.zoom.us/v2/users/me/meetings");
                client = new RestClient(Url);

                var response = client.Post(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    ZoomMeetingResponse zoomMeetingResponse = JsonConvert.DeserializeObject<ZoomMeetingResponse>(response.Content.ToString());
                    var getMessage = new Message();
                    getMessage.SenderId = Convert.ToInt32(SenderId);
                    getMessage.ReceiverId = Convert.ToInt32(ReceiverId);
                    getMessage.OrderId = Convert.ToInt32(OrderId);
                    getMessage.MessageDescription = zoomMeetingResponse.start_url + ":ZoomLink:" + zoomMeetingResponse.join_url;
                    getMessage.IsZoomMessage = 1;
                    getMessage.CreatedAt = GeneralPurpose.DateTimeNow();

                    var message = await PostAddOrderReasonMessage(getMessage);

                    string msgTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone);
                    string userName = getLoggedInUser.UserName;
                    string profile = getLoggedInUser.ProfilePicture;


                    ReceiveOrderMessageDto receiveOrderMessageDto = new ReceiveOrderMessageDto()
                    {
                        senderId = message.SenderId,
                        receiverId = message.ReceiverId,
                        userName = userName,
                        userProfile = profile,
                        message = "Click the link to Open Zoom Meeting",
                        reasonType = "Zoom",
                        messageTime = msgTime,
                        newOrderReasonId = "",
                        StartUrl = zoomMeetingResponse.start_url,
                        JoinUrl = zoomMeetingResponse.join_url,
                    };
                    Notification notificationObj1 = new Notification
                    {
                        UserId = message.SenderId,
                        Title = "Zoom Meeting",
                        IsRead = 0,
                        IsActive = (int)EnumActiveStatus.Active,
                        Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)message.OrderId)}",
                        CreatedAt = GeneralPurpose.DateTimeNow(),
                        Description = "You created new zoom meeting",
                        NotificationType = (int)NotificationType.ZoomMeetingCreated
                    };                   
                    Notification notificationObj2 = new Notification
                    {
                        UserId = message.ReceiverId,
                        Title = "Zoom Meeting Created",
                        IsRead = 0,
                        IsActive = (int)EnumActiveStatus.Active,
                        Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)message.OrderId)}",
                        CreatedAt = GeneralPurpose.DateTimeNow(),
                        Description = "Zoom meeting has been created for your Order.",
                        NotificationType = (int)NotificationType.ZoomMeetingCreated
                    };

                    bool isNotification = await _notificationService.AddNotification(notificationObj1);
                    bool isNotification2 = await _notificationService.AddNotification(notificationObj2);
                    await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.ReceiverId.ToString());
                    await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.SenderId.ToString());
                    await _notificationHubSocket.Clients.All.SendAsync("ReceiveOrderMessage", receiveOrderMessageDto);
                    return Ok(new { Status = true, Message = "Zoom Meeting Created Successfully", Profile = profile, userName = userName, MessageTime = msgTime, StartUrl = zoomMeetingResponse.start_url, JoinUrl = zoomMeetingResponse.join_url });
                }
                else
                {
                    return Ok(new { Status = false, Message = "Failed To Create Zoom Meeting" });
                }
            }
            return Ok(new { Status = false, Message = "There is an error while creating Zoom Meeting" });
        }

        #endregion

        #region Order
        [HttpPut("UploadOrderWork")]
        public async Task<IActionResult> UploadOrderWork(IFormFile file, [FromQuery] OrderDeliverDto WorkUploadDto)
        {
            try
            {
                var ValetId = Convert.ToInt32(WorkUploadDto.ValetId);
                var CustomerId = Convert.ToInt32(WorkUploadDto.CustomerId);
                var OrderId = StringCipher.DecryptId(WorkUploadDto.OrderId);
                var getOrder = await orderRepo.GetOrderById(OrderId);
                var filePath = await UploadFiles(file, "OrderDeliverable");
                
                Message OrderDelieverMessage = new Message
                {
                    MessageDescription = WorkUploadDto.Description,
                    IsRead = 0,
                    FilePath = filePath,
                    SenderId = ValetId,
                    ReceiverId = CustomerId,
                    OrderId = OrderId,
                    IsActive = 1,
                    CreatedAt = GeneralPurpose.DateTimeNow(),
                };
                await messagesRepo.AddMessage(OrderDelieverMessage);
                if (!await messagesRepo.saveChangesFunction())
                {
                    return Ok(new FilePathResponseDto() { FilePath = "", Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }

                getOrder.IsDelivered = 1;
                getOrder.UpdatedAt = GeneralPurpose.DateTimeNow();
                var updateOrder = await orderRepo.UpdateOrder(getOrder);

                return Ok(new FilePathResponseDto() { FilePath = filePath, Status = true, StatusCode = "200", Message = "Image Updated Successfully" });
            }
            catch (Exception ex)
            {
                await MailSender.SendErrorMessage("Environment:" + projectVariables.BaseUrl + "<br> Error Message:" + ex.Message.ToString() + "<br> Stack Trace:" + ex.StackTrace);
                return Ok(new FilePathResponseDto() { FilePath = "", Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("GetBookedSlots")]
        public async Task<IActionResult> GetBookedSlots(int valetID)
        {
            try
            {
                var bookedSlots = await orderRepo.GetBookedSlotsTime(valetID);
                if(bookedSlots.Count > 0)
                {
                    return Ok(new ResponseDto() { Status = true, StatusCode = "200", Data = bookedSlots });
                }
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Record not Found" });
            } 
            catch(Exception ex)
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "404", Message = GlobalMessages.SystemFailureMessage });
            }
        }

        [HttpGet("CheckAvailableSlot")]
        public async Task<IActionResult> CheckAvailableSlot(int valetID, DateTime startDate, DateTime endDate)
        {
            try
            {
                UserClaims? getUsetFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                if (getUsetFromToken.Role != "Valet" && !string.IsNullOrEmpty(getUsetFromToken.Timezone))
                {
                    var getValet = await userRepo.GetUserById(valetID);
                    startDate = DateTimeHelper.GetUtcTimeFromZoned(startDate, getValet?.Timezone);
                    endDate = DateTimeHelper.GetUtcTimeFromZoned(endDate, getValet?.Timezone);
                    startDate = DateTimeHelper.GetZonedDateTimeFromUtc(startDate, getUsetFromToken?.Timezone);
                    endDate = DateTimeHelper.GetZonedDateTimeFromUtc(endDate, getUsetFromToken?.Timezone);
                    
                }

                bool isSlotAvailable = await orderRepo.CheckAvailability(valetID, startDate, endDate);
                bool isDateAvailable = await userRepo.IsDateRangeAvailable(valetID, startDate, endDate);

                if (isSlotAvailable && isDateAvailable)
                {
                    // Both slot and valet are available
                    return Ok(new ResponseDto
                    {
                        Status = true,
                        StatusCode = "200",
                        Message = "Slot is available"
                    });
                }
                else if (isSlotAvailable)
                {
                    // Slot is available, but valet is not
                    return Ok(new ResponseDto
                    {
                        Status = false,
                        StatusCode = "400",
                        Message = "Valet is not available at this slot time. Please check valet availability."
                    });
                }
                else if (isDateAvailable)
                {
                    // Valet is available, but the slot is not
                    return Ok(new ResponseDto
                    {
                        Status = false,
                        StatusCode = "400",
                        Message = "This time slot is already booked or unavailable. Please choose another time."
                    });
                }
                else
                {
                    // Neither slot nor valet are available
                    return Ok(new ResponseDto
                    {
                        Status = false,
                        StatusCode = "400",
                        Message = "Neither slot nor valet is available at this time. Please choose another time."
                    });
                }
            }
            catch (Exception ex)
            {

                return Ok(new ResponseDto
                {
                    Status = false,
                    StatusCode = "500",
                    Message = "Internal server error"
                });
            }
        }


        //[HttpPost("AcceptOrder")]
        //public async Task<IActionResult> AcceptOrder(OrderDeliverDto orderDeliverDto)
        //{
        //    var OrderId = StringCipher.DecryptId(orderDeliverDto.OrderId);
        //    var getOrder = await orderRepo.GetOrderById(OrderId);
        //    getOrder.IsDelivered = 2;
        //    getOrder.OrderStatus = 1;
        //    getOrder.UpdatedAt = GeneralPurpose.DateTimeNow();
            
        //    UserRating userRating = new UserRating();
        //    userRating.OrderId = getOrder.Id;
        //    userRating.Reviews = orderDeliverDto.Reviews;
        //    userRating.Stars = Convert.ToInt32(orderDeliverDto.Stars);
        //    userRating.CustomerId = getOrder.CustomerId;
        //    userRating.ValetId = getOrder.ValetId;

        //    if (await orderRepo.UpdateOrder(getOrder))
        //    {
        //        await PostUserRating(userRating);
        //        var GetValet = await userRepo.GetUserById(getOrder.ValetId.Value);
        //        if (GetValet != null)
        //        {
        //            var TransferToSeller = await userRepo.TransferFunds(GetValet.StripeId, (long)getOrder.OrderPrice);
        //            if (TransferToSeller == true)
        //            {
        //                await orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.SentToValet);
        //                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Order Is Accepted Successfully"});
        //            }
        //            else
        //            {
        //                await orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.PaymentFailedToSend);
        //            }
        //        }
        //    }
        //    return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
        //}

        //private async Task<bool> PostUserRating(UserRating userRating)
        //{
        //    UserRating rating = new UserRating();
        //    rating.OrderId = userRating.OrderId;
        //    rating.Reviews = userRating.Reviews;
        //    rating.Stars = userRating.Stars;
        //    rating.CustomerId = userRating.CustomerId;
        //    rating.ValetId = userRating.ValetId;
        //    rating.CreatedAt = GeneralPurpose.DateTimeNow();
        //    if(await userRatingRepo.AddUserRating(rating))
        //    {
        //        if(!await UpdateRating((int)rating.ValetId, (int)rating.Stars))
        //        {
        //            return false;
        //        }
        //        return true;
        //    }
        //    return false; 
        //}

        //private async Task<bool> UpdateRating(int userId, int stars)
        //{
        //    var user = await userRepo.GetUserById((int)userId);
        //    if (user != null)
        //    {
        //        if (stars == 5)
        //        {
        //            user.StarsCount = user.StarsCount != null ? user.StarsCount + 1 : 1;
        //        }
        //        user.AverageRating = await GetAverageRating(user.Id);
        //        if (user.StarsCount >= 5 && user.StarsCount < 10)
        //        {
        //            if ((double)user.AverageRating >= 4.8)
        //            {
        //                user.PricePerHour = Convert.ToDecimal(29.99);
        //            }
        //        }
        //        if (user.StarsCount >= 10)
        //        {
        //            if ((double)user.AverageRating >= 4.8)
        //            {
        //                user.PricePerHour = Convert.ToDecimal(34.99);
        //            }
        //        }
        //    }
        //    if (!await userRepo.UpdateUser(user))
        //    {
        //        return false; 
        //    }
        //    return true;
        //}

        //private async Task<decimal> GetAverageRating(int userId)
        //{
        //    try
        //    {
        //        var valetRatingList = await userRatingRepo.GetUserRatingListByUserId(userId);
        //        valetRatingList = valetRatingList.Where(x => x.ValetId == userId).ToList();

        //        int totalRatings = valetRatingList.Count();
        //        int totalStars = (int)valetRatingList.Sum(x => x.Stars);
        //        decimal averageRating = totalStars / totalRatings;
        //        return averageRating;
        //    }
        //    catch (Exception ex)
        //    {
        //        return -1;
        //    }
        //}


        [HttpPost("AcceptOrder")]
        public async Task<IActionResult> AcceptOrder(OrderDeliverDto orderDeliverDto)
        {
            var decryptedOrderId = StringCipher.DecryptId(orderDeliverDto.OrderId);
            var order = await orderRepo.GetOrderById(decryptedOrderId);
            UpdateOrderDetails(order, orderDeliverDto);

            if (await orderRepo.UpdateOrder(order))
            {
                await ProcessUserRating(order, orderDeliverDto);
                var valet = await userRepo.GetUserById(order.ValetId.Value);
                if (valet != null)
                {
                    var transferSuccess = await userRepo.TransferFunds(valet.StripeId, (decimal)order.OrderPrice);
                    if (transferSuccess)
                    {
                        await orderRepo.ChangeStripePaymentStatus(decryptedOrderId, StripePaymentStatus.SentToValet);
                        return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "Order Is Accepted Successfully" });
                    }
                    else
                    {
                        await orderRepo.ChangeStripePaymentStatus(decryptedOrderId, StripePaymentStatus.PaymentFailedToSend);
                    }
                }
            }
            return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
        }

        private async Task ProcessUserRating(Order order, OrderDeliverDto orderDeliverDto)
        {
            var userRating = CreateUserRating(order, orderDeliverDto);
            if (userRating.Stars != null || userRating.Reviews != null)
            {
                if (await userRatingRepo.AddUserRating(userRating))
                {
                    await UpdateUserRating((int)userRating?.ValetId, (int)userRating?.Stars);
                }
            }
        }

        private void UpdateOrderDetails(Order order, OrderDeliverDto orderDeliverDto)
        {
            order.IsDelivered = 2;
            order.OrderStatus = 1;
            order.UpdatedAt = GeneralPurpose.DateTimeNow();
        }

        private UserRating CreateUserRating(Order order, OrderDeliverDto orderDeliverDto)
        {
            return new UserRating
            {
                OrderId = order.Id,
                Reviews = orderDeliverDto.Reviews,
                Stars = orderDeliverDto.Stars != null ? Convert.ToInt32(orderDeliverDto.Stars) : null,
                CustomerId = order.CustomerId,
                ValetId = order.ValetId
            };
        }

        private async Task UpdateUserRating(int valetId, int stars)
        {
            var user = await userRepo.GetUserById(valetId);
            if (user != null)
            {
                UpdateRatingBasedOnStarsAsync(user, stars);
                await userRepo.UpdateUser(user);
            }
        }

        private async Task UpdateRatingBasedOnStarsAsync(User user, int stars)
        {
            if (stars == 5)
            {
                user.StarsCount = user.StarsCount != null ? user.StarsCount + 1 : 1;
            }
            user.AverageRating = user.StarsCount != 0 ? await GetAverageRating(user.Id) : 0;

            if (user.StarsCount >= 5 && user.StarsCount < 10 && user.AverageRating >= Convert.ToDecimal(4.8))
            {
                user.PricePerHour = Convert.ToDecimal(29.99);
                user.HST = 17;
            }
            else if (user.StarsCount >= 10 && user.AverageRating >= Convert.ToDecimal(4.8))
            {
                user.PricePerHour = Convert.ToDecimal(34.99);
                user.HST = 15;
            }
            else
            {
                user.PricePerHour = Convert.ToDecimal(24.99);
                user.HST = 13;
            }
        }

        private async Task<decimal> GetAverageRating(int userId)
        {
            var valetRatingList = await userRatingRepo.GetUserRatingListByUserId(userId);
            valetRatingList = valetRatingList.Where(x => x.ValetId == userId).ToList();

            if (valetRatingList.Any())
            {
                int totalRatings = valetRatingList.Count();
                int totalStars = (int)valetRatingList.Sum(x => x.Stars);
                return totalStars / totalRatings;
            }
            return 0;
        }

        #region OrderRevision
        [HttpPut("PostUpdateOrderStatus")]
        public async Task<IActionResult> PostUpdateOrderStatus(string? OrderId, string senderId, string receiverId, string RevisionExplanation = "")
        {
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(senderId));
            var getOrder = await orderRepo.GetOrderById(Convert.ToInt32(OrderId));
            getOrder.IsDelivered = 0;
            getOrder.UpdatedAt = null;

            var OrderReason = new OrderReason();
            OrderReason.OrderId = getOrder.Id;
            OrderReason.ReasonExplanation = RevisionExplanation;
            OrderReason.ReasonType = 2;
            OrderReason.IsActive = 1;
            var orderReasons = await orderReasonRepo.AddOrderReason(OrderReason);
            var getMessage = new Message();

            getMessage.SenderId = Convert.ToInt32(senderId);
            getMessage.ReceiverId = Convert.ToInt32(receiverId);
            getMessage.OrderId = getOrder.Id;
            getMessage.OrderReasonId = orderReasons.Id;
            getMessage.MessageDescription = RevisionExplanation;

            var message = await PostAddOrderReasonMessage(getMessage);
            if (!await orderRepo.UpdateOrder(getOrder))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
            }

            string msgTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser.Timezone);
            string userName = getLoggedInUser.UserName;
            string profile = getLoggedInUser.ProfilePicture;
            string newOrderReasonId = orderReasons.Id.ToString();
            ReceiveOrderMessageDto receiveOrderMessageDto = new ReceiveOrderMessageDto()
            {
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                userName = userName,
                userProfile = profile,
                message = message.MessageDescription,
                messageTime = msgTime,
                newOrderReasonId = newOrderReasonId,
                reasonType = "Revision"
            };
            Notification notificationObj = new Notification
            {
                UserId = Convert.ToInt32(receiverId),
                Title = "Revision Requested",
                IsRead = 0,
                IsActive = (int)EnumActiveStatus.Active,
                Url = $"{projectVariables.BaseUrl}User/OrderDetail?orderId={StringCipher.EncryptId((int)getOrder.Id)}",
                CreatedAt = GeneralPurpose.DateTimeNow(),
                Description = "Client Requested a revision for your order.",
                NotificationType = (int)NotificationType.OrderCancellationRequested
            };
            bool isNotification = await _notificationService.AddNotification(notificationObj);
            await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications", message.ReceiverId.ToString());

            await _notificationHubSocket.Clients.All.SendAsync("ReceiveOrderMessage", receiveOrderMessageDto);
            return Ok(new { Status = true, UserName = userName, Profile = profile, Message = message.MessageDescription, MessageTime = msgTime, NewOrderReasonId = orderReasons.Id.ToString() });
        }
        #endregion

        #endregion

        #region refund payment
        private async Task<bool> RefundPayment(string chargeId, int OrderId)
        {
            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                Charge = chargeId,
            };

            var refund = refundService.Create(refundOptions);
            if (refund.Status == "succeeded")
            {
                bool updateStripeStatus = await orderRepo.ChangeStripePaymentStatus(OrderId, StripePaymentStatus.Refunded);
                return true;
            }
            return false;
        }

        #endregion

        [HttpGet("GetUserReviews")]
        public async Task<ActionResult<UserRatingListDto>> GetUserReviews(string userId)
        {
            List<UserRating> userRating = (List<UserRating>)await userRatingRepo.GetUserRatingListByUserId(StringCipher.DecryptId(userId));

            if (userRating == null)
            {
                return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
            }
            List<UserRatingListDto> userRatingDtoListuserRatingDtoList = new List<UserRatingListDto>();
            foreach (var userRatingObj in userRating)
            {
                Models.User CustomerName = await userRepo.GetUserById((int)userRatingObj.CustomerId);
                UserRatingListDto obj = new UserRatingListDto()
                {
                    Stars = userRatingObj.Stars,
                    UserName = CustomerName.FirstName + " " + CustomerName.LastName,
                    Reviews = userRatingObj.Reviews,
                };
                userRatingDtoListuserRatingDtoList.Add(obj);
            }

            var date = GeneralPurpose.DateTimeNow().Date;

            return Ok(new ResponseDto() { Data = userRatingDtoListuserRatingDtoList, Status = true, StatusCode = "200", Message = "Record Fetch Successfully" });
        }

        private async Task<string> UploadFiles(IFormFile file, string? uploadedFiles = "")
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

        private async Task<bool> postUpdatePackage(int orderId, int? packageId,int? CustomerId,DateTime? startDate, DateTime? endDate)
        {
            // Getting User Current Package


            var getCurrentUserPackage = await userPackageService.GetCurrentUserPackageByUserId(CustomerId);
            
            if (getCurrentUserPackage != null) 
            {

                TimeSpan timeDifference = endDate.Value - startDate.Value;

                // Calculate the ceiling number of hours
                int NoOfSessionRevert = (int)Math.Ceiling(timeDifference.TotalHours);
                var revertUserSession = getCurrentUserPackage.RemainingSessions.Value + NoOfSessionRevert;
                getCurrentUserPackage.RemainingSessions = revertUserSession;

                if (await userPackageService.UpdateUserPackageSession(getCurrentUserPackage))
                {
                    bool updateOrderStatusForCancel = await orderRepo.UpdateOrderStatusForCancel(orderId);
                    bool updateStripeStatus = await orderRepo.ChangeStripePaymentStatus(orderId, StripePaymentStatus.SessionReverted);
                    return updateOrderStatusForCancel;
                }

            }
            
            return false;
        }
    }
}
