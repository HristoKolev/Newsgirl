#!/usr/bin/env bash

export DOCKER_BUILDKIT=1

# tarball csproj files, sln files, and NuGet.config
find . \( -name "*.csproj" -o -name "*.sln" -o -name "NuGet.config" \) -print0 \
    | tar -cvf ./projectfiles.tar --null -T -

docker build -f ./FetcherDockerfile -t xdxd-registry.lan/newsgirl-fetcher .

rm ./projectfiles.tar -f

docker push xdxd-registry.lan/newsgirl-fetcher
