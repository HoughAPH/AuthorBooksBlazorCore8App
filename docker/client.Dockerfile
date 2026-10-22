# Build context is the repository root: see docker-compose.yml.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY SharedClassLibrary/SharedClassLibrary.csproj SharedClassLibrary/
COPY BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj BlazorWebAssemblyApp/
RUN dotnet restore BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj

COPY SharedClassLibrary SharedClassLibrary
COPY BlazorWebAssemblyApp BlazorWebAssemblyApp
RUN dotnet publish BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj -c Release -o /out --no-restore

FROM nginx:alpine AS final
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /out/wwwroot /usr/share/nginx/html
EXPOSE 80
