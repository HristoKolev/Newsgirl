#!/usr/bin/env bash

. ./env.sh

docker-compose -p $stack_name logs -f
