FROM microsoft/dotnet:2.2-sdk-alpine

COPY ./crontab /etc/crontabs/root
COPY ./invoke /invoke

ENTRYPOINT ["/usr/sbin/crond", "-f", "-l", "8"]
