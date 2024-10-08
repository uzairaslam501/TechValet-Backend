using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IContactUsRepo
    {
        Task<Contact?> GetContactById(int id);
        Task<IEnumerable<Contact>> GetContactList();
        Task<bool> AddContact(Contact Contact);
        Task<bool> UpdateContact(Contact Contact);
        Task<bool> DeleteContact(int id);
    }

    public class ContactUsRepo : IContactUsRepo
    {
        private readonly AppDbContext _context;
        public ContactUsRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }
        public async Task<bool> AddContact(Contact contact)
        {
            try
            {
                _context.Contact.Add(contact);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteContact(int id)
        {
            try
            {
                Contact? contact = await GetContactById(id);

                if (contact != null)
                {
                    contact.IsActive = 0;
                    contact.DeletedAt = GeneralPurpose.DateTimeNow();
                    return await UpdateContact(contact);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<Contact?> GetContactById(int id)
        {
            return await _context.Contact.FindAsync(id);
        }

        public async Task<IEnumerable<Contact>> GetContactList()
        {
            return await _context.Contact.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        }

        public async Task<bool> UpdateContact(Contact contact)
        {
            try
            {
                _context.Entry(contact).State = EntityState.Modified;
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
