#!/usr/bin/env bash
dotnet build -c Release && sudo dotnet ./bin/Release/netcoreapp3.1/Newsgirl.Benchmarks.dll benchmark.net --job short --filter AsyncLocalBenchmark
sudo chown 1000:1000 /work/projects/Newsgirl/ -R