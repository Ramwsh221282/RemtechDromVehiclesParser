using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace DromVehiclesParser.Parsers.Models;

public static class WorkingParserConstruction
{
    extension(WorkingParser)
    {
        public static WorkingParser FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            using JsonDocument document = JsonDocument.Parse(json);
            Guid id = document.RootElement.GetProperty("parser_id").GetGuid();
            string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
            string type = document.RootElement.GetProperty("parser_type").GetString()!;
            return new WorkingParser(id, domain, type);
        }
    }
}