begin;

drop schema public cascade;
create schema public;

create table system_settings (
  setting_id    serial,
  setting_name  varchar(255) not null unique,
  setting_value varchar(255) not null,
  primary key (setting_id)
);

create table users (
  user_id           serial,
  username          varchar(255) not null unique check (username = trim(lower(username))),
  password          varchar(255) not null,
  registration_date timestamp    not null,
  primary key (user_id)
);

create table user_sessions (
  session_id serial,
  user_id    int       not null references users,
  login_date timestamp not null,
  primary key (session_id)
);

create table feeds (
  feed_id serial,
  feed_name text not NULL,
  feed_url text NOT NULL,
  feed_last_failed_time timestamp,
  feed_last_failed_reason text,
  
  primary key(feed_id)
);

CREATE TABLE feed_items (
  feed_item_id serial,
  feed_item_title text NOT NULL,
  feed_item_url text,
  feed_id INTEGER NOT NULL REFERENCES feeds,
  feed_item_added_time timestamp NOT NULL,

  PRIMARY KEY (feed_item_id)
);

-- Views

drop view if exists "public"."db_columns";
create view "public"."db_columns" as
  ((SELECT
      t.tablename as TableName,
      n.nspname AS TableSchema,
      a.attname as ColumnName,
      pg_catalog.format_type(a.atttypid, NULL) AS DbDataType,
      pg_catalog.col_description(a.attrelid, a.attnum) AS ColumnComment,
      (a.attnotnull = FALSE) AS IsNullable,
      p.conname AS PrimaryKeyConstraintName,
      f.conname AS ForeignKeyConstraintName,
      fc.relname as ForeignKeyReferenceTableName,
      CASE WHEN pg_catalog.pg_get_constraintdef(f.oid) LIKE 'FOREIGN KEY %'
              THEN substring(pg_catalog.pg_get_constraintdef(f.oid),
                  position('(' in substring(pg_catalog.pg_get_constraintdef(f.oid), 14))+14, position(')'
                  in substring(pg_catalog.pg_get_constraintdef(f.oid), position('(' in
                  substring(pg_catalog.pg_get_constraintdef(f.oid), 14))+14))-1) END AS ForeignKeyReferenceColumnName,
      fn.nspname as ForeignKeyReferenceSchemaName,
      FALSE as IsViewColumn

      FROM pg_catalog.pg_class c
      JOIN pg_catalog.pg_tables t ON c.relname = t.tablename
      JOIN pg_catalog.pg_attribute a ON c.oid = a.attrelid AND a.attnum > 0
      JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
      LEFT JOIN pg_catalog.pg_constraint p ON p.contype = 'p'::char AND p.conrelid = c.oid AND (a.attnum = ANY (p.conkey))
      LEFT JOIN pg_catalog.pg_constraint f ON f.contype = 'f'::char AND f.conrelid = c.oid AND (a.attnum = ANY (f.conkey))
      LEFT JOIN pg_catalog.pg_class fc on fc.oid = f.confrelid
      LEFT JOIN pg_catalog.pg_namespace fn on fn.oid = fc.relnamespace

      WHERE a.atttypid <> 0::oid AND (n.nspname != 'information_schema' AND n.nspname NOT LIKE 'pg_%') AND c.relkind = 'r')
  UNION
  (SELECT
      t.viewname as TableName,
      n.nspname AS TableSchema,
      a.attname as ColumnName,
      pg_catalog.format_type(a.atttypid, NULL) AS DbDataType,
      pg_catalog.col_description(a.attrelid, a.attnum) AS ColumnComment,
      (a.attnotnull = FALSE) AS IsNullable,
      null AS PrimaryKeyConstraintName,
      null AS ForeignKeyConstraintName,
      null as ForeignKeyReferenceTableName,
      null as ForeignKeyReferenceColumnName,
      null as ForeignKeyReferenceSchemaName,
      true as IsViewColumn

      FROM pg_catalog.pg_class c
      JOIN pg_catalog.pg_views t ON c.relname = t.viewname
      JOIN pg_catalog.pg_attribute a ON c.oid = a.attrelid AND a.attnum > 0
      JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace

      WHERE a.atttypid <> 0::oid
        AND (n.nspname != 'information_schema' AND n.nspname NOT LIKE 'pg_%')
        and not (n.nspname = 'public' and t.viewname = 'db_columns')
  )) ORDER BY TableSchema, IsViewColumn, TableName, ColumnName;
 
-- Insert Data
 
INSERT INTO users (username, password, registration_date)
VALUES ('kenny', 'test123', current_timestamp);

commit;
