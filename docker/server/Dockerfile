FROM mono:5

WORKDIR /app

COPY ./bin/Release .

RUN wget https://github.com/dangerousplay/hagnix-go/releases/download/1.0/kubego

COPY /docker/server/server.sh .

ENTRYPOINT ["./server.sh"]

