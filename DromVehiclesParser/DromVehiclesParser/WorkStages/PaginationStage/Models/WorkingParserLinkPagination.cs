namespace DromVehiclesParser.WorkStages.PaginationStage.Models;

// CREATE TABLE IF NOT EXISTS drom_vehicles_parser.working_parser_link_pagination
// (
//     id uuid primary key,
//     link_id uuid not null,
//     url text,
//     catalogue_items_fetched boolean not null,
//     CONSTRAINT link_fk FOREIGN KEY(link_id) REFERENCES drom_vehicles_parser.working_parser_links(id)
// );

public sealed record WorkingParserLinkPagination(Guid Id, Guid LinkId, string Url, bool CatalogueItemsFetched, int RetryCount);