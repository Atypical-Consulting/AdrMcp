# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AdrMcp.slnx .
COPY AdrMcp/AdrMcp.csproj AdrMcp/
RUN dotnet restore AdrMcp/AdrMcp.csproj
COPY AdrMcp/ AdrMcp/
RUN dotnet publish AdrMcp/AdrMcp.csproj -c Release -o /app

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# ADRs live under /workspace/docs/adr by default; mount your repo at /workspace.
ENV ADR_REPO_ROOT=/workspace
VOLUME ["/workspace"]

# stdio transport: the MCP client launches this container and speaks over stdin/stdout.
ENTRYPOINT ["dotnet", "AdrMcp.dll"]
