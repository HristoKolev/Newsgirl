#!/usr/bin/env bash

TMP_SOURCE="${BASH_SOURCE[0]}"
while [ -h "$TMP_SOURCE" ]; do
  SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"
  TMP_SOURCE="$(readlink "$TMP_SOURCE")"
  [[ $TMP_SOURCE != /* ]] && TMP_SOURCE="$SCRIPT_PATH/$TMP_SOURCE"
done
SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"

set -eu -o pipefail

run-poco-generator() {
    dotnet tool run xdxd-dotnet-postgres-poco-generator -- "$@" >&1
}

CONNECTION_STRING="Server=xdxd-db-playground;Port=6000;Database=newsgirl;Uid=newsgirl;Pwd=6871091a-6c53-11ec-bbc6-479f17e49937;";

run-poco-generator -c $CONNECTION_STRING -n "Newsgirl.Shared" -o- > "./Poco.cs"
