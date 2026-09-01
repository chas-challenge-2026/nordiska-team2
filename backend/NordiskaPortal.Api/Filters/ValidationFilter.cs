using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NordiskaPortal.Api.Filters
{
    /*
        Runs any registered FluentValidation validator against every action
        argument automatically. Applied globally in Program.cs, so E4-US-03
        ("user queries follow a set of validation rules") covers every current
        and future request DTO without per-controller boilerplate.
    */
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                    continue;

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                        context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                    context.Result = new BadRequestObjectResult(context.ModelState);
                    return;
                }
            }

            await next();
        }
    }
}