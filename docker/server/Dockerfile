FROM mono:5

WORKDIR /app

COPY ./bin/Release .

RUN apt-get update

RUN apt-get install -y wget

RUN wget https://github.com/dangerousplay/hagnix-go/releases/download/1.0/kubego

COPY /docker/server/server.sh .

RUN chmod +x server.sh

RUN chmod +x kubego

ENTRYPOINT ["./server.sh"]

