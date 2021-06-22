
drop schema if exists "public" cascade;
create schema "public";

create table system_settings (
  setting_id    serial,

  setting_name  text not null unique,
  setting_value jsonb,

  primary key (setting_id)
);

create table feeds (
  feed_id serial,

  feed_name text not NULL,
  feed_url text NOT NULL,

  feed_content_hash bigint,
  feed_items_hash bigint,

  primary key(feed_id)
);

CREATE TABLE feed_items (
  feed_item_id serial,

  feed_item_title text NOT NULL,
  feed_item_url text,

  feed_id INTEGER NOT NULL REFERENCES feeds,
  feed_item_added_time timestamp(0) NOT NULL,
  feed_item_description text,

  feed_item_string_id_hash bigint not null,
  feed_item_string_id text not null,

  PRIMARY KEY (feed_item_id)
);

create index idx__feed_items__feed_id on feed_items using btree(feed_id);
create unique index idx__feed_items__feed_item_string_id_hash on feed_items using btree(feed_item_string_id_hash);

CREATE FUNCTION get_missing_feed_items(p_feed_id int, p_new_item_hashes bigint[]) RETURNS bigint[] AS $$
  SELECT array(
    SELECT unnest(p_new_item_hashes)
    EXCEPT
    SELECT fi.feed_item_string_id_hash from feed_items fi where fi.feed_id = p_feed_id
  )
$$
language sql stable;

create table public.user_profiles (
    user_profile_id serial,

    email_address text not null,
    registration_date timestamp(0)    not null,

    primary key (user_profile_id)
);

create table public.user_logins (
    login_id serial,

    enabled boolean not null,

    username text not null,
    password_hash text not null,

    verification_code text,
    verified bool not null,

    user_profile_id int not null references user_profiles,

    primary key (login_id)
);

create table public.user_sessions (
    session_id serial,

    login_date timestamp(0) not null,
    expiration_date timestamp(0),
    csrf_token text not null,

    login_id int not null references user_logins,
    profile_id int not null references user_profiles,

    primary key (session_id)
);

--SPLIT_HERE

INSERT into public.system_settings(setting_name, setting_value) VALUES
  ('HttpClientUserAgent', '"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.81 Safari/537.36"'),
  ('FetcherCyclePause', '0'),
  ('HttpClientRequestTimeout', '120'),
  ('ParallelFeedFetching', 'true'),
  ('SessionCertificate', '"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"'),
  ('HttpServerAppLoggingConfig', '{
    "StructuredLogger": [
      {
        "Name": "GENERAL_LOG",
        "Enabled": true,
        "Destinations": [
          { "Name": "ConsoleConsumer", "Enabled": true },
          { "Name": "ElasticsearchConsumer", "Enabled": true }
        ]
      },
      {
        "Name": "HTTP_REQUESTS",
        "Enabled": false,
        "Destinations": [
          { "Name": "ElasticsearchConsumer", "Enabled": false }
        ]
      },
      {
        "Name": "HTTP_REQUESTS_DETAILED",
        "Enabled": true,
        "Destinations": [
          { "Name": "ElasticsearchConsumer", "Enabled": true }
        ]
      }
    ],
    "Elasticsearch": {
      "Url": "http://dev-host.lan:9200",
      "Username": "newsgirl",
      "Password": "test123"
    },
    "ElkIndexes": {
      "GeneralLogIndex": "newsgirl-server-general",
      "HttpLogIndex": "newsgirl-server-http"
    }
  }'),
  ('FetcherAppLoggingConfig', '{
    "StructuredLogger": [
      {
        "Name": "GENERAL_LOG",
        "Enabled": true,
        "Destinations": [
          { "Name": "ConsoleConsumer", "Enabled": true },
          { "Name": "ElasticsearchConsumer", "Enabled": true }
        ]
      },
      {
        "Name": "FETCHER_LOG",
        "Enabled": true,
        "Destinations": [
          { "Name": "ElasticsearchConsumer", "Enabled": true }
        ]
      }
    ],
    "Elasticsearch": {
      "Url": "http://dev-host.lan:9200",
      "Username": "newsgirl",
      "Password": "test123"
    },
    "ElkIndexes": {
      "GeneralLogIndex": "newsgirl-fetcher-general",
      "FetcherLogIndex": "newsgirl-fetcher-log"
    }
  }')
;
