FROM node:alpine

RUN apk --no-cache add nginx
RUN apk --no-cache add supervisor
RUN mkdir -p /run/nginx

COPY ./inject-settings.js /inject-settings.js
COPY ./supervisord.conf /etc/supervisord.conf
COPY ./nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

ENTRYPOINT ["supervisord"]