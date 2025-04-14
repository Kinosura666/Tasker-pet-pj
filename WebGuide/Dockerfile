FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN ls -la /src
RUN ls -la /src/coursach1
RUN ls -la /src/coursach1/WebGuide

WORKDIR /src/coursach1/WebGuide

RUN dotnet publish WebGuide.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebGuide.dll"]
