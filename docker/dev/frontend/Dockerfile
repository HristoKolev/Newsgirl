FROM node:alpine

RUN apk --no-cache add nginx
RUN apk --no-cache add supervisor
RUN mkdir -p /run/nginx

COPY ./supervisord.conf /etc/supervisord.conf
COPY ./nginx.conf /etc/nginx/conf.d/default.conf

ENTRYPOINT ["supervisord"]