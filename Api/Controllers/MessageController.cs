using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Stripe;
using System.Web;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IJwtUtils jwtUtils;
        private readonly IUserRepo userRepo;
        private readonly Zoom _zoomVariables;
        private readonly IOrderRepo orderRepo;
        private readonly IMessagesRepo messagesRepo;
        private readonly IUserRatingRepo userRatingRepo;
        private readonly IOrderReasonRepo orderReasonRepo;
        private readonly ProjectVariables projectVariables;
        private readonly IOfferDetailsRepo offerDetailsRepo;
        private readonly INotificationRepo _notificationService;
        private readonly INotificationService userPackageService;
        private readonly IFundTransferService _fundTransferService;
        private readonly IPayPalGateWayService _payPalGateWayService;
        private readonly IHubContext<NotificationHubSocket> _notificationHubSocket;

        public MessageController(IHubContext<NotificationHubSocket> notificationHubSocket, IUserRepo _userRepo, 
            IMessagesRepo _messagesRepo, IOfferDetailsRepo _offerDetailsRepo, IOptions<ProjectVariables> options, IJwtUtils _jwtUtils, 
            IOrderRepo _orderRepo, IOrderReasonRepo _orderReasonRepo, IFundTransferService fundTransferService, IUserRatingRepo _userRatingRepo,
            IPayPalGateWayService payPalGateWayService, INotificationService _userPackageService, INotificationRepo notificationService,
            IOptions<Zoom> zoomVariables)
        {
            jwtUtils = _jwtUtils;
            userRepo = _userRepo;
            orderRepo = _orderRepo;
            messagesRepo = _messagesRepo;
            userRatingRepo = _userRatingRepo;
            projectVariables = options.Value;
            orderReasonRepo = _orderReasonRepo;
            _zoomVariables = zoomVariables.Value;
            offerDetailsRepo = _offerDetailsRepo;
            userPackageService = _userPackageService;
            _notificationService = notificationService;
            _fundTransferService = fundTransferService;
            _payPalGateWayService = payPalGateWayService;
            _notificationHubSocket = notificationHubSocket;
        }

        [HttpPost("SendMessageToClients")]
        public async Task<IActionResult> SendMessageToClients(string userId, string message)
        {
            await _notificationHubSocket.Clients.All.SendAsync("ReceiveMessage", userId, message);
            return Ok(message);
        }
        
        #region ForReact
        [HttpGet("GetReceiverStatuses/{userId}")]
        public async Task<IActionResult> GetReceiverStatuss(string userId)
        {
            try
            {
                var decrypt = StringCipher.DecryptionId(userId);
                var user = await userRepo.GetUserById(decrypt);

                if (user == null)
                {
                    return NotFound(new ResponseDto() { Status = false, StatusCode = "404", Message = "No record found." });
                }
                return Ok(new ResponseDto() { Status = true, StatusCode = "200", Data= user.Status });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost("PostAddMessages")]
        public async Task<IActionResult> PostAddMessages(PostAddMessage postAddMessage)
        {
            var getLoggedInUser = await userRepo.GetUserById(Convert.ToInt32(postAddMessage.SenderId));
            var receiver = await userRepo.GetUserById(Convert.ToInt32(postAddMessage.ReceiverId));
            var isWayUserProfile = postAddMessage.Way == "ViewUserProfile";
            var messageList = new List<Message>();
            var message = new Message();

            if (isWayUserProfile)
            {
                messageList = (List<Message>)await messagesRepo.GetMessageBySenderIdAndRecieverId(
                    getLoggedInUser!.Id,
                    Convert.ToInt32(receiver!.Id)
                );

                if (messageList?.Count > 0)
                {
                    return Ok(new ResponseDto
                    {
                        Id = StringCipher.EncryptId(Convert.ToInt32(postAddMessage.ReceiverId)),
                        Status = true,
                        StatusCode = "200",
                        Message = "Exist"
                    });
                }

                postAddMessage.MessageDescription = getLoggedInUser.Role == 4
                    ? "Hi! I am interested in your project! Please reach me back asap. Thanks!"
                    : "Hi! I am interested in your services! Please reach me back asap. Thanks!";

                await CreateMessage(postAddMessage, message);

                if (!await messagesRepo.saveChangesFunction())
                {
                    return Ok(new ResponseDto
                    {
                        Status = false,
                        StatusCode = "500",
                        Message = "User unavailable. Try again after some time."
                    });
                }

                await AddNotification(message, "Message Received", "You just received a message.", "messages", "");

                return Ok(new ResponseDto
                {
                    Id = StringCipher.EncryptId(Convert.ToInt32(postAddMessage.ReceiverId)),
                    Status = true,
                    StatusCode = "200",
                    Message = "Message Sent Successfully."
                });
            }
            else if (!string.IsNullOrEmpty(postAddMessage.MessageDescription))
            {
                await CreateMessage(postAddMessage, message);

                if (!await messagesRepo.saveChangesFunction())
                    return Ok("Failed to send/add message.");

                await AddNotification(message, "Message Received", "You just received a message.", "messages", "");

                if (message.Id != 0)
                {
                    var offer = new OfferDetail();

                    var pricePerHour = (getLoggedInUser?.Role == (int)EnumRoles.Valet ? getLoggedInUser?.PricePerHour :
                   (receiver?.Role == (int)EnumRoles.Valet ? receiver?.PricePerHour : 0));


                    if (!string.IsNullOrEmpty(postAddMessage.OfferTitle))
                        offer = await CreateOffer(postAddMessage, message, getLoggedInUser!, (decimal)pricePerHour!);
                    
                    var model = await NotifyOffer(offer, message, getLoggedInUser!, receiver!);
                    var data = new
                    {
                        model,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                    };

                    return Ok(new ResponseDto()
                    {
                        Status = true,
                        StatusCode = "200",
                        Data = data
                    });
                }
            }

            return Ok(new ResponseDto
            {
                Status = false,
                StatusCode = "400",
                Message = "Message cannot be Empty"
            });
        }

        [HttpPut("UpdateOrderOfferStatus")]
        public async Task<IActionResult> UpdateOrderOfferStatus(PostUpdateMessage postAddMessage)
        {
            try
            {
                // Retrieve the offer details
                var offerDetails = await offerDetailsRepo.GetOfferDetailById(Convert.ToInt32(postAddMessage.OfferDetailId));
                if (offerDetails == null)
                {
                    return NotFound(new ResponseDto { Status = false, StatusCode = "404", Message = "Offer not found." });
                }

                string offerStatus = "";
                if (postAddMessage.MessageDescription == "Accept")
                {
                    offerDetails.OfferStatus = 2; // Accepted
                    offerStatus = "accepted";
                }
                else if (postAddMessage.MessageDescription == "Reject")
                {
                    offerDetails.OfferStatus = 3; // Rejected
                    offerStatus = "rejected";
                }

                offerDetails.UpdatedAt = GeneralPurpose.DateTimeNow();
                if (!await offerDetailsRepo.UpdateOfferDetail(offerDetails))
                {
                    return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }

                // Update the associated message
                var message = await messagesRepo.GetMessageById((int)offerDetails?.MessageId!);
                if (message == null)
                {
                    return NotFound(new ResponseDto { Status = false, StatusCode = "404", Message = "Message not found." });
                }

                message.MessageDescription = postAddMessage.MessageDescription;
                message.UpdatedAt = GeneralPurpose.DateTimeNow();

                if (!await messagesRepo.UpdateMessage(message))
                {
                    return Ok(new ResponseDto { Status = false, StatusCode = "400", Message = GlobalMessages.SystemFailureMessage });
                }

                // Notify the receiver
                var getReceiverUser = await userRepo.GetUserById(Convert.ToInt32(message.ReceiverId));
                var getSenderUser = await userRepo.GetUserById(Convert.ToInt32(message.SenderId));

                var model = await NotifyOffer(offerDetails, message, getSenderUser!, getReceiverUser!);

                // Prepare and return the response
                var data = new
                {
                    model,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                };

                return Ok(new ResponseDto
                {
                    Status = true,
                    StatusCode = "200",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto { Status = false, StatusCode = "500", Message = GlobalMessages.SystemFailureMessage, Data = ex.Message });
            }
        }

        [HttpGet("GetMessageSideBarLists/{userId}")]
        public async Task<IActionResult> GetMessageSideBarLists(string? userId, string? Name = "",
            string? GetUserChatOnTop = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    var decrypt = StringCipher.DecryptionId(userId);
                    var getLoggedInUser = await userRepo.GetUserById(decrypt);

                    var decryptUserChat = 0;
                    var Sender = new User();
                    var Receiver = new User();
                    if (!string.IsNullOrEmpty(GetUserChatOnTop) && GetUserChatOnTop != "undefined" && GetUserChatOnTop != "null") 
                    {
                        decryptUserChat = StringCipher.DecryptionId(GetUserChatOnTop);
                        var getUserMEssages = await messagesRepo.GetMessageBySenderIdAndRecieverId(decrypt, decryptUserChat);
                        if(getUserMEssages.Count() == 0)
                        {
                            var postAddMessage = new PostAddMessage()
                            {
                                SenderId = decrypt.ToString(),
                                ReceiverId = decryptUserChat.ToString(),
                                Way = "ViewUserProfile",
                            };
                            await PostAddMessages(postAddMessage);
                        }
                    }
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
                    if (!string.IsNullOrEmpty(GetUserChatOnTop) && GetUserChatOnTop != "null" && GetUserChatOnTop != "undefined")
                    {
                        // ID to move to the top
                        string idToMoveToTop = decryptUserChat.ToString();
                        // Reordering the list
                        messagesList = messagesList.OrderBy(user => user.SenderId == idToMoveToTop || user.ReceiverId == idToMoveToTop ? 0 : 1)
                                                  .ThenBy(user => user.Id)
                                                  .ToList();
                    }
                    else
                    {
                        GetUserChatOnTop = "";
                    }

                    if (!String.IsNullOrWhiteSpace(Name))
                    {
                        messagesList = messagesList.Where(a => a.Username.ToLower().Contains(Name.ToLower())).ToList();
                    }
                    if(string.IsNullOrWhiteSpace(Name) && string.IsNullOrEmpty(GetUserChatOnTop))
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

        [HttpGet("GetMessagesForUsers/{userId}")]
        public async Task<IActionResult> GetMessagesForUsers(string? userId, string? messageUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(messageUserId))
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var decrypt = StringCipher.DecryptionId(userId);
                var targerDecrypt = StringCipher.DecryptionId(messageUserId);

                var loggedInUser = await userRepo.GetUserById(decrypt);
                var targetUser = await userRepo.GetUserById(targerDecrypt);

                if (loggedInUser == null || targetUser == null)
                {
                    return NotFound(new ResponseDto()
                    {
                        Status = false,
                        StatusCode = "404",
                        Message = "User not found."
                    });
                }

                var messages = await messagesRepo.GetMessageBySenderIdAndRecieverId(loggedInUser.Id, targetUser.Id);
                
                var groupedMessages = messages
                    .Select(message => MapMessageToViewModel(message, loggedInUser, targetUser))
                    .GroupBy(m => Convert.ToDateTime(m.MessageTime).Date) // Grouping by date
                    .OrderBy(g => g.Key) // Sorting latest date first
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.ToList()); // Convert to dictionary for frontend

                return Ok(new ResponseDto()
                {
                    Status = true,
                    StatusCode = "200",
                    Data = groupedMessages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto()
                {
                    Status = false,
                    StatusCode = "500",
                    Message = ex.Message
                });
            }
        }

        #endregion

        #region OrderZone
        [HttpGet("GetMessagesForOrder/{orderId}")]
        public async Task<IActionResult> GetMessagesForOrder(string orderId, string userId)
        {
            try
            {
                var decryptId = StringCipher.DecryptionId(orderId);
                var decryptUserId = StringCipher.DecryptionId(userId);
                var loggedInUser = await userRepo.GetUserById(decryptUserId);
                var order = await orderRepo.GetOrderById(decryptId);
                var messages = await messagesRepo.GetMessageListByOrdrId(order.Id);
                var userCache = new Dictionary<int, User>(); // Cache users to reduce redundant calls
                var messagesList = new List<ViewModelMessageChatBox>();

                foreach (var message in messages)
                {
                    var viewModelMessage = await CreateViewModelMessage(message, loggedInUser, userCache, order);
                    messagesList.Add(viewModelMessage);
                }

                var groupedMessages = messagesList
                    .GroupBy(m => Convert.ToDateTime(m.MessageTime).Date) // Grouping by date
                    .OrderBy(g => g.Key) // Sorting latest date first
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.ToList());

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, groupedMessages));
            }
            catch (Exception ex)
            {
                // Log the error instead of returning null (if logging is implemented)
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", ex.Message, null));
            }
        }

        [HttpPost("PostAddOrderMessage")]
        public async Task<IActionResult> PostAddOrderMessage([FromForm] PostAddMessage postAddMessage)
        {
            try
            {
                var decrypt = StringCipher.DecryptionId(postAddMessage.SenderId!);
                var decryptRecieverId = StringCipher.DecryptionId(postAddMessage.ReceiverId!);
                var decryptOrderId = StringCipher.DecryptionId(postAddMessage.OrderId!);
                var sender= await userRepo.GetUserById(decrypt);
                var receiver = await userRepo.GetUserById(decryptRecieverId);
                var getOrder = await orderRepo.GetOrderById(decryptOrderId);

                postAddMessage.MessageDescription = HttpUtility.UrlDecode(postAddMessage.MessageDescription);
                var message = await MapMessage(postAddMessage, decrypt, decryptRecieverId, getOrder);
                var error = postAddMessage.Way != "cancel" ? GlobalMessages.MessageSentFail : GlobalMessages.OrderDeliverFail;

                await messagesRepo.AddMessage(message);

                if (!await messagesRepo.saveChangesFunction())
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", error, null));
                

                if (message.Id != 0)
                {
                    var orderMessage = MapOrderMessage(message, sender!, postAddMessage.Way);
                    await AddNotification(message, "Message Received", "You just received a message for your order.",
                        $"order-details/{HttpUtility.UrlDecode(postAddMessage.OrderId!)}", postAddMessage.Way!);
                    
                    var userCache = new Dictionary<int, User>();
                    var viewModelMessage = await CreateViewModelMessage(message, sender!, userCache, getOrder);
                    viewModelMessage.SenderId = decrypt.ToString();
                    viewModelMessage.ReceiverId = decryptRecieverId.ToString();

                    viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                    viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
                }
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", error, null));
            }
            catch (Exception ex)
            {
                var error = postAddMessage.Way != "cancel" ? GlobalMessages.MessageSentFail : GlobalMessages.OrderDeliverFail;
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", error, null));
            }
        }

        [HttpPost("PostDeliverOrder/{orderId}")]
        public async Task<IActionResult> PostDeliverOrder(string orderId, [FromForm] PostAddMessage postAddMessage)
        {
            try
            {
                var decryptOrderId = StringCipher.DecryptionId(orderId);
                var decrypt = StringCipher.DecryptionId(postAddMessage.SenderId!);
                var decryptRecieverId = StringCipher.DecryptionId(postAddMessage.ReceiverId!);

                var sender= await userRepo.GetUserById(decrypt);
                var receiver = await userRepo.GetUserById(decryptRecieverId);
                var getOrder = await orderRepo.GetOrderById(decryptOrderId);
                postAddMessage.Way = "deliver";
                postAddMessage.MessageDescription = HttpUtility.UrlDecode(postAddMessage.MessageDescription);
                var message = await MapMessage(postAddMessage, decrypt, decryptRecieverId, getOrder!);

                await messagesRepo.AddMessage(message);

                if (!await messagesRepo.saveChangesFunction())
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.OrderDeliverFail, null));


                if (message.Id != 0)
                {
                    var orderMessage = MapOrderMessage(message, sender!, postAddMessage.Way);
                    await AddNotification(message, "Message Received", "Your order has been deliver, look at it",
                        $"order-details/{HttpUtility.UrlDecode(orderId!)}", postAddMessage.Way!);

                    var userCache = new Dictionary<int, User>();
                    var viewModelMessage = await CreateViewModelMessage(message, sender!, userCache, getOrder);
                    viewModelMessage.SenderId = decrypt.ToString();
                    viewModelMessage.ReceiverId = decryptRecieverId.ToString();
                    viewModelMessage.IsDelivered = getOrder.IsDelivered.ToString();

                    // Send a notification to connected clients via SignalR
                    viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                    viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
                }
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", GlobalMessages.OrderDeliverFail, null));
            }
            catch (Exception ex)
            {
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", GlobalMessages.OrderDeliverFail, null));
            }
        }

        [HttpPut("RequestExtendDate/{orderId}")]
        public async Task<IActionResult> PostOrderAccept(string orderId, OrderStatusDto obj)
        {
            return await HandleOrderStatusChange(orderId, obj, "Extend Date", "Order Date Extension Requested.", "extension", reasonType: 1);
        }

        [HttpPut("CancelOrder/{orderId}")]
        public async Task<IActionResult> PostOrderCancel(string orderId, OrderStatusDto obj)
        {
            return await HandleOrderStatusChange(orderId, obj, "Cancel Order", "Order cancellation Requested.", "cancel", reasonType: 3);
        }

        [HttpPut("HandleCancelOrderRequest/{orderId}")]
        public async Task<IActionResult> HandleCancelOrderRequest(string orderId, OrderExtentionDto obj)
        {
            // Decrypt input IDs
            var decryptSenderId = StringCipher.DecryptionId(obj.SenderId!);
            var decryptReceiverId = StringCipher.DecryptionId(obj.ReceiverId!);
            var decryptOrderId = StringCipher.DecryptionId(orderId!);
            var decryptOrderReasonId = StringCipher.DecryptionId(obj.OrderReasonId!);

            // Fetch necessary entities
            var order = await orderRepo.GetOrderById(decryptOrderId);
            var sender = await userRepo.GetUserById(decryptSenderId);
            var receiver = await userRepo.GetUserById(decryptReceiverId);
            var getOrderReason = await orderReasonRepo.GetOrderReasonById(decryptOrderReasonId);

            var getMessage = new Message()
            {
                SenderId = decryptSenderId,
                ReceiverId = decryptReceiverId,
                OrderId = decryptOrderId,
                OrderReasonId = decryptOrderReasonId
            };

            if(obj.OrderStatus == "Cancel")
                getOrderReason!.IsActive = 3; //Reject Case
            else
            {
                getOrderReason!.IsActive = 2; //Accept Case
                await ProcessCancellationAsync(order!);
            }

            getOrderReason!.UpdatedAt = GeneralPurpose.DateTimeNow();
            var orderReasons = await orderReasonRepo.UpdateOrderReason(getOrderReason);
            var chkOrderUpdated = await orderRepo.UpdateOrder(order!);

            var title = getOrderReason.IsActive == 3 ? "Cancellation Rejected" : "Order Cancelled";
            getMessage.MessageDescription = getOrderReason.IsActive == 3
                ? "Your Request to cancel this order has been declined"
                : "Your Request to cancel this order has been accepted";

            var message = await PostAddOrderReasonMessage(getMessage);

            await AddNotification(message, title,
                getMessage.MessageDescription,
                $"order-details/{HttpUtility.UrlDecode(orderId)}", "");

            var userCache = new Dictionary<int, User>();
            var viewModelMessage = await CreateViewModelMessage(message, sender!, userCache, order!);
            viewModelMessage.SenderId = decryptSenderId.ToString();
            viewModelMessage.ReceiverId = decryptReceiverId.ToString();
            viewModelMessage.OrderReasonId = decryptOrderReasonId.ToString();
            viewModelMessage.OrderReasonEncId = StringCipher.EncryptId(decryptOrderReasonId);

            // Send a notification to connected clients via SignalR
            viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
            viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

            // Return success response
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
        }

        private async Task ProcessCancellationAsync(Order order)
        {
            if (order.PackageId == null)
            {
                if (order.CapturedId != null)
                {
                    await _fundTransferService.RefundPayment(order.CapturedId, order.Id);
                }
                else
                {
                    var refundSuccess = await RefundPayment(order.StripeChargeId!, order.Id);
                    if (refundSuccess)
                    {
                        await orderRepo.UpdateOrderStatusForCancel(order.Id);
                    }
                }
            }
            else
            {
                if (order.PackageBuyFrom == "PAYPAL")
                {
                    await _payPalGateWayService.DeleteCheckOutOrderOfPackages(order.Id);
                }
                await postUpdatePackage(order.Id, order.PackageId.Value, order.CustomerId, order.StartDateTime, order.EndDateTime);
            }
        }


        [HttpPut("ExtendDateApproval/{orderId}")]
        public async Task<IActionResult> PostExtendDeadline(string orderId, OrderExtentionDto obj)
        {
            // Decrypt input IDs
            var decryptSenderId = StringCipher.DecryptionId(obj.SenderId!);
            var decryptReceiverId = StringCipher.DecryptionId(obj.ReceiverId!);
            var decryptOrderId = StringCipher.DecryptionId(orderId!);
            var decryptOrderReasonId = StringCipher.DecryptionId(obj.OrderReasonId!);

            // Fetch necessary entities
            var order = await orderRepo.GetOrderById(decryptOrderId);
            var sender = await userRepo.GetUserById(decryptSenderId);
            var receiver = await userRepo.GetUserById(decryptReceiverId);
            var getOrderReason = await orderReasonRepo.GetOrderReasonById(decryptOrderReasonId);

            var datetimes = Convert.ToDateTime(obj.DateExtension);

            var getMessage = new Message()
            {
                SenderId = decryptSenderId,
                ReceiverId = decryptReceiverId,
                OrderId = decryptOrderId,
                MessageDescription = "Extention Has Not been Approved ",
                OrderReasonId = decryptOrderReasonId
            };
            var notificationMessage = "";
            if (obj.OrderStatus == "Accept")
            {
                getOrderReason!.IsActive = 2; //Accept Case
                getMessage.MessageDescription = "Extention Date Has been Approved ";
                notificationMessage = "The date you have requested for extention has been accepted";
                await PostExtendOrderDate(decryptOrderId, datetimes.ToString());
            }
            else
            {
                getOrderReason!.IsActive = 3; //Reject Case
                notificationMessage = "The date you have requested for extention has been rejected";
            }

            getOrderReason!.UpdatedAt = GeneralPurpose.DateTimeNow();
            await orderReasonRepo.UpdateOrderReason(getOrderReason);

            var message = await PostAddOrderReasonMessage(getMessage);

            // Add a notification for the action
            await AddNotification(
                message,
                "Order Extention Request",
                notificationMessage,
                $"order-details/{HttpUtility.UrlDecode(orderId)}",
                ""
            );

            // Prepare data for the real-time notification
            var userCache = new Dictionary<int, User>();
            var viewModelMessage = await CreateViewModelMessage(message, sender!, userCache, order!);
            viewModelMessage.SenderId = decryptSenderId.ToString();
            viewModelMessage.ReceiverId = decryptReceiverId.ToString();
            viewModelMessage.OrderReasonId = decryptOrderReasonId.ToString();
            viewModelMessage.OrderReasonEncId = StringCipher.EncryptId(decryptOrderReasonId);

            // Send a notification to connected clients via SignalR
            viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
            viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

            // Return success response
            return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
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
            var decryptReceiver = StringCipher.DecryptionId(ReceiverId);
            var decryptSender = StringCipher.DecryptionId(SenderId);
            var decryptOrder = StringCipher.DecryptionId(OrderId);

            var sender= await userRepo.GetUserById(decryptSender);
            var receiver = await userRepo.GetUserById(decryptReceiver);
            var getOrder = await orderRepo.GetOrderById(decryptOrder);

            var getToken = await GetLoginWithAccountId(_zoomVariables.AccountId!, _zoomVariables.ClientId!, _zoomVariables.ClientSecret!);

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
                var Url = string.Format("https://api.zoom.us/v2/users/me/meetings");
                client = new RestClient(Url);

                var response = client.Post(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    ZoomMeetingResponse zoomMeetingResponse = JsonConvert.DeserializeObject<ZoomMeetingResponse>(response.Content.ToString());
                    var generateURl = zoomMeetingResponse!.start_url + ":ZoomLink:" + zoomMeetingResponse.join_url;
                    
                    var message = MapMessageForZoom(generateURl, decryptSender, decryptReceiver, decryptOrder);
                    var getMessage = await PostAddOrderReasonMessage(message);
                    var receiveOrderMessageDto = MapOrderMessage(message, sender!);
                    receiveOrderMessageDto.message = "Click the link to Open Zoom Meeting";
                    receiveOrderMessageDto.reasonType = "Zoom";
                    receiveOrderMessageDto.StartUrl = zoomMeetingResponse.start_url;
                    receiveOrderMessageDto.JoinUrl = zoomMeetingResponse.join_url;
                    //For Sender
                    await AddNotification(message, "Zoom Meeting", "You created new zoom meeting",
                        $"order-details/{HttpUtility.UrlDecode(OrderId)}", "Zoom Meeting Created");
                    //For Receiver
                    await AddNotification(message, "Zoom Meeting Created", "You created new zoom meeting",
                        $"order-details/{HttpUtility.UrlDecode(OrderId)}", "Zoom Meeting Created");

                    var userCache = new Dictionary<int, User>();
                    var viewModelMessage = await CreateViewModelMessage(message, sender!, userCache, getOrder!);
                    viewModelMessage.SenderId = decryptSender.ToString();
                    viewModelMessage.ReceiverId = decryptReceiver.ToString();
                    viewModelMessage.JoinUrl = receiveOrderMessageDto.JoinUrl;

                    // Send a notification to connected clients via SignalR
                    viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                    viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
                }
                else
                {
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", "Failed To Create Zoom Meeting", null));
                }
            }
            return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", "Failed To Create Zoom Meeting", null));
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

        [HttpPost("AcceptOrder/{orderId}")]
        public async Task<IActionResult> AcceptOrder(string orderId, OrderDeliverViewModel orderDeliverDto)
        {
            try
            {
                var senderId = StringCipher.DecryptionId(orderDeliverDto.SenderId!);
                var receiverId = StringCipher.DecryptionId(orderDeliverDto.ReceiverId!);
                var decryptedOrderId = StringCipher.DecryptionId(orderId);

                var sender = await userRepo.GetUserById(senderId);
                var receiver = await userRepo.GetUserById(receiverId);
                var order = await orderRepo.GetOrderById(decryptedOrderId);
                var message = new Message();

                if (order == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var postMessage = new PostAddMessage
                {
                    OrderId = order!.Id.ToString(),
                    SenderId = senderId.ToString(),
                    ReceiverId = receiverId.ToString(),
                    MessageDescription = "Order Confirmed",
                };

                UpdateOrderDetails(order!);

                if (await orderRepo.UpdateOrder(order!))
                {
                    await ProcessUserRating(order!, orderDeliverDto);
                    var valetId = order.ValetId! == receiverId ? orderDeliverDto.ReceiverId : orderDeliverDto.SenderId;
                    var findValet = order.ValetId! == receiverId ? receiver : sender;
                    if (findValet != null)
                    {

                        var alertMessages = "Order Completed Successfully";
                        var transferSuccess = await userRepo.TransferFunds(findValet.StripeId!, (decimal)order.OrderPrice!);
                        if (transferSuccess)
                        {
                            if (!await orderRepo.ChangeStripePaymentStatus(decryptedOrderId, StripePaymentStatus.SentToValet))
                                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Something Went Wrong, Please try again later"));
                        }
                        else
                        {
                            alertMessages = "Order Is Accepted But there is issue in payment. Contact Support to resolve this";
                            if (!await orderRepo.ChangeStripePaymentStatus(decryptedOrderId, StripePaymentStatus.PaymentFailedToSend))
                                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", "Something Went Wrong, Please try again later"));
                        }

                        await CreateMessage(postMessage, message);

                        await AddNotification(message,
                        "Delivered Order Accepted",
                            $"Congrats, Your order ${order.OrderTitle} has been accepted",
                        $"order-details/${orderId}",
                        "DeliveryAccepted");


                        // Prepare data for the real-time notification
                        var userCache = new Dictionary<int, User>();
                        var viewModelMessage = await CreateViewModelMessage(message, sender, userCache, order);
                        viewModelMessage.SenderId = senderId.ToString();
                        viewModelMessage.ReceiverId = receiverId.ToString();


                        // Send a notification to connected clients via SignalR
                        viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                        viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                        await _notificationHubSocket.Clients.All.SendAsync("SendOrderMessage",
                            viewModelMessage,
                            message.SenderId,
                            message.ReceiverId);

                        return Ok(GeneralPurpose.GenerateResponseCode(true, "200", alertMessages, viewModelMessage));
                    }
                }
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.SystemFailureMessage));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        [HttpPost("PaypalAcceptOrder/{orderId}")]
        public async Task<IActionResult> PaypalAcceptOrder(string orderId, OrderDeliverViewModel orderDeliverDto)
        {
            try
            {
                UserClaims? getUserFromToken = jwtUtils.ValidateToken(Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());
                var senderId = StringCipher.DecryptionId(orderDeliverDto.SenderId!);
                var receiverId = StringCipher.DecryptionId(orderDeliverDto.ReceiverId!);
                var decryptedOrderId = StringCipher.DecryptionId(orderId);

                var sender = await userRepo.GetUserById(senderId);
                var receiver = await userRepo.GetUserById(receiverId);
                var order = await orderRepo.GetOrderById(decryptedOrderId);

                var message = new Message();

                if (order == null)
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound));

                var postMessage = new PostAddMessage
                {
                    OrderId = order?.Id.ToString(),
                    SenderId = senderId.ToString(),
                    ReceiverId = receiverId.ToString(),
                    MessageDescription = "Order Confirmed",
                };

                UpdateOrderDetails(order!);
                var valetId = order.ValetId! == receiverId ? orderDeliverDto.ReceiverId : orderDeliverDto.SenderId;
                var findValet = order.ValetId! == receiverId ? receiver : sender;
                var valetPayPalEmail = await _payPalGateWayService.GetPayPalAccount(valetId!);

                if (await orderRepo.UpdateOrders(order!))
                {
                    await ProcessUserRating(order!, orderDeliverDto);

                    if (order.PackageId != null && order.PackageBuyFrom == "PAYPAL")
                    {
                        if (!await OrderFromPackage(order, findValet, valetPayPalEmail?.Data?.PayPalEmail))
                            return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", GlobalMessages.SystemFailureMessage));
                    }
                    else
                    {
                        // Order from checkout
                        if (!await OrderFromCheckout(order, findValet, valetPayPalEmail?.Data?.PayPalEmail))
                            return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", GlobalMessages.SystemFailureMessage));
                    }

                    await CreateMessage(postMessage, message);

                    await orderRepo.saveChangesFunction();

                    await AddNotification(message, 
                        "Delivered Order Accepted",
                        $"Congrats, Your order ${order.OrderTitle} has been accepted",
                        $"order-details/${orderId}",
                        "DeliveryAccepted");

                    // Prepare data for the real-time notification
                    var userCache = new Dictionary<int, User>();
                    var viewModelMessage = await CreateViewModelMessage(message, sender, userCache, order);
                    viewModelMessage.SenderId = senderId.ToString();
                    viewModelMessage.ReceiverId = receiverId.ToString();


                    // Send a notification to connected clients via SignalR
                    viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                    viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                    await _notificationHubSocket.Clients.All.SendAsync("SendOrderMessage",
                            viewModelMessage,
                            message.SenderId,
                            message.ReceiverId);

                    return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "Order Completed Successfully", viewModelMessage));
                }
                else
                    return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", GlobalMessages.SystemFailureMessage));
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(projectVariables, ex);
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "500", GlobalMessages.SystemFailureMessage));
            }
        }

        private async Task ProcessUserRating(Order order, OrderDeliverViewModel orderDeliverDto)
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

        private void UpdateOrderDetails(Order order)
        {
            order.IsDelivered = 2;
            order.OrderStatus = 1;
            order.UpdatedAt = GeneralPurpose.DateTimeNow();
        }

        private UserRating CreateUserRating(Order order, OrderDeliverViewModel orderDeliverDto)
        {
            return new UserRating
            {
                OrderId = order.Id,
                Reviews = orderDeliverDto.MessageDescription,
                Stars = orderDeliverDto.Rating != null ? Convert.ToInt32(orderDeliverDto.Rating) : null,
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
                //await userRepo.UpdateUser(user);
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

        #region PayPalScenerio For Order Acception
        private async Task<bool> OrderFromPackage(Order order, User valet, string paypalEmail)
        {
            var orderObject = new OrderAcceptedOfPackage()
            {
                PaidByPackage = true,
                ValetId = valet.Id,
                CustomerId = (int)order.CustomerId!,
                OrderId = order.Id,
                OrderPrice = order.OrderPrice,
                PayPalAccount = paypalEmail,
            };
            return await _payPalGateWayService.OrderCreatedByPayPalPackage(orderObject);
        }

        private async Task<bool> OrderFromCheckout(Order order, User valet, string paypalEmail)
        {
            var checkoutObj = new OrderCheckOutAccepted
            {
                OrderId = order.Id,
                PaymentId = order.PayPalPaymentId,
                PayPalAccount = paypalEmail, // May be null, which is okay
            };

            // Calculate HST fee and deduct it from the Order Price 
            decimal orderPrice = order.OrderPrice ?? 0m;
            checkoutObj.OrderPrice = orderPrice;

            return await _payPalGateWayService.PayPalOrderCheckoutAccepted(checkoutObj);
        }
        #endregion

        #region OrderRevision
        [HttpPut("PostSendRevision/{oderId}")]
        public async Task<IActionResult> PostSendRevision(string? orderId, PostAddMessage postAddMessage)
        {
            var decrypt = StringCipher.DecryptionId(postAddMessage.SenderId!);
            var decryptRecieverId = StringCipher.DecryptionId(postAddMessage.ReceiverId!);
            var decryptOrderId = StringCipher.DecryptionId(postAddMessage.OrderId!);

            var sender= await userRepo.GetUserById(decrypt);
            var receiver = await userRepo.GetUserById(decryptRecieverId);
            var getOrder = await orderRepo.GetOrderById(decryptOrderId);

            postAddMessage.MessageDescription = HttpUtility.UrlDecode(postAddMessage.MessageDescription);
            
            var OrderReason = new OrderReason();
            OrderReason.OrderId = decryptOrderId;
            OrderReason.ReasonExplanation = postAddMessage.MessageDescription;
            OrderReason.ReasonType = 2;
            OrderReason.IsActive = 1;
            var orderReasons = await orderReasonRepo.AddOrderReason(OrderReason);

            var message = await MapMessage(postAddMessage, decrypt, decryptRecieverId, getOrder);
            message.OrderReasonId = orderReasons.Id;

            await messagesRepo.AddMessage(message);

            if (!await messagesRepo.saveChangesFunction())
                return BadRequest(GeneralPurpose.GenerateResponseCode(false, "400", GlobalMessages.RecordNotFound, null));

            if (message.Id != 0)
            {
                var orderMessage = MapOrderMessage(message, sender!, postAddMessage.Way);
                await AddNotification(message, "Message Received", "You have just received a revision for your order.",
                    $"order-details/{HttpUtility.UrlDecode(postAddMessage.OrderId!)}", postAddMessage.Way!);

                var userCache = new Dictionary<int, User>();
                var viewModelMessage = await CreateViewModelMessage(message, sender!, userCache, getOrder);
                viewModelMessage.SenderId = decrypt.ToString();
                viewModelMessage.ReceiverId = decryptRecieverId.ToString();

                // Send a notification to connected clients via SignalR
                viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
            }
            return BadRequest(GeneralPurpose.GenerateResponseCode(false, "404", GlobalMessages.MessageSentFail, null));

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
                User CustomerName = await userRepo.GetUserById((int)userRatingObj.CustomerId);
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

        #region Helpers
        
        #region PostAddMessages
        private async Task CreateMessage(PostAddMessage postAddMessage, Message message)
        {
            if (!string.IsNullOrEmpty(postAddMessage.OrderId))
                message.OrderId = Convert.ToInt32(postAddMessage.OrderId);

            message.MessageDescription = postAddMessage.MessageDescription;
            message.SenderId = Convert.ToInt32(postAddMessage.SenderId);
            message.ReceiverId = Convert.ToInt32(postAddMessage.ReceiverId);
            message.IsRead = 0;
            message.IsActive = 1;
            message.CreatedAt = GeneralPurpose.DateTimeNow();
            await messagesRepo.AddMessage(message);
        }

        private async Task AddNotification(Message message, string title, string description, string url, string orderType = "")
        {
            var notificationObj = new Notification();
            if (!string.IsNullOrEmpty(orderType))
            {
                if(orderType == "cancel")
                {
                    notificationObj.NotificationType = (int)NotificationType.OrderCancellationRequested;
                }
                else if (orderType == "Zoom Meeting Created")
                {
                    notificationObj.NotificationType = (int)NotificationType.ZoomMeetingCreated;
                }
                else if(orderType == "extention")
                {
                    notificationObj.NotificationType = (int)NotificationType.DateExtensionRequested;
                }
                else if(orderType == "deliver")
                {
                    notificationObj.NotificationType = (int)NotificationType.OrderDelivered;
                }
                else if (orderType == "DeliveryAccepted")
                {
                    notificationObj.NotificationType = (int)NotificationType.DeliveryAccepted;
                }
            }
            notificationObj.IsRead = 0;
            notificationObj.Title = title;
            notificationObj.Description = description;
            notificationObj.UserId = message.ReceiverId;
            notificationObj.IsActive = (int)EnumActiveStatus.Active;
            notificationObj.Url = $"{url}";
            notificationObj.CreatedAt = GeneralPurpose.DateTimeNow();

            await _notificationService.AddNotification(notificationObj);
            await _notificationHubSocket.Clients.All.SendAsync("ReloadNotifications",
                notificationObj.UserId.ToString(),
                notificationObj.Title,
                notificationObj.IsRead,
                notificationObj.IsActive,
                $"{projectVariables.ReactUrl}{notificationObj.Url}",
                notificationObj.Description,
                notificationObj.CreatedAt,
                notificationObj.NotificationType
            );
        }

        private async Task<OfferDetail> CreateOffer(PostAddMessage postAddMessage, Message message,
            User getLoggedInUser, decimal pricePerHour)
        {
            var orderStartTime = Convert.ToDateTime(postAddMessage.StartedDateTime);
            var orderEndTime = Convert.ToDateTime(postAddMessage.EndedDateTime);
            var totalHours = GeneralPurpose.CalculatePrice(orderStartTime, orderEndTime, pricePerHour);

            postAddMessage.CustomerId = getLoggedInUser.Role != 3 ? postAddMessage.ReceiverId.ToString() : postAddMessage.SenderId.ToString();
            postAddMessage.ValetId = postAddMessage.ValetId;
            postAddMessage.OfferPrice = totalHours.price.ToString();
            postAddMessage.TransactionFee = totalHours.fee.ToString();

            var offer = new OfferDetail
            {
                OfferTitle = postAddMessage.OfferTitle,
                OfferDescription = postAddMessage.OfferDescription,
                OfferPrice = Convert.ToDouble(postAddMessage.OfferPrice),
                StartedDateTime = orderStartTime,
                EndedDateTime = orderEndTime,
                OfferStatus = 1,
                TransactionFee = postAddMessage.TransactionFee,
                CustomerId = Convert.ToInt32(postAddMessage.CustomerId),
                ValetId = Convert.ToInt32(postAddMessage.ValetId),
                MessageId = message.Id
            };

            if (await offerDetailsRepo.AddOfferDetail(offer))
            {
                return offer;
            }

            return null;
        }

        private async Task<ViewModelMessageChatBox> NotifyOffer(OfferDetail offer, Message message,
            User getLoggedInUser, User getMessageReceiver)
        {
            var viewModelMessage = new ViewModelMessageChatBox
            {
                Id = message.Id.ToString(),
                MessageEncId = StringCipher.EncryptId(message.Id),
                MessageDescription = message.MessageDescription,
                IsRead = message.IsRead?.ToString(),
                FilePath = message.FilePath,
                SenderId = message.SenderId.ToString(),
            };

            //order wprk
            if (offer != null)
            {
                viewModelMessage.OfferTitleId = offer.Id.ToString();
                viewModelMessage.OfferTitle = offer.OfferTitle;
                viewModelMessage.TransactionFee = offer.TransactionFee;
                viewModelMessage.OfferDescription = offer.OfferDescription;
                viewModelMessage.OfferPrice = offer.OfferPrice.ToString();
                viewModelMessage.StartedDateTime = offer.StartedDateTime.ToString();
                viewModelMessage.EndedDateTime = offer.EndedDateTime.ToString();
                viewModelMessage.CustomerId = offer.CustomerId.ToString();
                viewModelMessage.ValetId = offer.ValetId.ToString();
                viewModelMessage.OfferStatus = offer.OfferStatus.ToString();
                viewModelMessage.Name = $"{getLoggedInUser?.FirstName} {getLoggedInUser?.LastName}";
                viewModelMessage.Username = getLoggedInUser?.UserName;
                viewModelMessage.ProfileImage = getLoggedInUser?.ProfilePicture;
            }
            //end

            viewModelMessage = await SetReceiverTime(viewModelMessage, message, getMessageReceiver, "Message");
            viewModelMessage = await SetSenderTime(viewModelMessage, message, getLoggedInUser!);

            return viewModelMessage;
        }

        private async Task<ViewModelMessageChatBox> SetSenderTime(ViewModelMessageChatBox viewModelMessage, Message message,
            User getLoggedInUser)
        {
            viewModelMessage.MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser?.Timezone!);
            viewModelMessage.MessageDate = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser?.Timezone!)).ToString("yyyy-MM-dd");
            viewModelMessage.Time = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getLoggedInUser?.Timezone!)).ToString("t");

            return viewModelMessage;
        }

        private async Task<ViewModelMessageChatBox> SetReceiverTime(ViewModelMessageChatBox viewModelMessage, Message message,
            User getMessageReceiver, string order = "")
        {
            viewModelMessage.MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getMessageReceiver?.Timezone!);
            viewModelMessage.MessageDate = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getMessageReceiver?.Timezone!)).ToString("yyyy-MM-dd");
            viewModelMessage.Time = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), getMessageReceiver?.Timezone!)).ToString("t");

            if(order == "Order")
                await _notificationHubSocket.Clients.All.SendAsync("SendOrderMessage",
                            viewModelMessage,
                            message.SenderId,
                            message.ReceiverId
                        );
            else if(order == "Message")
                await _notificationHubSocket.Clients.All.SendAsync("ReceiveOffers",
                        viewModelMessage,
                        message.SenderId,
                        message.ReceiverId
                    );


            return viewModelMessage;
        }
        #endregion

        #region GetMessagesForUsers
        private ViewModelMessageChatBox MapMessageToViewModel(Message message, User loggedInUser, User targetUser)
        {
            var viewModel = new ViewModelMessageChatBox
            {
                Id = message.Id.ToString(),
                FilePath = message.FilePath,
                IsRead = message.IsRead?.ToString(),
                SenderId = message.SenderId.ToString(),
                MessageDescription = message.MessageDescription,
                MessageEncId = StringCipher.EncryptId(message.Id),
                SenderEncId = StringCipher.EncryptId((int)message.SenderId!),
                MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), loggedInUser.Timezone!),
                MessageDate = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), loggedInUser.Timezone!)).ToString("yyyy-MM-dd"),
                Time = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), loggedInUser.Timezone!)).ToString("t"),

            };
            if (loggedInUser.Role == 4)
            {
                viewModel.PricePerHour = loggedInUser.PricePerHour.ToString();
            }
            else if (targetUser.Role == 4)
            {
                viewModel.PricePerHour = targetUser.PricePerHour.ToString();
            }

            if (message.OfferDetails != null)
            {
                MapOfferDetailsToViewModel(viewModel, message.OfferDetails);
            }

            if (loggedInUser.Id == message.SenderId)
            {
                viewModel.Name = $"{loggedInUser.FirstName} {loggedInUser.LastName}";
                viewModel.Username = loggedInUser.UserName;
                viewModel.ProfileImage = projectVariables.BaseUrl + loggedInUser.ProfilePicture;
            }
            else
            {
                viewModel.ReceiverEncId = StringCipher.EncryptId(targetUser.Id!);
                viewModel.Name = $"{targetUser.FirstName} {targetUser.LastName}";
                viewModel.Username = targetUser.UserName;
                viewModel.ProfileImage = projectVariables.BaseUrl + targetUser.ProfilePicture;
            }

            return viewModel;
        }

        private void MapOfferDetailsToViewModel(ViewModelMessageChatBox viewModel, OfferDetail offerDetails)
        {
            viewModel.OfferTitleId = offerDetails.Id.ToString();
            viewModel.OfferTitle = offerDetails.OfferTitle;
            viewModel.TransactionFee = offerDetails.TransactionFee;
            viewModel.OfferDescription = offerDetails.OfferDescription;
            viewModel.OfferPrice = offerDetails.OfferPrice.ToString();
            viewModel.StartedDateTime = offerDetails.StartedDateTime.ToString();
            viewModel.EndedDateTime = offerDetails.EndedDateTime.ToString();
            viewModel.CustomerId = offerDetails.CustomerId.ToString();
            viewModel.ValetId = offerDetails.ValetId.ToString();
            viewModel.OfferStatus = offerDetails.OfferStatus.ToString();
            viewModel.CustomerEncId = StringCipher.EncryptId((int)offerDetails.CustomerId!);
            viewModel.ValetEncId = StringCipher.EncryptId((int)offerDetails.ValetId!);
        }
        #endregion

        #region GetMessagesForOrder
        private async Task<ViewModelMessageChatBox> CreateViewModelMessage(Message message, User loggedInUser,
            Dictionary<int, User> userCache, Order order)
        {
            string startUrl = string.Empty, endUrl = string.Empty;

            if (message.IsZoomMessage == 1)
            {
                startUrl = ExtractPart(message.MessageDescription, 1);
                endUrl = ExtractPart(message.MessageDescription, 2);
            }

            var orderReason = message.OrderReasonId != null && message.OrderReasonId.HasValue
                ? await orderReasonRepo.GetOrderReasonByOrderReasonId((int)message.OrderReasonId)
                : null;

            var receiverId = loggedInUser.Id == message.SenderId ? message.ReceiverId : message.SenderId;

            if (!userCache.ContainsKey((int)receiverId))
            {
                userCache[(int)receiverId] = await userRepo.GetUserById((int)receiverId);
            }

            var receiver = userCache[(int)receiverId];

            var viewModel = new ViewModelMessageChatBox
            {
                Id = message.Id.ToString(),
                OrderId = order.Id.ToString(),
                ValetId = order.ValetId.ToString(),
                IsRead = message.IsRead?.ToString(),
                SenderId = message.SenderId.ToString(),
                CustomerId = order.CustomerId.ToString(),
                OrderEncId = StringCipher.EncryptId(order.Id),
                MessageDescription = message.MessageDescription,
                OrderReasonId = message.OrderReasonId?.ToString(),
                MessageEncId = message.Id != null ? StringCipher.EncryptId(message.Id) : null,
                ValetEncId = StringCipher.EncryptId((int)order.ValetId!),
                CustomerEncId = StringCipher.EncryptId((int)order.CustomerId!),
                OrderReasonEncId = message.OrderReasonId != null ? StringCipher.EncryptId((int)message.OrderReasonId) : null,
                FilePath = !string.IsNullOrEmpty(message.FilePath) ? $"{projectVariables.BaseUrl}{message.FilePath}" : "",
                MessageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), loggedInUser.Timezone!),
                IsDelivered = order.IsDelivered?.ToString(),
                OrderStatus = order.OrderStatus?.ToString(),
            };
            if(message.OrderReasonId != null)
                SetOrderReasonDetails(viewModel, orderReason);
            if(message.OfferDetails != null)
                SetOfferDetails(viewModel, message);
            if(message.IsZoomMessage == 1)
                SetZoomMessageDetails(viewModel, message, loggedInUser, receiver, startUrl, endUrl);

            if (loggedInUser.Id == message.SenderId)
            {
                viewModel.Username = loggedInUser.UserName;
                viewModel.ProfileImage = loggedInUser.ProfilePicture;
                viewModel.Name = $"{loggedInUser.FirstName} {loggedInUser.LastName}";
                viewModel.Time = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), loggedInUser?.Timezone!)).ToString("t");
            }
            else
            {
                viewModel.Username = receiver.UserName;
                viewModel.ProfileImage = receiver.ProfilePicture;
                viewModel.Name = $"{receiver.FirstName} {receiver.LastName}";
                viewModel.Time = Convert.ToDateTime(GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), receiver?.Timezone!)).ToString("t");
            }

            return viewModel;
        }

        private void SetOrderReasonDetails(ViewModelMessageChatBox viewModel, OrderReason orderReason)
        {
            if (orderReason != null)
            {
                viewModel.OrderReasonType = Enum.GetName(typeof(OrderReasonType), orderReason.ReasonType);
                viewModel.OrderReasonIsActive = orderReason.IsActive?.ToString();
            }
        }

        private void SetOfferDetails(ViewModelMessageChatBox viewModel, Message message)
        {
            if (message.OfferDetails != null)
            {
                viewModel.OfferTitleId = message.OfferDetails.Id.ToString();
                viewModel.OfferTitle = message.OfferDetails.OfferTitle;
                viewModel.OfferDescription = message.OfferDetails.OfferDescription;
                viewModel.OfferPrice = message.OfferDetails.OfferPrice.ToString();
                viewModel.StartedDateTime = message.OfferDetails.StartedDateTime?.ToString();
                viewModel.EndedDateTime = message.OfferDetails.EndedDateTime?.ToString();
                viewModel.CustomerId = message.OfferDetails.CustomerId?.ToString();
                viewModel.ValetId = message.OfferDetails.ValetId?.ToString();
                viewModel.OfferStatus = message.OfferDetails.OfferStatus?.ToString();
            }
        }

        private void SetZoomMessageDetails(ViewModelMessageChatBox viewModel, Message message, User loggedInUser,
            User receiver, string startUrl, string endUrl)
        {
            if (message.IsZoomMessage == 1)
            {
                viewModel.IsZoomMeeting = string.IsNullOrEmpty(startUrl) ? 0 : 1;
                viewModel.MessageDescription = "Zoom Meeting Created";
                viewModel.OrderReasonType = "Zoom";

                if (loggedInUser.Id == message.SenderId)
                {
                    viewModel.StartUrl = startUrl;
                }
                else
                {
                    viewModel.JoinUrl = endUrl;
                }
            }
        }
        #endregion
        
        #region PostAddOrderMessages
        private async Task<Message> MapMessage(PostAddMessage message, int SenderId, int ReceiverId, Order order)
        {
            var filePath = "";
            if (message.IFilePath != null)
            {
                filePath = await UploadFiles(message.IFilePath, "OrderDeliverable");
                message.FilePath = filePath;
            }

            if (message.Way == "deliver")
            {
                order!.IsDelivered = 1;
                order.UpdatedAt = GeneralPurpose.DateTimeNow();
                var updateOrder = await orderRepo.UpdateOrder(order);
            }

            return new Message()
            {
                MessageDescription = String.IsNullOrEmpty(message.MessageDescription) ? "" : message.MessageDescription,
                SenderId = SenderId,
                ReceiverId = ReceiverId,
                OrderId = order.Id,
                IsRead = 0,
                IsActive = 1,
                CreatedAt = GeneralPurpose.DateTimeNow(),
                FilePath = message.FilePath
            };
        }

        private Message MapMessageForZoom(string message, int SenderId, int ReceiverId, int order)
        {
            return new Message()
            {
                MessageDescription = message,
                SenderId = SenderId,
                ReceiverId = ReceiverId,
                OrderId = order,
                IsZoomMessage = 1,
                IsActive = 1,
                CreatedAt = GeneralPurpose.DateTimeNow(),
            };
        }

        private ReceiveOrderMessageDto MapOrderMessage(Message message, User sender, string? orderType = "")
        {
            return new ReceiveOrderMessageDto()
            {
                newOrderReasonId = "",
                IsDelivery = orderType,
                userName = sender.UserName,
                filePath = message.FilePath,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                userProfile = sender.ProfilePicture,
                message = message.MessageDescription,
                messageTime = GeneralPurpose.regionChanged(Convert.ToDateTime(message.CreatedAt), sender.Timezone!),
            };
        }
        #endregion

        #region CancelAcceptOrders
        private async Task<IActionResult> HandleOrderStatusChange(string orderId, OrderStatusDto obj, string notificationTitle,
            string notificationMessage, string notificationType, int reasonType)
        {
            try
            {
                // Decrypt input IDs
                var decryptSenderId = StringCipher.DecryptionId(obj.SenderId!);
                var decryptReceiverId = StringCipher.DecryptionId(obj.ReceiverId!);
                var decryptOrderId = StringCipher.DecryptionId(orderId!);

                // Fetch necessary entities
                var sender= await userRepo.GetUserById(decryptSenderId);
                var receiver = await userRepo.GetUserById(decryptReceiverId);
                var order = await orderRepo.GetOrderById(decryptOrderId);

                if (sender== null || receiver == null || order == null)
                    return NotFound(GeneralPurpose.GenerateResponseCode(false, "404", "Invalid sender, receiver, or order ID."));

                // Prepare the order reason entity
                var orderReason = await CreateOrderReason(order, obj, reasonType);

                // Prepare the message entity
                var message = await CreateStatusMessage(decryptOrderId, decryptSenderId, decryptReceiverId, orderReason, obj.DateExtension);

                // Add a notification for the action
                await AddNotification(
                    message,
                    notificationTitle,
                    notificationMessage,
                    $"order-details/{HttpUtility.UrlDecode(orderId)}",
                    notificationType
                );

                // Prepare data for the real-time notification
                var userCache = new Dictionary<int, User>();
                var viewModelMessage = await CreateViewModelMessage(message, sender, userCache, order);
                viewModelMessage.SenderId = decryptSenderId.ToString();
                viewModelMessage.ReceiverId = decryptReceiverId.ToString();
                viewModelMessage.OrderReasonId = orderReason.Id.ToString();
                viewModelMessage.OrderReasonEncId = StringCipher.EncryptId(orderReason.Id);


                // Send a notification to connected clients via SignalR
                viewModelMessage = await SetReceiverTime(viewModelMessage, message, receiver!, "Order");
                viewModelMessage = await SetSenderTime(viewModelMessage, message, sender!);

                // Return success response
                return Ok(GeneralPurpose.GenerateResponseCode(true, "200", "", viewModelMessage));
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, GeneralPurpose.GenerateResponseCode(false, "500", "An error occurred while processing the request."));
            }
        }

        private async Task<OrderReason> CreateOrderReason(Order order, OrderStatusDto obj, int reasonType)
        {
            var orderReason = new OrderReason
            {
                OrderId = order.Id,
                ReasonExplanation = $"<strong>Reason: </strong> {obj.Explanation}",
                IsActive = 1,
                CreatedAt = GeneralPurpose.DateTimeNow(),
                ReasonType = reasonType
            };

            if (!string.IsNullOrEmpty(obj.DateExtension))
            {
                DateTime dateOfExtension = DateTime.Parse(obj.DateExtension);
                DateTime currentDate = DateTime.Now;
                TimeSpan timeDifference = dateOfExtension - currentDate;
                // Extract days, hours, and minutes
                int daysDifference = timeDifference.Days;
                int hoursDifference = timeDifference.Hours;
                int minutesDifference = timeDifference.Minutes;

                orderReason.ReasonExplanation += $"<br /> <strong>Requested time: </strong> {daysDifference} days, {hoursDifference} hours, and {minutesDifference} minutes.";
            }

            return await orderReasonRepo.AddOrderReason(orderReason);
        }

        private async Task<Message> CreateStatusMessage(int orderId, int senderId, int receiverId,
            OrderReason orderReason, string? dateExtension)
        {
            var message = new Message
            {
                OrderId = orderId,
                SenderId = senderId,
                ReceiverId = receiverId,
                OrderReasonId = orderReason.Id,
                MessageDescription = orderReason.ReasonExplanation
            };

            if (!string.IsNullOrEmpty(dateExtension))
                message.MessageDescription += $"<br /> <strong>Extended to: </strong> {Convert.ToDateTime(dateExtension).ToString("yyyy-MM-dd HH:mm tt")}";

            return await PostAddOrderReasonMessage(message);
        }

        private async Task CreateAcceptedOrderMessage(int orderId, int senderId, int receiverId, Message message)
        {
            var postMessage = new PostAddMessage
            {
                OrderId = orderId.ToString(),
                SenderId = senderId.ToString(),
                ReceiverId = receiverId.ToString(),
                MessageDescription = "Congratulations!, your order delivery has been accepted.",
            };

            await CreateMessage(postMessage, message);
        }
        #endregion
        #endregion
    }
}
