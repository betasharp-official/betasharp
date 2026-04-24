FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BetaSharp.Server/BetaSharp.Server.csproj" -c ${BUILD_CONFIGURATION} -o /out

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /data

COPY --from=build /out /app
COPY BetaSharp.Ops/monitoring/server-data/ /seed-data/
COPY BetaSharp.Ops/monitoring/server/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 25565
EXPOSE 9464

ENTRYPOINT ["/entrypoint.sh"]
