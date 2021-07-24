using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net;

namespace MentorBot
{
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
  public class ThrottleAttribute : ActionFilterAttribute
  {
    public string Name { get; set; } = string.Empty;

    private readonly int _seconds;

    public string Message { get; set; } = "You may only perform this action every {0} seconds.";

    private static MemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

    public ThrottleAttribute(int seconds)
    {
      _seconds = seconds;
    }

    public override void OnActionExecuting(ActionExecutingContext c)
    {
      var key = string.Concat(Name, "-", c.HttpContext.Connection.RemoteIpAddress);

      if (!Cache.TryGetValue(key, out bool _))
      {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_seconds));

        Cache.Set(key, true, cacheEntryOptions);
      }
      else
      {
        c.Result = new ObjectResult(string.Format(Message, _seconds))
        {
          StatusCode = (int)HttpStatusCode.Conflict,
        };
      }
    }
  }
}
