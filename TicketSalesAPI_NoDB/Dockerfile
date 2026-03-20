FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TicketSalesAPI_NoDB.csproj", "."]
RUN dotnet restore "TicketSalesAPI_NoDB.csproj"
COPY . .
RUN dotnet publish "TicketSalesAPI_NoDB.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TicketSalesAPI_NoDB.dll"]