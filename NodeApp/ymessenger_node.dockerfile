FROM ubuntu:18.04

WORKDIR /var/www/ymessenger_node
ARG UBUNTUVERSION=bionic

RUN apt-get update
RUN apt-get upgrade -y
RUN apt-get install -y apt-utils
RUN apt-get install -y libc6
RUN apt-get update && \
    apt-get install -y --allow-unauthenticated libgdiplus libc6-dev
RUN apt-get update && \
    apt-get install -y --allow-unauthenticated libx11-dev
RUN apt-get install -y openssl
RUN apt-get install -y icu-devtools
RUN apt-get install -y liblttng-ust0
RUN apt-get install -y libcurl4
RUN apt-get install -y libssl1.0.0
RUN apt-get install -y libkrb5-3
RUN apt-get install -y zlib1g
RUN apt-get install -y libicu60
RUN apt-get install -y gnupg
RUN apt-get install -y wget
RUN echo "deb http://apt.postgresql.org/pub/repos/apt/ $UBUNTUVERSION-pgdg main" | tee /etc/apt/sources.list.d/pgdg.list
RUN wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | apt-key add -
RUN apt-get update
RUN apt-get install -y postgresql-client-11

COPY . .
COPY Config /var/www/ymessenger_node/Config
RUN chmod 777 NodeApp
ENTRYPOINT ["/bin/bash", "-c" ,"./NodeApp"]