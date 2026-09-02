using FluentValidation;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Validators
{
    public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
    {
        public WithdrawRequestValidator()
        {
            /*
                Insufficient-balance is deliberately NOT validated here.
                That requires reading current balance, which belongs in 
                the service layer (and inside the Serializable transaction), 
                not in a stateless input validator.
            */

            RuleFor(x => x.AccountId).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Beloppet måste vara större än 0.");
        }
    }
}