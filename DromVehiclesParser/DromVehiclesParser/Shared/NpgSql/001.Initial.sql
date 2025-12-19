CREATE SCHEMA IF NOT EXISTS drom_vehicles_parser;

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.registration_tickets
(
  id uuid primary key,
  was_sent boolean not null,
  finished timestamptz 
);

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.work_stages
(
    id uuid primary key,
    stage_name varchar(64),
    finished boolean not null
);

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.working_parsers
(
    id uuid primary key,
    domain varchar(128),
    type varchar(128)
);

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.working_parser_links
(
    id uuid primary key,
    url text,
    processed boolean not null,
    retry_count integer not null
);

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.catalogue_pages
(
    url text,
    processed boolean not null,
    retry_count integer not null
);

-- public sealed record DromAdvertisementFromPage(
--     string Id,
--     string Url,
--     Dictionary<string, string> Characteristics,
--     long Price,
--     bool IsNds,
--     string Title,
--     string Address,
--     IReadOnlyList<string> Photos
-- );

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.items
(
  id varchar(128) primary key,
  url text,
  photos jsonb not null,
  retry_count integer not null,
  processed boolean not null,
  characteristics jsonb,
  price bigint,
  is_nds boolean,
  title text,
  address text    
);