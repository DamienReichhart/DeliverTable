FROM nginx:1.31-alpine

RUN apk add --no-cache curl

RUN rm /etc/nginx/conf.d/default.conf

COPY docker/config/dev/nginx/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
