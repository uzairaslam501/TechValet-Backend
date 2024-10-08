using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IContactUsRepo contactUsRepo;
        private readonly ProjectVariables projectVariables;
        public ContactController(IContactUsRepo _contactUsRepo, IOptions<ProjectVariables> options)
        {
            contactUsRepo = _contactUsRepo;
            projectVariables = options.Value;
        }

        #region Contact
        [HttpPost("PostAddContact")]
        public async Task<IActionResult> PostAddContact(PostAddContact addContact)
        {
            var obj = new Contact();

            obj.Name = addContact.Name;
            obj.Email = addContact.Email;
            obj.Subject = addContact.Subject;
            obj.Message = addContact.Message;
            obj.IsActive = 1;
            obj.CreatedAt = GeneralPurpose.DateTimeNow();

            if (!await contactUsRepo.AddContact(obj))
            {
                return Ok(new ResponseDto() { Status = false, StatusCode = "400", Message = "Database updation failed." });
            }

            return Ok(new ResponseDto() { Status = true, StatusCode = "200", Message = "AvailableSlot has been added to your account" });
        }


        [HttpGet("GetContactById")]
        public async Task<IActionResult> GetContactById(string ContactId)
        {
            var obj = await contactUsRepo.GetContactById(StringCipher.DecryptId(ContactId));
            ContactDto ContactDto = new ContactDto()
            {
                Id = obj.Id,
                UserContactEncId = StringCipher.EncryptId(obj.Id),
                Name = obj.Name.ToString(),
                Email = obj.Email.ToString(),
                Subject = obj.Subject.ToString(),
                Message = obj.Message.ToString(),
            };

            return Ok(new ResponseDto() { Data = ContactDto, Status = true, StatusCode = "200" });
        }

        [HttpPost("GetContactList")]
        public async Task<IActionResult> GetContactList(string? Name = "", string? Email = "", string? subject = "")
        {
            var list = await contactUsRepo.GetContactList();

            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            if (sortColumn != "" && sortColumn != null)
            {
                if (sortColumn != "0")
                {
                    if (sortColumnDirection == "asc")
                    {
                        list = list.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                    else
                    {
                        list = list.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x)).ToList();
                    }
                }
            }
            int totalrows = list.Count();

            if (!string.IsNullOrEmpty(searchValue))
            {
                list = list.ToList();
            }
            int totalrowsafterfilterinig = list.Count();

            list = list.Skip(skip).Take(pageSize).ToList();
            List<ContactDto> dtos = new List<ContactDto>();
            foreach (var obj in list)
            {
                ContactDto contactDto = new ContactDto()
                {
                    Id = obj.Id,
                    UserContactEncId = StringCipher.EncryptId(obj.Id),
                    Name = obj.Name.ToString(),
                    Email = obj.Email.ToString(),
                    Subject = obj.Subject.ToString(),
                    Message = obj.Message.ToString(),
                };
                dtos.Add(contactDto);
            }
            return new ObjectResult(new { data = dtos, draw = draw, recordsTotal = totalrows, recordsFiltered = totalrowsafterfilterinig });
        }
        #endregion
    }
}
