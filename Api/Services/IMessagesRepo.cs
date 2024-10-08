using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace ITValet.Services
{
    public interface IMessagesRepo
    {
        Task<Message?> GetMessageById(int id);
        Task<IEnumerable<Message>> GetMessageByUserId(int userId);
        Task<IEnumerable<Message>> GetMessageBySenderIdAndRecieverId(int senderId, int receiverId);
        Task<IEnumerable<Message>> GetMessageList();
        Task<IEnumerable<Message>> GetMessageListByOrdrId(int orderId);
        Task<List<Message>> GetZoomMessageList();
        Task<bool> AddMessage(Message message);
        Task<int> AddMessageReturnId(Message message);
        Task<bool> UpdateMessage(Message message);
        Task<bool> DeleteMessage(int id);
        Task<bool> DeleteMessage2(int id);
        Task<bool> saveChangesFunction();
    }

    public class MessagesRepo : IMessagesRepo
    {
        private readonly AppDbContext _context;
        public MessagesRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddMessage(Message Message)
        {
            try
            {
                _context.Message.Add(Message);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<int> AddMessageReturnId(Message Message)
        {
            try
            {
                _context.Message.Add(Message);
                await _context.SaveChangesAsync();
                return Message.Id;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
        public async Task<List<Message>> GetZoomMessageList()
        {
            DateTime dateTime = GeneralPurpose.DateTimeNow().AddHours(-2);
            DateTime dateTime2 = GeneralPurpose.DateTimeNow().AddMinutes(-10);
            return await _context.Message.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.IsZoomMessage == 1 && x.CreatedAt >= dateTime && x.CreatedAt <= dateTime2).ToListAsync();
        }

        public async Task<bool> DeleteMessage(int id)
        {
            try
            {
                Message? message = await GetMessageById(id);
                message.IsActive = 0;
                message.DeletedAt = GeneralPurpose.DateTimeNow();
                return await UpdateMessage(message);
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteMessage2(int id)
        {
            try
            {
                Message? message = await GetMessageById(id);
                if (message != null)
                {
                    message.IsActive = 0;
                    message.DeletedAt = GeneralPurpose.DateTimeNow();
                    _context.Entry(message).State = EntityState.Modified;
                }
                return true;
            }
            catch (Exception ex)
            {
                MailSender.SendErrorMessage(ex.Message.ToString());
                return false;
            }
        }

        public async Task<Message?> GetMessageById(int id)
        {
            return await _context.Message.FindAsync(id);
        }

        public async Task<IEnumerable<Message>> GetMessageByUserId(int userId)
        {
            var list = await _context.Message.Where(x => x.OrderId == null && (x.SenderId == userId || x.ReceiverId == userId))
                    .GroupBy(x => x.SenderId == userId ? x.ReceiverId : x.SenderId)  // Group by the other party (SenderId or ReceiverId)
                    .Select(m => m.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
                    .ToListAsync();
            return list;
        }

        public async Task<IEnumerable<Message>> GetMessageBySenderIdAndRecieverId(int senderId, int receiverId)
        {
            var list = await _context.Message.Where(x => x.IsActive == (int)EnumActiveStatus.Active && x.OrderId == null &&
            (x.SenderId == senderId && x.ReceiverId == receiverId) || (x.SenderId == receiverId && x.ReceiverId == senderId))
                .Include(x => x.OfferDetails)
                .ToListAsync();
            list = list.Where(x => x.OrderId == null).ToList();
            return list;
        }

        public async Task<IEnumerable<Message>> GetMessageList()
        {
            return await _context.Message.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetMessageListByOrdrId(int orderId)
        {
            var list = await _context.Message.Where(x => x.OrderId == orderId)
                    .ToListAsync();
            return list;
        }

        public async Task<bool> UpdateMessage(Message Message)
        {
            try
            {
                _context.Entry(Message).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> saveChangesFunction()
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
    }
}
