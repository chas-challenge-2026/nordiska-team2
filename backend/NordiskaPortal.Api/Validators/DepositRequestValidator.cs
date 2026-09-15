using FluentValidation;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Validators
{
    public class DepositRequestValidator : AbstractValidator<DepositRequest>
    {
        public DepositRequestValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Beloppet måste vara större än 0.");
        }
    }
}