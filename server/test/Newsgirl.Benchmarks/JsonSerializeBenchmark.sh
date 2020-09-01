#!/usr/bin/env bash
dotnet build -c Release && sudo dotnet ./bin/Release/net5.0/Newsgirl.Benchmarks.dll benchmark.net --job short --filter JsonSerializeBenchmark
sudo chown 1000:1000 /work/projects/Newsgirl/ -R
