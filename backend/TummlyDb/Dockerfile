# 2019 uses less memory than 2022 on small Railway plans (1 GB free tier).
FROM mcr.microsoft.com/mssql/server:2019-latest

ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Express
ENV MSSQL_MEMORY_LIMIT_MB=384

USER root

COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN sed -i 's/\r$//' /docker-entrypoint.sh && chmod +x /docker-entrypoint.sh

EXPOSE 1433

ENTRYPOINT ["/docker-entrypoint.sh"]
