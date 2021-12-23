using System;

namespace MentorBot
{
  public static class DateTimeExtensions
  {
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// t: Short time (e.g 9:41 PM)
    /// T: Long Time(e.g. 9:41:30 PM)
    /// d: Short Date(e.g. 30/06/2021)
    /// D: Long Date(e.g. 30 June 2021)
    /// f(default) : Short Date/Time(e.g. 30 June 2021 9:41 PM)
    /// F: Long Date/Time(e.g.Wednesday, June, 30, 2021 9:41 PM)
    /// R: Relative Time(e.g. 2 months ago, in an hour)</remarks>
    /// <param name="dateTime"></param>
    /// <param name="timestampKind"></param>
    /// <returns></returns>
    public static string ToDiscordTimecode(this DateTime dateTime, string format)
    {
      return $"<t:{new DateTimeOffset(dateTime).ToUnixTimeSeconds()}:{format}>";
    }
  }
}
