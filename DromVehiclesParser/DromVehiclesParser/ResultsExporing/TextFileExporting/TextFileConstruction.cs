using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;

namespace DromVehiclesParser.ResultsExporing.TextFileExporting;

public static class TextFileConstruction
{
    extension(TextFile)
    {
        public static TextFile FromDromAdvertisement(DromAdvertisementFromPage advertisement, string path)
        {
            Func<bool, string> isNdsText = isNds => isNds ? "с НДС" : "";
            Func<Dictionary<string, string>, string> characteristicsText = characteristics =>
            {
                List<string> result = new();
                foreach (KeyValuePair<string, string> item in characteristics)
                {
                    result.Add($"{item.Key}: {item.Value}");
                }
                return string.Join("\n", result);
            };
            
            
            string content = $"""
                              {advertisement.Title}
                              {advertisement.Price} {isNdsText(advertisement.IsNds)}
                              {advertisement.Address}
                              {advertisement.Url}
                              {characteristicsText(advertisement.Characteristics)}
                              """;
            
            return new TextFile(content, path);
        }
    }
}