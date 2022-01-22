#!/usr/bin/env bash

TMP_SOURCE="${BASH_SOURCE[0]}"
while [ -h "$TMP_SOURCE" ]; do
  SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"
  TMP_SOURCE="$(readlink "$TMP_SOURCE")"
  [[ $TMP_SOURCE != /* ]] && TMP_SOURCE="$SCRIPT_PATH/$TMP_SOURCE"
done
SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"

set -eu -o pipefail

# We want to cache the package restore steps when running docker build.
# This is achieved by having a restore step at the start of each Dockerfile that
# only includes files that are required to restore packages so that that step
# only runs if these files change.
#
# We do this by packaging these files in a tar archive and unpacking it in the build step.
#
# Currently these types of files are included:
#   *.csproj
#   *.sln

rm ./projectfiles.tar -f
find . \( -name "*.csproj" -o -name "*.sln" \) -print0 | tar -cvf ./projectfiles.tar --null -T -

export DOCKER_BUILDKIT=1

docker build -f ./FetcherDockerfile -t xdxd-registry.lan/newsgirl-fetcher .
docker push xdxd-registry.lan/newsgirl-fetcher

rm ./projectfiles.tar -f
