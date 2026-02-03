FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Apiconvert.Api/Apiconvert.Api.csproj src/Apiconvert.Api/
RUN dotnet restore src/Apiconvert.Api/Apiconvert.Api.csproj
COPY src/Apiconvert.Api/ src/Apiconvert.Api/
RUN dotnet publish src/Apiconvert.Api/Apiconvert.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Apiconvert.Api.dll"]
