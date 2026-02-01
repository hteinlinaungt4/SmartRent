FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SmartRent.csproj", "./"]
RUN dotnet restore "./SmartRent.csproj"
COPY . .
RUN dotnet publish "SmartRent.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .


RUN mkdir -p /app/wwwroot/uploads/properties && chmod -R 777 /app/wwwroot

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SmartRent.dll"]