#!/usr/bin/env bash
dotnet build -c Release && sudo dotnet ./bin/Release/net5.0/Newsgirl.Benchmarks.dll benchmark.net --job short --filter '*JsonSizeCountBenchmark*'
sudo chown 1000:1000 /work/projects/Newsgirl/ -R
rm /tmp/NuGetScratch/lock/ -rf