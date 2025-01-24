using AutoMapper;
using ITValet.HelpingClasses;
using ITValet.Models;
using ITValet.Utils.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITValet.Services
{
    public interface IBlogRepo
    {
        Task<ResponseDto> GetSkills();
        Task<ResponseDto> GetBlogById(string id);
        Task<ResponseDto> GetBlogBySkill(string skillName);
        Task<ResponseDto> GetBlogList(int start, int length, string? sortColumnName, string? sortDirection,
            string? searchValue, bool isSkill = false, string skillName = "");
        Task<ResponseDto> AddBlog(AddUpdateBlogViewModel viewModel);
        Task<ResponseDto> UpdateBlog(AddUpdateBlogViewModel viewModel);
        Task<ResponseDto> DeleteBlog(string id);

        Task<bool> SlugExists(string slug, int? id = 0);
    }

    public class BlogRepo : IBlogRepo
    {
        private readonly IMapper _mapper;
        private readonly IUserRepo _userRepo;
        private readonly AppDbContext _context;
        private readonly ProjectVariables _projectVariables;
        public BlogRepo(AppDbContext _appDbContext, IUserRepo userRepo, IMapper mapper, IOptions<ProjectVariables> options)
        {
            _context = _appDbContext;
            _userRepo = userRepo;
            _mapper = mapper;
            _projectVariables = options.Value;
        }


        public async Task<ResponseDto> AddBlog(AddUpdateBlogViewModel viewModel)
        {
            try
            {
                string uniqueSlug = await GetUniqueSlug(viewModel.Slug!);

                var getObj = _mapper.Map<Blog>(viewModel);
                getObj.Slug = uniqueSlug;
                getObj.IsActive = (int)EnumActiveStatus.Active;
                getObj.CreatedAt = GeneralPurpose.DateTimeNow();
                getObj.PublishedDate = GeneralPurpose.DateTimeNow();
                
                if (viewModel.Image != null && viewModel.Image.Length > 0)
                {
                    var imagePath = await GeneralPurpose.UploadFiles(viewModel.Image, "Blogs");
                    if (!string.IsNullOrEmpty(imagePath))
                        getObj.Image = imagePath;
                    else
                        return GeneralPurpose.GenerateResponse(false, "400", "File not uploaded. Try with another one!");
                }

                bool isAdded = await Add(getObj);
                if (!isAdded)
                    return GeneralPurpose.GenerateResponse(false, "500", "Something's went wrong. Try again later!");

                var responseData = _mapper.Map<BlogViewModel>(getObj);
                return GeneralPurpose.GenerateResponse(true, "200", "Record added successfully.", responseData);
            }
            catch (Exception ex)
            {
                return GeneralPurpose.GenerateResponse(false, "500", GlobalMessages.SystemFailureMessage);
            }
        }

        public async Task<ResponseDto> UpdateBlog(AddUpdateBlogViewModel viewModel)
        {
            try
            {
                var getObj = await Get(viewModel.EncId!);
                if(getObj == null)
                    return GeneralPurpose.GenerateResponse(false, "400", "Record not found, might be deleted");

                string oldImage = getObj?.Image;

                string uniqueSlug = await GetUniqueSlug(viewModel.Slug!, getObj.Id);

                var mappedObj = _mapper.Map(viewModel, getObj);
                mappedObj.UpdatedAt = GeneralPurpose.DateTimeNow();

                if (viewModel.Image != null && viewModel.Image.Length > 0)
                {
                    var imagePath = await GeneralPurpose.UploadFiles(viewModel.Image, "Blogs");
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        mappedObj.Image = imagePath;
                        GeneralPurpose.DeleteFile(_projectVariables.BaseUrl + oldImage);
                    }
                    else
                        return GeneralPurpose.GenerateResponse(false, "400", "File not uploaded. Try with another one!");
                }
                else
                {
                    mappedObj.Image = oldImage;
                }


                bool isUpdated = await Update(mappedObj);
                if (!isUpdated)
                    return GeneralPurpose.GenerateResponse(false, "500", "Something's went wrong. Try again later!");

                var responseData = _mapper.Map<BlogViewModel>(mappedObj);
                return GeneralPurpose.GenerateResponse(true, "200", GlobalMessages.UpdateMessage, responseData);
            }
            catch (Exception ex)
            {
                return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.SystemFailureMessage);
            }
        }

        public async Task<ResponseDto> DeleteBlog(string id)
        {
            try
            {
                Blog? blog = await Get(id);

                if (blog == null)
                    return GeneralPurpose.GenerateResponse(false, "400", "Something' went wrong. Might already be deleted or removed!");
                
                blog.IsActive = 0;
                blog.DeletedAt = GeneralPurpose.DateTimeNow();
                bool isDeleted = await Update(blog);
                    
                if (!isDeleted)
                    return GeneralPurpose.GenerateResponse(false, "400", "Something' went wrong. Try again later!");

                return GeneralPurpose.GenerateResponse(true, "200", GlobalMessages.DeletedMessage, blog);
            }
            catch (Exception ex)
            {
                return GeneralPurpose.GenerateResponse(false, "500", GlobalMessages.SystemFailureMessage);
            }
        }

        public async Task<ResponseDto> GetBlogById(string id)
        {
            try
            {
                var getObj = await Get(id);
                if (getObj == null)
                    return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.RecordNotFound);

                var responseData = _mapper.Map<BlogViewModel>(getObj);
                return GeneralPurpose.GenerateResponse(true, "200", GlobalMessages.RecordFound, responseData);
            }
            catch (Exception)
            {
                return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.SystemFailureMessage);
            }
        }

        public async Task<ResponseDto> GetBlogBySkill(string skillName)
        {
            try
            {
                var getObj = await GetBySkill(skillName);
                if (getObj == null)
                    return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.RecordNotFound);

                var responseData = _mapper.Map<BlogViewModel>(getObj);
                return GeneralPurpose.GenerateResponse(true, "200", GlobalMessages.RecordFound, responseData);
            }
            catch (Exception)
            {
                return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.SystemFailureMessage);
            }
        }

        public async Task<ResponseDto> GetSkills()
        {
            try
            {
                var getObj = await GetAll(true);
                var getSkills = getObj.Select(x => x.Skill).ToList();
                if (getSkills == null)
                    return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.RecordNotFound);

                return GeneralPurpose.GenerateResponse(true, "200", GlobalMessages.RecordFound, getSkills);
            }
            catch (Exception)
            {
                return GeneralPurpose.GenerateResponse(false, "400", GlobalMessages.SystemFailureMessage);
            }
        }

        public async Task<ResponseDto> GetBlogList(int start, int length, string? sortColumnName, string? sortDirection,
            string? searchValue, bool isSkill = false, string skillName = "")
        {
            try
            {
                var blogsList = await GetAll(isSkill, skillName);

                // Initialize BaseService
                var baseService = new DatatableHelper<Models.Blog>();

                // Apply sorting
                blogsList = baseService.ApplySorting(blogsList, sortColumnName, sortDirection);

                // Apply filtering
                if (!string.IsNullOrEmpty(searchValue))
                {
                    blogsList = baseService.ApplyFiltering(blogsList, o =>
                        o.Title?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.Tags?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.Skill?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.Content?.ToLower().Contains(searchValue.ToLower()) == true ||
                        o.PublishedDate?.ToString().Contains(searchValue) == true ||
                        o.Slug != null && o.Slug.ToLower().Contains(searchValue.ToLower()));
                }

                // Record counts
                int totalRows = blogsList.Count();
                int totalRowsAfterFiltering = totalRows;

                // Apply pagination
                if (totalRowsAfterFiltering > 0 && start < totalRowsAfterFiltering)
                {
                    blogsList = baseService.ApplyPagination(blogsList, start, length);
                }

                var viewModelList = new List<BlogViewModel>();
                var getUser = new User();
                foreach (var item in blogsList)
                {
                    var viewModel = _mapper.Map<BlogViewModel>(item);
                    if(getUser?.Id != item.CreatedBy)
                    {
                        getUser = await _userRepo.GetUserById((int)item.CreatedBy!);
                    }
                    // vehicle image
                    viewModel.Image = string.IsNullOrEmpty(item.Image)
                    ?
                    "" : _projectVariables.BaseUrl + item.Image;
                    viewModel.PublishedBy = getUser?.FirstName + " " + getUser?.LastName;
                    viewModel.PublisherImage = string.IsNullOrEmpty(getUser?.ProfilePicture) ? "" : _projectVariables.BaseUrl + getUser?.ProfilePicture;

                    viewModelList.Add(viewModel);
                }

                return GeneralPurpose.GenerateResponseCode(true, "200", GlobalMessages.RecordFound, new
                {
                    draw = (start / length) + 1,
                    data = viewModelList,
                    recordsTotal = totalRows,
                    recordsFiltered = totalRowsAfterFiltering
                });
            }
            catch (Exception ex)
            {
                GeneralPurpose.CreateLogger(_projectVariables, ex);
                return GeneralPurpose.GenerateResponse(false, "500", GlobalMessages.SystemFailureMessage);
            }
        }


        public async Task<bool> SlugExists(string slug, int? id = 0)
        {
            try
            {
                if (id != 0)
                    return await _context.Blog.AnyAsync(b => b.IsActive == (int)EnumActiveStatus.Active &&
                                                              b.Id != id &&
                                                              b.Slug == slug);
                else
                    return await _context.Blog.AnyAsync(b => b.IsActive == (int)EnumActiveStatus.Active && b.Slug == slug);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        #region Private Methods
        private async Task<bool> Add(Blog blog)
        {
            try
            {
                _context.Blog.Add(blog);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> Update(Blog blog)
        {
            try
            {
                _context.Entry(blog).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<Blog?> Get(string id)
        {
            try
            {
                int decryptedId = StringCipher.DecryptionId(id);
                var getObj = await _context.Blog.Where(x=> 
                                                        x.IsActive == (int)EnumActiveStatus.Active && 
                                                        x.Id == decryptedId).FirstOrDefaultAsync();
                return getObj;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<Blog?> GetBySkill(string skill)
        {
            try
            {
                var getObj = await _context.Blog.Where(x =>
                                                        x.IsActive == (int)EnumActiveStatus.Active &&
                                                        !string.IsNullOrEmpty(x.Skill) && x.Skill.ToLower() == skill.ToLower()).FirstOrDefaultAsync();
                return getObj;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<IEnumerable<Blog>> GetAll(bool isSkill = false, string? skillName = "")
        {
            try
            {
                if (isSkill && !string.IsNullOrEmpty(skillName))
                {
                    return await _context.Blog.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                                                           !string.IsNullOrEmpty(x.Skill) &&
                                                           x.Skill.Contains(skillName))
                                              .OrderByDescending(x => x.Id)
                                              .ToListAsync();
                }
                else if (isSkill)
                {
                    return await _context.Blog.Where(x => x.IsActive == (int)EnumActiveStatus.Active &&
                                                           !string.IsNullOrEmpty(x.Skill))
                                              .OrderByDescending(x => x.Id)
                                              .ToListAsync();
                }
                else
                    return await _context.Blog.Where(x => x.IsActive == (int)EnumActiveStatus.Active && string.IsNullOrEmpty(x.Skill))
                                              .OrderByDescending(x => x.Id)
                                              .ToListAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<string> GetUniqueSlug(string slug, int? id = 0)
        {
            string uniqueSlug = slug;
            if (await SlugExists(uniqueSlug, id))
            {
                int randomNumber = new Random().Next(1, 1000);
                uniqueSlug = $"{uniqueSlug}-{randomNumber}";

                while (await SlugExists(uniqueSlug, id))
                {
                    randomNumber = new Random().Next(1, 1000);
                    uniqueSlug = $"{slug}-{randomNumber}";
                }
            }

            return uniqueSlug;
        }
#endregion
    }
}
