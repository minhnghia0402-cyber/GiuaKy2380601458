FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/SV2380601458.Web.Mvc/SV2380601458.Web.Mvc.csproj", "src/SV2380601458.Web.Mvc/"]
COPY ["src/SV2380601458.Web.Core/SV2380601458.Web.Core.csproj", "src/SV2380601458.Web.Core/"]
COPY ["src/SV2380601458.Application/SV2380601458.Application.csproj", "src/SV2380601458.Application/"]
COPY ["src/SV2380601458.Core/SV2380601458.Core.csproj", "src/SV2380601458.Core/"]
COPY ["src/SV2380601458.EntityFrameworkCore/SV2380601458.EntityFrameworkCore.csproj", "src/SV2380601458.EntityFrameworkCore/"]
WORKDIR "/src/src/SV2380601458.Web.Mvc"
RUN dotnet restore 

WORKDIR /src
COPY ["src/SV2380601458.Web.Mvc", "src/SV2380601458.Web.Mvc"]
COPY ["src/SV2380601458.Web.Core", "src/SV2380601458.Web.Core"]
COPY ["src/SV2380601458.Application", "src/SV2380601458.Application"]
COPY ["src/SV2380601458.Core", "src/SV2380601458.Core"]
COPY ["src/SV2380601458.EntityFrameworkCore", "src/SV2380601458.EntityFrameworkCore"]
WORKDIR "/src/src/SV2380601458.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "SV2380601458.Web.Mvc.dll"]
