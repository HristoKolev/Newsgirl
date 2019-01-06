#!/usr/bin/env sh
set -exu

cd /app/src/Newsgirl.ApiInvoke

dotnet run "$@"
