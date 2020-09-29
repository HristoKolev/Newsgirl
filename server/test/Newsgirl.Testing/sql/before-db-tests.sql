
drop schema "public" cascade;

create schema "public";

create table "public"."test1" (
	test_id serial,
	test_name1 varchar(255) not null,
	test_name2 varchar(255),
	test_date1 date not null,
	test_date2 date,
	test_timestamp1 timestamp not null,
	test_timestamp2 timestamp,
	test_boolean1 boolean not null,
	test_boolean2 boolean,
  test_integer1 integer,
  test_integer2 integer not NULL,
  test_bigint1 bigint,
  test_bigint2 bigint not NULL,
  test_text1 text,
  test_text2 text not NULL,
  test_real1 real,
  test_real2 real not NULL,
  test_double1 double precision,
  test_double2 double precision NOT NULL,
  test_decimal1 numeric,
  test_decimal2 numeric NOT NULL,
  test_char1 char,
  test_char2 char NOT NULL,

	primary key (test_id)
);

CREATE TABLE "public"."test2" (
  test_id serial,
  test_name text NOT NULL,
  test_date timestamp NOT NULL,
  test_number integer not null,
  PRIMARY KEY (test_id)
);

DROP view if exists "public"."view1";

create or REPLACE view "public"."view1" as
select test1.test_id as test1_test_id,
       test_name1,
       test_name2,
       test_date1,
       test_date2,
       test_timestamp1,
       test_timestamp2,
       test_boolean1,
       test_boolean2,
       test_integer1,
       test_integer2,
       test_bigint1,
       test_bigint2,
       test_text1,
       test_text2,
       test_real1,
       test_real2,
       test_double1,
       test_double2,
       test_decimal1,
       test_decimal2,
       test_char1,
       test_char2,
       test2.test_id as test2_test_id,
       test_name,
       test_date
from test1 JOIN test2 on test1.test_id = test2.test_id;

drop view if exists "public"."v_generate_series";
create view "public"."v_generate_series" as select generate_series num from generate_series(0, 10);

 CREATE OR REPLACE FUNCTION "public".increment_by_one(num INTEGER) RETURNS INTEGER AS $$
   SELECT num + 1;
 $$ LANGUAGE 'sql';


INSERT into public.test2 (test_name, test_date, test_number) values 
('test 1', now(), 1),
('test 2', now(), 2),
('test 3', now(), 3),
('test 4', now(), 4),
('test 5', now(), 5),
('test 6', now(), 6),
('test 7', now(), 7),
('test 8', now(), 8),
('test 9', now(), 9)
;
