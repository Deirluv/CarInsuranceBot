FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY *.sln .
COPY CarInsuranceBot/*.csproj ./CarInsuranceBot/
RUN dotnet restore

COPY . .
WORKDIR /app/CarInsuranceBot
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]