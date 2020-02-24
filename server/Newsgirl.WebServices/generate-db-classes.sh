#!/usr/bin/env bash

schema2code \
-c "Server=home-sentry.lan;Port=4401;Database=newsgirl;Uid=newsgirl;Pwd=test123;" \
-o "./Infrastructure/Data/Poco.cs" \
-n "Newsgirl.WebServices.Infrastructure.Data"
 