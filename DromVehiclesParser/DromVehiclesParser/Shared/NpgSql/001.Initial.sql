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
    parser_id uuid not null,
    url text,
    pagination_calculated boolean not null,
    retry_count integer not null,
    CONSTRAINT parser_fk FOREIGN KEY(parser_id) REFERENCES drom_vehicles_parser.working_parsers(id)
        ON DELETE CASCADE 
);

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.working_parser_link_pagination
(
    id uuid primary key,
    link_id uuid not null,
    url text,
    catalogue_items_fetched boolean not null,
    retry_count integer not null,
    CONSTRAINT link_fk FOREIGN KEY(link_id) REFERENCES drom_vehicles_parser.working_parser_links(id)
);

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.catalogue_items
(
    id varchar(64) primary key,
    url text not null,
    photos jsonb not null,
    processed boolean not null,
    retry_count integer not null
);