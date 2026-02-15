using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace MentorBot;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ThrottleAttribute(int seconds) : ActionFilterAttribute
{
    public string Name { get; set; } = string.Empty;

    public string Message { get; set; } = "You may only perform this action every {0} seconds.";

    private static MemoryCache Cache { get; } = new(new MemoryCacheOptions());

    public override void OnActionExecuting(ActionExecutingContext c)
    {
        var key = string.Concat(Name, "-", c.HttpContext.Connection.RemoteIpAddress);
        var key2 = string.Concat(Name, "-", c.HttpContext.Connection.RemoteIpAddress, "-2");

        if (!Cache.TryGetValue(key, out bool _))
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(seconds));

            Cache.Set(key, true, cacheEntryOptions);
        }
        else if (!Cache.TryGetValue(key2, out bool _))
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(seconds));

            Cache.Set(key2, true, cacheEntryOptions);
        }
        else
        {
            c.Result = new ObjectResult(string.Format(Message, seconds))
            {
                StatusCode = (int)HttpStatusCode.Conflict
            };
        }
    }
}