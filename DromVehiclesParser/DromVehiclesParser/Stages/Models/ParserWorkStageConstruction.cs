using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace DromVehiclesParser.Stages.Models;

public static class ParserWorkStageConstruction
{
    extension(ParserWorkStage)
    {
        public static ParserWorkStage FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            using JsonDocument document = JsonDocument.Parse(json);
            Guid id = document.RootElement.GetProperty("id").GetGuid();
            return new ParserWorkStage(id, "", false);
        }
        
        public static ParserWorkStage InitialPagination(Guid id)
        {
            return new ParserWorkStage(Id: id, ParserWorkStageConstants.PAGINATION, false);
        }

        public static ParserWorkStage MapFrom<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, string> nameMap,
            Func<T, bool> finishedMap)
        {
            return new ParserWorkStage(
                Id: idMap(source),
                StageName: nameMap(source),
                Finished: finishedMap(source)
            );
        }
    }
}