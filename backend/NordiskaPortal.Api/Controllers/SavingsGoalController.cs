using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Extensions;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/savings-goals")]
    [Authorize]
    public class SavingsGoalsController : ControllerBase
    {
        private readonly ISavingsGoalService _savingsGoalService;

        public SavingsGoalsController(ISavingsGoalService savingsGoalService)
        {
            _savingsGoalService = savingsGoalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyGoals()
        {
            var goals = await _savingsGoalService.GetGoalsAsync(User.GetCustomerId());
            return Ok(goals);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSavingsGoalRequest request)
        {
            var goal = await _savingsGoalService.CreateGoalAsync(User.GetCustomerId(), request);
            if (goal == null)
                return NotFound(new { error = "Kontot kunde inte hittas." });

            return CreatedAtAction(nameof(GetMyGoals), goal);
        }

        [HttpDelete("{goalId:int}")]
        public async Task<IActionResult> Delete(int goalId)
        {
            var deleted = await _savingsGoalService.DeleteGoalAsync(User.GetCustomerId(), goalId);
            if (!deleted)
                return NotFound(new { error = "Sparmålet kunde inte hittas." });

            return NoContent();
        }
    }
}