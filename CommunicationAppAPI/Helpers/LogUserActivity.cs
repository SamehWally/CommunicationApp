using CommunicationAppApi.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CommunicationAppApi.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();
            var userId = int.Parse(resultContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var repo = resultContext.HttpContext.RequestServices.GetService<ICommunicationRepository>();
            var user = await repo.GetUser(userId, true);
            user.LastActive = DateTime.Now;
            await repo.SaveAll();
        }
    }
}
