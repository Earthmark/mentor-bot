using System.Collections.Generic;
using System.Linq;

namespace MentorBot
{
  public static class EnumerableExtensions
  {
    public static IEnumerable<T> OnlyNotNull<T>(this IEnumerable<T?> enumerable) where T : struct
    {
      return enumerable.Where(e => e.HasValue).Select(e => e!.Value);
    }
  }
}
