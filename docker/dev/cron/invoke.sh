#!/usr/bin/env sh
set -exu

cd /app/src/Newsgirl.Invoke

dotnet run "$@"
