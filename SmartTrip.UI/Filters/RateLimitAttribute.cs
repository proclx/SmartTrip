using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace SmartTrip.UI.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _maxRequests;
        private readonly int _minutes;

        public RateLimitAttribute(int maxRequests = 10, int minutes = 1)
        {
            _maxRequests = maxRequests;
            _minutes = minutes;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // ќтримуЇмо MemoryCache з серв≥с≥в
            var cache = context.HttpContext.RequestServices.GetService(typeof(IMemoryCache)) as IMemoryCache;
            if (cache == null)
            {
                base.OnActionExecuting(context);
                return;
            }

            // ќтримуЇмо IP адресу кл≥Їнта
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            // ‘ормуЇмо ун≥кальний ключ дл€ кешу
            var cacheKey = $"RateLimit_{ipAddress}_{context.ActionDescriptor.DisplayName}";

            if (cache.TryGetValue(cacheKey, out int requestCount))
            {
                if (requestCount >= _maxRequests)
                {
                    // ѕеренаправл€Їмо на стор≥нку з помилкою (≈кшен RateLimit у контролер≥ Trip)
                    context.Result = new RedirectToActionResult("RateLimit", "Trip", null);
                    return;
                }

                cache.Set(cacheKey, requestCount + 1, TimeSpan.FromMinutes(_minutes));
            }
            else
            {
                cache.Set(cacheKey, 1, TimeSpan.FromMinutes(_minutes));
            }

            base.OnActionExecuting(context);
        }
    }
}