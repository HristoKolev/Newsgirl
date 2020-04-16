#!/usr/bin/env bash

dotnet build -c Release && sudo dotnet ./bin/Release/netcoreapp3.1/Newsgirl.Benchmarks.dll benchmark.net --job short --filter Md5VsSha256

sudo chown 1000:1000 /work/projects/Newsgirl/ -R