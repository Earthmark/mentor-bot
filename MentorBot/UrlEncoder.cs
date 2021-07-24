using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace MentorBot
{
  public static class UrlEncoder
  {
    public static string Encode(object o)
    {
      var objTicket = JObject.FromObject(o);
      var fields = Enumerable.Select<KeyValuePair<string, JToken>, KeyValuePair<string, string>>(objTicket,
        kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString()));
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
      return objs.ToObject<T>();
    }
  }
}
