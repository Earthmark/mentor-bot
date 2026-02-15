using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MentorPanelTranslationPacker;

internal class Program
{
    private static void Main(string[] args)
    {
        using var file = File.CreateText("encodedLang.urlencoded.txt");
        if (Directory.Exists("mod-loc")) Directory.Delete("mod-loc", true);
        Directory.CreateDirectory("mod-loc");
        foreach (var jsonFile in Directory.EnumerateFiles("./Locales", "*.json"))
        {
            var doc = JsonConvert.DeserializeObject<LocaleDoc>(File.ReadAllText(jsonFile));
            doc.Messages = doc.Messages.ToDictionary(kvp =>
                "Earthenworks.MentorSignal." +
                string.Join(".",
                    kvp.Key.Remove(0, "locale/".Length)
                        .Split(".").Select(s =>
                            s.Substring(0, 1).ToUpper() + s.Remove(0, 1))), kvp => kvp.Value);
            File.WriteAllText($"mod-loc/{doc.LocaleCode}.json", JsonConvert.SerializeObject(doc, Formatting.Indented));
        }
    }
}

public class LocaleDoc
{
    public string LocaleCode { get; set; }
    public List<string> Authors { get; set; } = new();
    public Dictionary<string, string> Messages { get; set; } = new();
}