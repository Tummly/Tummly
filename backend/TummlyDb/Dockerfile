FROM mcr.microsoft.com/mssql/server:2022-latest

ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Express
# Must stay below Railway container RAM (1 GB free tier). Leave headroom for the OS.
ENV MSSQL_MEMORY_LIMIT_MB=512

USER root

COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 1433

ENTRYPOINT ["/docker-entrypoint.sh"]
