#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["CategoryFinder/CategoryFinder.csproj", "CategoryFinder/"]
RUN dotnet restore "CategoryFinder/CategoryFinder.csproj"
COPY . .
WORKDIR "/src/CategoryFinder"
RUN dotnet build "CategoryFinder.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CategoryFinder.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CategoryFinder.dll"]