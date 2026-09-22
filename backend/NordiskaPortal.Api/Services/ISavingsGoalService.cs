using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services
{
    public interface ISavingsGoalService
    {
        Task<List<SavingsGoalDto>> GetGoalsAsync(int customerId);

        // Returns null if AccountId was given but the customer doesn't own it.
        Task<SavingsGoalDto?> CreateGoalAsync(int customerId, CreateSavingsGoalRequest request);

        // Returns false if the goal doesn't exist OR belongs to someone else.
        Task<bool> DeleteGoalAsync(int customerId, int goalId);
    }
}