#!/usr/bin/env bash

pg-net-generator \
    -c "Server=dev-host.lan;Port=5101;Database=newsgirl;Uid=newsgirl;Pwd=test123;" \
    -o "./Poco.cs" \
    -n "Newsgirl.Shared"
