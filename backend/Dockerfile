FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:5288
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5288

# META_MODEL_API_KEY must be supplied at `docker run` time (-e) or via the
# compose file's env_file - it is never baked into the image.
ENTRYPOINT ["dotnet", "IncidentCommander.Api.dll"]
