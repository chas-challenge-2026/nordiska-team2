using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class SavingsGoalService : ISavingsGoalService
    {
        private readonly BankContext _db;
        private readonly IAccountService _accountService;
        private readonly ITransactionService _transactionService;

        public SavingsGoalService(BankContext db, IAccountService accountService, ITransactionService transactionService)
        {
            _db = db;
            _accountService = accountService;
            _transactionService = transactionService;
        }

        public async Task<List<SavingsGoalDto>> GetGoalsAsync(int customerId)
        {
            var goals = await _db.SavingsGoals
                .Where(g => g.CustomerId == customerId)
                .OrderBy(g => g.CreatedAt)
                .ToListAsync();

            // Sequential on purpose: DbContext can't run concurrent
            // operations (the same lesson as the reverted AccountService
            // parallelization).
            var result = new List<SavingsGoalDto>();
            foreach (var goal in goals)
                result.Add(await ToDtoAsync(goal));

            return result;
        }

        public async Task<SavingsGoalDto?> GetGoalAsync(int customerId, int goalId)
        {
            // Filters on BOTH id and owner in one query, so another
            // customer's goal looks identical to a nonexistent one.
            var goal = await _db.SavingsGoals
                .FirstOrDefaultAsync(g => g.Id == goalId && g.CustomerId == customerId);

            return goal == null ? null : await ToDtoAsync(goal);
        }

        public async Task<SavingsGoalDto?> CreateGoalAsync(int customerId, CreateSavingsGoalRequest request)
        {
            if (request.AccountId.HasValue &&
                !await _accountService.CustomerOwnsAccountAsync(customerId, request.AccountId.Value))
                return null;

            var goal = new SavingsGoal
            {
                CustomerId = customerId,
                AccountId = request.AccountId,
                Name = request.Name,
                TargetAmount = request.TargetAmount,
                Deadline = request.Deadline,
            };

            _db.SavingsGoals.Add(goal);
            await _db.SaveChangesAsync();

            return await ToDtoAsync(goal);
        }

        public async Task<bool> DeleteGoalAsync(int customerId, int goalId)
        {
            var goal = await _db.SavingsGoals
                .FirstOrDefaultAsync(g => g.Id == goalId && g.CustomerId == customerId);

            if (goal == null)
                return false;

            _db.SavingsGoals.Remove(goal);
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<SavingsGoalDto> ToDtoAsync(SavingsGoal goal)
        {
            decimal? current = null;
            decimal? progress = null;

            if (goal.AccountId.HasValue)
            {
                current = await _transactionService.GetBalanceAsync(goal.AccountId.Value);
                progress = Math.Min(100m, Math.Round(current.Value / goal.TargetAmount * 100m, 1));
            }

            return new SavingsGoalDto(goal.Id, goal.Name, goal.TargetAmount, goal.Deadline, goal.AccountId, current, progress);
        }
    }
}