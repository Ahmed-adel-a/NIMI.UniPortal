# The Cairo University host (core + CreditHours + Forum + Events) as one image (decision DEVDOCKER).
# Build from the repository root:
#   docker build -f deployments/cairo-university/Dockerfile -t nimi-uniportal-cairo .
# Runs on port 8080 as the non-root `app` user. Configuration comes from environment variables
# (ConnectionStrings__Portal, Auth__Jwt__SigningKey, …); nothing secret is baked in.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Hosts/Host.CairoUniversity/Host.CairoUniversity.csproj \
    -c Release -o /app -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Uploaded files (Storage:RootPath) live on a volume mounted here; the app user must own it.
RUN mkdir -p /data/files && chown -R $APP_UID /data
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Storage__RootPath=/data/files
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Host.CairoUniversity.dll"]
