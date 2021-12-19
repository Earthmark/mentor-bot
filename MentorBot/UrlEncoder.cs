using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MentorBot
{
  public static class UrlEncoder
  {
    public static string Encode(object o)
    {
      var fields = JObject.FromObject(o).ToObject<Dictionary<string, string>>();
      var query = QueryHelpers.AddQueryString("", fields!).Remove(0, 1);
      return query;
    }

    public static T Decode<T>(string query)
    {
      var objs = new JObject();
      foreach(var field in QueryHelpers.ParseQuery(query))
      {
        objs.Add(field.Key, new JValue(field.Value.ToString()));
      }
      return objs.ToObject<T>()!;
    }
  }
}
