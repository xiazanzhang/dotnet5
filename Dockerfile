#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["CodeUin.WebApi/CodeUin.WebApi.csproj", "CodeUin.WebApi/"]
COPY ["CodeUin.Helpers/CodeUin.Helpers.csproj", "CodeUin.Helpers/"]
COPY ["CodeUin.Dapper/CodeUin.Dapper.csproj", "CodeUin.Dapper/"]
RUN dotnet restore "CodeUin.WebApi/CodeUin.WebApi.csproj"
COPY . .
WORKDIR "/src/CodeUin.WebApi"
RUN dotnet build "CodeUin.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeUin.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeUin.WebApi.dll"]