using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Interop;
using NordiskaPortal.Api.Services;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/bank-statements")]
    public class BankStatementController : ControllerBase
    {
        private readonly BankStatementService _bankStatementService;
        private readonly PdfGeneratorService _pdfGeneratorService;

        public BankStatementController(BankStatementService bankStatementService, PdfGeneratorService pdfGeneratorService)
        {
            _bankStatementService = bankStatementService;
            _pdfGeneratorService = pdfGeneratorService;
        }

        [HttpGet("{accountId:int}/{year:int}/{month:int}")]
        public async Task<IActionResult> GetStatementForMonth(int accountId, int year, int month)
        {
            BankStatementDto? statement;

            try
            {
                statement = await _bankStatementService.BuildStatementForMonthAsync(accountId, year, month);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (statement == null)
                return NotFound(new { message = "Account not found." });

            var pdfResult = _pdfGeneratorService.GenerateSingleReport(statement, PdfReportType.BankStatement, null, null);
            if (!pdfResult.Success)
            {
                return StatusCode(500, new
                {
                    message = "PDF generation failed.",
                    errorCode = pdfResult.ErrorCode.ToString(),
                    detail = pdfResult.ErrorMessage
                });
            }

            return File(pdfResult.PdfBytes!, "application/pdf", $"{statement.Metadata.StatementId}.pdf");
        }
    }
}