using FluentValidation;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Validators
{
    public class CreateSavingsGoalRequestValidator : AbstractValidator<CreateSavingsGoalRequest>
    {
        public CreateSavingsGoalRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Sparmålet måste ha ett namn.")
                .MaximumLength(100).WithMessage("Namnet får vara högst 100 tecken.");

            RuleFor(x => x.TargetAmount)
                .GreaterThan(0).WithMessage("Målbeloppet måste vara större än 0.");

            RuleFor(x => x.Deadline)
                .GreaterThan(DateTime.UtcNow).WithMessage("Slutdatum måste vara i framtiden.")
                .When(x => x.Deadline.HasValue);
        }
    }
}