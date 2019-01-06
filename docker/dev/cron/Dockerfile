FROM microsoft/dotnet:2.2-sdk-alpine

COPY crontab.txt /etc/crontabs/root

COPY ./entry.sh /entry.sh
COPY ./invoke.sh /invoke.sh

CMD ["/entry.sh"]
