using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JwtAuthorization;
using ITValet.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITValet.Controllers
{
    [ExceptionHandler]
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private readonly IBlogRepo _blogRepo;
        public BlogsController(IBlogRepo blogRepo)
        {
             _blogRepo = blogRepo;
        }

        #region Blog

        [HttpPost]
        [Route("InsertBlog")]
        public async Task<IActionResult> InsertBlog([FromForm] AddUpdateBlogViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _blogRepo.AddBlog(viewModel);
            return Ok(response);
        }

        [HttpPut]
        [Route("UpdateBlog")]
        public async Task<IActionResult> UpdateBlog([FromForm] AddUpdateBlogViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var response = await _blogRepo.UpdateBlog(viewModel);
            return Ok(response);
        }

        [HttpPost]
        [Route("BlogDatatable")]
        public async Task<ActionResult> BlogDatatable(int start = 0, int length = 10, string? sortColumnName = "", string? sortDirection = "", string? searchValue = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _blogRepo.GetBlogList(start, length, sortColumnName, sortDirection, searchValue);
            return Ok(response);
        }

        [HttpPost]
        [Route("BlogDatatableWithSkill")]
        public async Task<ActionResult> BlogDatatableWithSkill(int start = 0, int length = 10, string? sortColumnName = "", 
            string? sortDirection = "", string? searchValue = "", string skillName = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _blogRepo.GetBlogList(start, length, sortColumnName, sortDirection, searchValue, true, skillName);
            return Ok(response);
        }

        [HttpDelete]
        [Route("DeleteBlog/{Id}")]
        public async Task<ActionResult> DeleteBlog(string Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var response = await _blogRepo.DeleteBlog(Id);
            return Ok(response);
        }

        [HttpGet]
        [Route("GetBlogById")]
        public async Task<IActionResult> GetBlogById(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var response = await _blogRepo.GetBlogById(id);
            return Ok(response);
        }

        #endregion Blog
    }
}
