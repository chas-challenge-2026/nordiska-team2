using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/faq")]
    public class FaqController : ControllerBase
    {
        private readonly FaqService _faqService;
        private readonly FaqCategoryService _categoryService;

        public FaqController(FaqService faqService, FaqCategoryService categoryService)
        {
            _faqService = faqService;
            _categoryService = categoryService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] FaqSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { message = "Query is required." });

            var result = await _faqService.SearchAsync(request);
            return Ok(result);
        }
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("categories/{id}/entries")]
        public async Task<IActionResult> GetEntries(string id)
        {
            var entries = await _categoryService.GetEntriesByCategoryAsync(id);

            if (entries == null)
                return NotFound(new { message = "Category not found." });

            return Ok(entries);
        }

        [HttpGet("popular")]
        public async Task<IActionResult> GetPopular()
        {
            var popular = await _categoryService.GetPopularAsync();
            return Ok(popular);
        }
    }
}