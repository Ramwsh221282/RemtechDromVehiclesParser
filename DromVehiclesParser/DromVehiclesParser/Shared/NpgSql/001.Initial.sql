CREATE SCHEMA IF NOT EXISTS drom_vehicles_parser;

CREATE TABLE IF NOT EXISTS drom_vehicles_parser.registration_tickets
(
  id uuid primary key,
  was_sent boolean not null,
  finished timestamptz 
);