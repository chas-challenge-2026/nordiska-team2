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

        public FaqController(FaqService faqService)
        {
            _faqService = faqService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] FaqSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { message = "Query is required." });

            var result = await _faqService.SearchAsync(request);
            return Ok(result);
        }
    }
}