FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["neurozen.API/neurozen.API.csproj", "neurozen.API/"]
RUN dotnet restore "neurozen.API/neurozen.API.csproj"

COPY . .
RUN dotnet publish "neurozen.API/neurozen.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["sh", "-c", "dotnet neurozen.API.dll --urls http://0.0.0.0:${PORT:-10000}"]
