begin;

drop schema public cascade;
create schema public;

create table system_settings (
  setting_id    serial,

  setting_name  text not null unique,
  setting_value text not null,

  primary key (setting_id)
);
--
-- create table users (
--   user_id           serial,
--
--   username          text not null unique,
--   password          text not null,
--   registration_date timestamp(0)    not null,
--
--   primary key (user_id)
-- );
--
-- create table user_sessions (
--   session_id serial,
--
--   user_id    int       not null references users,
--   login_date timestamp(0) not null,
--
--   primary key (session_id)
-- );

create table feeds (
  feed_id serial,

  feed_name text not NULL,
  feed_url text NOT NULL,

  feed_hash bigint,
  
  primary key(feed_id)
);

CREATE TABLE feed_items (
  feed_item_id serial,

  feed_item_title text NOT NULL,
  feed_item_url text,

  feed_id INTEGER NOT NULL REFERENCES feeds,
  feed_item_added_time timestamp(0) NOT NULL,
  feed_item_description text,

  feed_item_hash bigint not null,

  PRIMARY KEY (feed_item_id)
);

CREATE FUNCTION get_missing_feed_items(p_feed_id int, p_new_item_hashes bigint[]) RETURNS bigint[] AS $$
  SELECT array(
    SELECT unnest(p_new_item_hashes)
    EXCEPT
    SELECT fi.feed_item_hash from feed_items fi where fi.feed_id = p_feed_id
  )
$$
language SQL stable;

-- Insert Data
--
-- INSERT INTO users (username, password, registration_date)
-- VALUES ('hristo', 'test123', current_timestamp);

INSERT into system_settings(setting_name, setting_value) VALUES
  ('HttpClientUserAgent', 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.81 Safari/537.36'),
  ('HttpClientRequestTimeout', '120');

commit;
