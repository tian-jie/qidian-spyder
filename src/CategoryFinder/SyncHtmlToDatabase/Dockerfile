#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["SyncHtmlToDatabase/BookFinder.SyncHtmlToDatabase.csproj", "SyncHtmlToDatabase/"]
COPY ["BookFinder.EntityFrameworkCore/BookFinder.EntityFrameworkCore.csproj", "BookFinder.EntityFrameworkCore/"]
COPY ["Domain/BookFinder.Domain.csproj", "Domain/"]
COPY ["BookFinder.Common.Tools/BookFinder.Tools.csproj", "BookFinder.Common.Tools/"]
COPY ["CategoryFinder/CategoryFinder.csproj", "CategoryFinder/"]
RUN dotnet restore "SyncHtmlToDatabase/BookFinder.SyncHtmlToDatabase.csproj"
COPY . .
WORKDIR "/src/SyncHtmlToDatabase"
RUN dotnet build "BookFinder.SyncHtmlToDatabase.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookFinder.SyncHtmlToDatabase.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookFinder.SyncHtmlToDatabase.dll"]