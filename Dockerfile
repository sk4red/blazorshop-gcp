#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR .
COPY ["BlazorShop.Web/Server/BlazorShop.Web.Server.csproj", "BlazorShop.Web/Server/"]
COPY ["BlazorShop.Models/BlazorShop.Models.csproj", "BlazorShop.Models/"]
COPY ["BlazorShop.Data/BlazorShop.Data.csproj", "BlazorShop.Data/"]
COPY ["BlazorShop.Common/BlazorShop.Common.csproj", "BlazorShop.Common/"]
COPY ["BlazorShop.Services/BlazorShop.Services.csproj", "BlazorShop.Services/"]
COPY ["BlazorShop.Web/Client/BlazorShop.Web.Client.csproj", "BlazorShop.Web/Client/"]
RUN dotnet restore "BlazorShop.Web/Server/BlazorShop.Web.Server.csproj"
COPY . .
WORKDIR "/BlazorShop.Web/Server"

RUN dotnet build "BlazorShop.Web.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorShop.Web.Server.csproj" -c Release -o /app/publish

FROM base AS final
ENV MySQLConnection="dummy"

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorShop.Web.Server.dll"]