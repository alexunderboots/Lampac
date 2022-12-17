FROM ubuntu:20.04

LABEL maintainer="al.tar44046@gmail.com"
LABEL description="This is custom Docker Image for Lampac Services."

RUN apt-get update && \
        apt-get install ca-certificates mc apt-utils cron curl wget unzip ffmpeg systemctl -y --no-install-recommends && \
        apt-get clean && rm -rf /var/lib/apt/lists/*
RUN wget https://raw.githubusercontent.com/immisterio/lampac/main/install.sh && \
        chmod +x install.sh && ./install.sh && \
        apt clean && rm -rf /var/lib/apt/lists/*

CMD ["systemctl", "start", "lampac"]

EXPOSE 9117
EXPOSE 9118
