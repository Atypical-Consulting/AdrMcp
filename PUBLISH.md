# Publishing a release

Releases are fully automated from a git tag. Versioning is derived from the tag by
[MinVer](https://github.com/adamralph/minver) (`v1.2.3` → package `1.2.3`).

## One-time setup

Repository **Settings → Secrets and variables → Actions**:

| Secret | Used by | Purpose |
| --- | --- | --- |
| `NUGET_API_KEY` | `publish-nuget.yml` | Push the package to NuGet.org |

GHCR (Docker) and the MCP Registry use the built-in `GITHUB_TOKEN` / OIDC — no extra secrets.
The MCP Registry namespace `io.github.Atypical-Consulting` is proven at publish time via GitHub OIDC, so
the repository owner must be `Atypical-Consulting` (or update the name in `.mcp/server.json`).

## Cut a release

1. Update `CHANGELOG.md`: move the relevant notes from `## [Unreleased]` into a new
   `## [X.Y.Z] - YYYY-MM-DD` section. The release notes are extracted from this section.
2. Tag and push:

   ```bash
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```

That triggers, on the `v*` tag:

- **Publish NuGet** — stamps `.mcp/server.json` to the tag version, builds, tests, packs, verifies
  the manifest is embedded, pushes to NuGet.org, builds the `.mcpb` bundle, creates a GitHub Release
  (with the `.nupkg` and `.mcpb` attached), then publishes the manifest to the MCP Registry.
- **Docker Publish** — builds and pushes a multi-arch image to `ghcr.io/atypical-consulting/adr-mcp`.

## Install channels

```bash
# .NET global tool
dotnet tool install -g AdrMcp

# on-demand via dnx (no install; requires the .NET 10 SDK)
dnx AdrMcp --yes

# Docker
docker run --rm -i -v "$PWD:/workspace" ghcr.io/atypical-consulting/adr-mcp --adr-root /workspace/docs/adr
```
