using System.Text.Json;

namespace DromVehiclesParser.WorkStages.Common.Parsers.Models;

public static class WorkingParserConstruction
{
    extension(WorkingParser parser)
    {
        public static WorkingParser MapFrom<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, string> domainMap,
            Func<T, string> typeMap,
            Func<T, IReadOnlyList<WorkingParserLink>> linksMap) => WorkingParser.Create
        (
            id: idMap(source),
            domain: domainMap(source),
            type: typeMap(source),
            links: linksMap(source)
        );
        
        public static WorkingParser Create(Guid id, string domain, string type, IEnumerable<WorkingParserLink> links)
        {
            return new(
                Id: id,
                Domain: domain,
                Type: type,
                Links: [..links]
            );
        }
        
        public static WorkingParser FromJsonDocument(JsonDocument document)
        {
            Guid id = document.RootElement.GetProperty("id").GetGuid();
            string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
            string type = document.RootElement.GetProperty("parser_type").GetString()!;
            IEnumerable<WorkingParserLink> links = document.ExtractLinks(id);
            return new WorkingParser(id, domain, type, [..links]);
        }
    }

    extension(JsonDocument document)
    {
        private IEnumerable<WorkingParserLink> ExtractLinks(Guid parserId)
        {
            JsonElement linksProperty = document.RootElement.GetProperty("parser_links"); 
            int listLength = linksProperty.GetArrayLength();
            List<WorkingParserLink> links = new(listLength);
            foreach (JsonElement link in linksProperty.EnumerateArray())
            {
                Guid id = link.GetProperty("id").GetGuid();
                string url = link.GetProperty("url").GetString()!;
                links.Add(new WorkingParserLink(id, parserId, url, false, 0));
            }
            return links;
        }
    }
}