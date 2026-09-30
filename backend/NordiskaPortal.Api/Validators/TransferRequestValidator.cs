using FluentValidation;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Validators
{
    public class TransferRequestValidator : AbstractValidator<TransferRequest>
    {
        public TransferRequestValidator()
        {
            RuleFor(x => x.FromAccountId).GreaterThan(0);
            RuleFor(x => x.ToAccountId).GreaterThan(0)
                .NotEqual(x => x.FromAccountId).WithMessage("Från- och tillkonto måste vara olika.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Beloppet måste vara större än 0.");
            RuleFor(x => x.Description)
                .MaximumLength(100).WithMessage("Beskrivningen får vara högst 100 tecken.");
        }
    }
}