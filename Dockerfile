FROM node:20-alpine AS frontend
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend
WORKDIR /src
COPY backend/src/Woah.Api/Woah.Api.csproj ./Woah.Api/
RUN dotnet restore Woah.Api/Woah.Api.csproj
COPY backend/src/Woah.Api/ ./Woah.Api/
RUN dotnet publish Woah.Api/Woah.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=backend /app .
COPY --from=frontend /app/dist ./wwwroot
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Woah.Api.dll"]