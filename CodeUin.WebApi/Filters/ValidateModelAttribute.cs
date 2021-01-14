using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace CodeUin.WebApi.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var item = context.ModelState.Keys.ToList().FirstOrDefault();

                //返回第一个验证参数错误的信息
                context.Result = new BadRequestObjectResult(new
                {
                    Code = 400,
                    Msg = context.ModelState[item].Errors[0].ErrorMessage
                });
            }
        }
    }
}
