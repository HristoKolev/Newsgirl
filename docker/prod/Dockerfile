FROM microsoft/dotnet:2.2-sdk-alpine

COPY ./backend-src /workspace

WORKDIR /workspace/src/Newsgirl.WebServices

RUN dotnet restore
RUN dotnet publish -c Release -r alpine-x64 -o /build

FROM node:alpine

COPY ./frontend-src /workspace

WORKDIR /workspace

RUN npm i

RUN npm run build

RUN cp ./dist /build -R

FROM microsoft/dotnet:2.2-sdk-alpine

COPY ./backend-src /workspace

WORKDIR /workspace/src/Newsgirl.Invoke

RUN dotnet restore
RUN dotnet publish -c Release -r alpine-x64 -o /build

FROM alpine:3.8

RUN apk --no-cache add libstdc++
RUN apk --no-cache add libintl
RUN apk --no-cache add icu-libs
RUN apk --no-cache add openssl

RUN apk --no-cache add nginx
RUN apk --no-cache add supervisor

COPY ./ssh_config /etc/ssh/

RUN mkdir -p /run/nginx

COPY ./supervisord.conf /etc/supervisord.conf
COPY ./nginx.conf /etc/nginx/conf.d/default.conf
COPY ./crontab.txt /etc/crontabs/root

COPY --from=0 /build /app/backend
COPY --from=1 /build /app/frontend
COPY --from=2 /build /app/newsgirl-invoke
COPY ./inject-json /app/inject-json
COPY ./invoke.sh /invoke.sh
COPY ./entry.sh /entry.sh

ENTRYPOINT ["/entry.sh"]
