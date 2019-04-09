FROM microsoft/dotnet:2.2-sdk-alpine as backend

COPY ./backend-src /workspace

WORKDIR /workspace/src/Newsgirl.WebServices

RUN dotnet restore
RUN dotnet publish -c Release -r alpine-x64 -o /build

FROM node:alpine as frontend

COPY ./frontend-src /workspace

WORKDIR /workspace

RUN npm i

RUN npm run build

RUN cp ./dist /build -R

FROM alpine:3.8

RUN apk --no-cache add libstdc++
RUN apk --no-cache add libintl
RUN apk --no-cache add icu-libs
RUN apk --no-cache add openssl

RUN apk --no-cache add nginx
RUN apk --no-cache add supervisor

RUN mkdir -p /run/nginx

COPY ./supervisord.conf /etc/supervisord.conf
COPY ./nginx.conf /etc/nginx/conf.d/default.conf
COPY ./crontab /etc/crontabs/root

COPY --from=backend /build /app/backend
COPY --from=frontend /build /app/frontend
COPY ./inject-json /app/inject-json
COPY ./invoke /invoke

ENTRYPOINT ["supervisord"]

