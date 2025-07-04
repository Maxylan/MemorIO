FROM httpd:2.4-alpine

RUN apk --no-cache add \
    bash \
    build-base \
    apache2 \
    apache2-utils \
    apache2-dev

RUN mkdir /var/log/apache2/mage
RUN chown www-data:www-data -R /var/log/apache2/mage
