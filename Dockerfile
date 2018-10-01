FROM mono:5

WORKDIR /app

COPY /bin/Release .

ENTRYPOINT ["mono", "wServer.exe"]

# "DB_HOST" = URL to MySQL
# "DB_DATABASE" = Database used MySQL
# "DB_USER" = User of MySQL
# "DB_PASSWORD" = Pass of the user


