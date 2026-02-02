FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Apiconvert.Core/Apiconvert.Core.csproj src/Apiconvert.Core/
RUN dotnet restore src/Apiconvert.Core/Apiconvert.Core.csproj
COPY src/Apiconvert.Core/ src/Apiconvert.Core/
RUN dotnet build src/Apiconvert.Core/Apiconvert.Core.csproj -c Release

CMD ["dotnet", "build", "src/Apiconvert.Core/Apiconvert.Core.csproj", "-c", "Release"]
