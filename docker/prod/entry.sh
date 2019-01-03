#!/usr/bin/env sh
set -exu

if [ "$1" = "web" ]; then 
    supervisord
elif [ "$1" = "cron" ]; then
    /usr/sbin/crond -f -l 8
else
    exit 1;
fi
