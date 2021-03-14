#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
ADD http://ftp.us.debian.org/debian/pool/main/c/ca-certificates/ca-certificates_20210119_all.deb .
RUN dpkg -i ca-certificates_20210119_all.deb
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates && rm -rf /var/lib/apt/lists/*


WORKDIR /src
COPY ["Zhaoxi.MSACommerce.StockMicroservice/Zhaoxi.MSACommerce.StockMicroservice.csproj", "Zhaoxi.MSACommerce.StockMicroservice/"]
COPY ["StockItemInterface/StockItemInterface.csproj", "StockItemInterface/"]
COPY ["StockModel/StockModel.csproj", "StockModel/"]
COPY ["StockItemServer/StockItemServer.csproj", "StockItemServer/"]
RUN dotnet restore "Zhaoxi.MSACommerce.StockMicroservice/Zhaoxi.MSACommerce.StockMicroservice.csproj"
COPY . .
WORKDIR "/src/Zhaoxi.MSACommerce.StockMicroservice"
RUN dotnet build "Zhaoxi.MSACommerce.StockMicroservice.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Zhaoxi.MSACommerce.StockMicroservice.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zhaoxi.MSACommerce.StockMicroservice.dll"]

# docker run -itd -p 6299:80 --name stockmicroservice 
#docker build -t msastockmicroservice.v0305 -f /tzx/msaproject/Zhaoxi.MSACommerce.StockMicroservice/Dockerfile /tzx/msaproject