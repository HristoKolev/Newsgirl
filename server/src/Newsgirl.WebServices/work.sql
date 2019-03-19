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
  feed_item_description text,

  PRIMARY KEY (feed_item_id)
);

-- Insert Data
 
INSERT INTO users (username, password, registration_date)
VALUES ('kenny', 'test123', current_timestamp);

INSERT into system_settings(setting_name, setting_value) VALUES
  ('WebServerPort', '5000'),
  ('HttpClientUserAgent', 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.81 Safari/537.36');

commit;
