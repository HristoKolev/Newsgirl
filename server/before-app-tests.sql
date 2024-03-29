
drop schema if exists "public" cascade;
create schema "public";

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
  feed_item_added_time timestamptz(0) NOT NULL,
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
    registration_date timestamptz(0)    not null,

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

    login_date timestamptz(0) not null,
    expiration_date timestamptz(0),
    csrf_token text not null,

    login_id int not null references user_logins,
    profile_id int not null references user_profiles,

    primary key (session_id)
);
