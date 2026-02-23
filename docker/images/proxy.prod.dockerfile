FROM nginx:1.27-alpine

RUN apk add --no-cache curl \
    && rm /etc/nginx/conf.d/default.conf

COPY docker/config/prod/nginx/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
