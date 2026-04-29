FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NKZAPI/NKZAPI.csproj", "NKZAPI/"]
RUN dotnet restore "NKZAPI/NKZAPI.csproj"

COPY . .
RUN dotnet publish "NKZAPI/NKZAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet NKZAPI.dll --urls http://0.0.0.0:${PORT:-8080}"]
