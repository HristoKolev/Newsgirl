#!/usr/bin/env bash

. ./env.sh

docker-compose -p $stack_name down 
docker-compose -p $stack_name up --build -d