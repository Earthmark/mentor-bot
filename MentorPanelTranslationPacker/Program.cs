using Newtonsoft.Json.Linq;
using System.IO;

namespace MentorPanelTranslationPacker
{
  class Program
  {
    static void Main(string[] args)
    {
      using var file = File.CreateText("encodedLang.urlencoded");
      foreach(var jsonFile in Directory.EnumerateFiles("./Locales", "*.json"))
      {
        var fileInfo = new FileInfo(jsonFile);
        var messages = JObject.Parse(File.ReadAllText(jsonFile))["messages"] as JObject;
        if (messages != null)
        {
          var message = MentorBot.UrlEncoder.Encode(messages);
          file.WriteLine($"{fileInfo.Name}:{message}");
        }
      }
    }
  }
}
