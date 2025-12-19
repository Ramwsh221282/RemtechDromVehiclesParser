using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace DromVehiclesParser.Parsers.Models;

public static class WorkingParserLinkConstruction
{
    extension(WorkingParserLink)
    {
        public static WorkingParserLink[] FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement linksProperty = document.RootElement.GetProperty("parser_links"); 
            int listLength = linksProperty.GetArrayLength();
            WorkingParserLink[] links = new WorkingParserLink[listLength];
            int index = 0;
            foreach (JsonElement link in linksProperty.EnumerateArray())
            {
                Guid id = link.GetProperty("id").GetGuid();
                string url = link.GetProperty("url").GetString()!;
                links[index++] = new WorkingParserLink(id, url, false, 0);
            }
            return links; 
        }
    }
}