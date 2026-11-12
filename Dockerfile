FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BrightSmileDEntal/BrightSmileDEntal.csproj", "BrightSmileDEntal/"]
RUN dotnet restore "BrightSmileDEntal/BrightSmileDEntal.csproj"
COPY . .
WORKDIR /src/BrightSmileDEntal
RUN dotnet publish "BrightSmileDEntal.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "BrightSmileDEntal.dll"]
