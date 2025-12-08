using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace DromVehiclesParser.Shared.Extensions;

public static class JsonDocumentExtensions
{
    extension(JsonDocument)
    {
        public static JsonDocument FromBasicDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            return JsonDocument.Parse(Encoding.UTF8.GetString(ea.Body.ToArray()));
        }
    }
}