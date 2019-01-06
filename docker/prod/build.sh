#!/usr/bin/env bash

cp ../../server ./backend-src -R
cp ../../client ./frontend-src -R

chmod 777 ./inject-json

docker-compose build && docker-compose push

rm ./backend-src -R
rm ./frontend-src -R
