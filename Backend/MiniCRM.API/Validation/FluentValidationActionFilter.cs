using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiniCRM.API.Validation;

public sealed class FluentValidationActionFilter: IAsyncActionFilter,IOrderedFilter
{
    public int Order=>-3000;
    public async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next)
    {
        foreach(var value in context.ActionArguments.Values.Where(x=>x is not null))
        {
            var validatorType=typeof(IValidator<>).MakeGenericType(value!.GetType());
            if(context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)continue;
            var validation=await validator.ValidateAsync(new ValidationContext<object>(value),context.HttpContext.RequestAborted);
            if(validation.IsValid)continue;
            var errors=validation.Errors.GroupBy(x=>string.IsNullOrEmpty(x.PropertyName)?"request":x.PropertyName).ToDictionary(x=>x.Key,x=>x.Select(v=>v.ErrorMessage).ToArray());
            var details=new ValidationProblemDetails(errors){Status=StatusCodes.Status400BadRequest,Title="One or more validation errors occurred."};
            context.Result=new BadRequestObjectResult(details);return;
        }
        await next();
    }
}
