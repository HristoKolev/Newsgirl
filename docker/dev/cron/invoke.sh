#!/usr/bin/env sh
set -exu

cd /app/src/Newsgirl.WebServices

dotnet restore

dotnet run --no-build api-call "$@"
