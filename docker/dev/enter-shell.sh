#!/usr/bin/env bash

containerName=$1

. ./env.sh

docker-compose -p $stack_name exec $containerName sh