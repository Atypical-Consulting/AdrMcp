# Contributing to AdrMcp

Thank you for your interest in contributing to AdrMcp! AdrMcp is an MCP server
for Architectural Decision Records (ADRs) — it lets an agent navigate, author,
validate, link, and analyze decision records stored as Markdown on disk. This
document explains how to get set up and how to submit changes.

## Code of Conduct

By participating in this project, you agree to abide by our
[Code of Conduct](CODE_OF_CONDUCT.md). In short:

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Accept feedback gracefully

## Commit attribution

**Every commit in this repository MUST be authored by `phmatray@gmail.com`.**

This is a hard requirement, enforced by a tracked pre-commit hook in
`.githooks/pre-commit`. The hook inspects the recorded author identity and
rejects any commit whose author email is not `phmatray@gmail.com`.

After cloning, you MUST enable the hook and set your author identity:

```bash
git config core.hooksPath .githooks
git config user.email phmatray@gmail.com
```

The first command points Git at the tracked hooks directory so the attribution
check runs locally before each commit. The second command sets the author email
that will be recorded. Both are repo-local (`git config`, not `--global`), so
they only affect this repository. If a commit is rejected, the hook prints the
offending author and the exact command needed to fix it.

## Getting Started

### Prerequisites

1. Install the .NET 10.0 SDK or later
2. Install an IDE (Visual Studio, VS Code with the C# Dev Kit, or JetBrains Rider)
3. Fork and clone the repository

### Development Environment Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/AdrMcp.git
cd AdrMcp

# Add upstream remote
git remote add upstream https://github.com/phmatray/AdrMcp.git

# Enable the commit-attribution hook and set your author email (required)
git config core.hooksPath .githooks
git config user.email phmatray@gmail.com

# Restore, build, and test
dotnet restore AdrMcp.slnx
dotnet build AdrMcp.slnx
dotnet test AdrMcp.slnx
```

## How to Contribute

### Reporting Issues

1. Check existing issues to avoid duplicates
2. Use the issue templates (bug report / feature request)
3. Include:
   - A clear description of the problem
   - Steps to reproduce, including the MCP tool called and its parameters
   - Expected vs actual behavior
   - Environment details (AdrMcp version, .NET SDK version, MCP client, OS)
   - Relevant logs or error messages

### Suggesting Features

1. Open a discussion first for major features
2. Explain the use case and the workflow you are trying to accomplish
3. Consider implementation complexity and how it fits the existing tool surface
4. Be open to alternative approaches

### Submitting Pull Requests

1. **Create a branch** for your feature/fix:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**:
   - Follow existing code style and conventions
   - Add/update tests as needed
   - Update documentation if applicable
   - Keep commits focused and atomic
   - Ensure every commit is authored by `phmatray@gmail.com`

3. **Build and test your changes**:
   ```bash
   dotnet build AdrMcp.slnx
   dotnet test AdrMcp.slnx

   # Run a specific test
   dotnet test AdrMcp.slnx --filter "FullyQualifiedName~YourTestName"
   ```

4. **Commit your changes** using conventional commit messages:
   ```bash
   git commit -m "feat: add coverage_report grouping option"
   ```

   Common prefixes:
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation changes
   - `test:` Test additions/changes
   - `refactor:` Code refactoring
   - `perf:` Performance improvements
   - `chore:` Maintenance tasks

5. **Push and create a PR**:
   ```bash
   git push origin feature/your-feature-name
   ```
   Then open a pull request on GitHub.

## Development Guidelines

### Code Style

- Follow standard C# coding conventions
- Use meaningful names; keep methods small and single-purpose
- Add XML documentation comments for public APIs
- Use `async`/`await` for I/O (file reads/writes, provider calls)
- Prefer LINQ for collection operations where it improves readability

### Architecture Guidelines

AdrMcp is layered: **Tools → Services → Models/Interfaces**, wired via
dependency injection in `Program.cs` and hosted over stdio.

1. **MCP Tools** (`AdrMcp/Tools/`):
   - Grouped by concern: `NavigationTools`, `AuthoringTools`,
     `IntelligenceTools`, `UtilityTools`
   - Annotate tool methods with `[McpServerTool]` and describe every parameter
     with `[Description]`
   - Return responses consistently and handle errors gracefully
   - Mutating tools are **preview-by-default**: they must show the intended
     change and only apply it when the caller explicitly confirms

2. **Services** (`AdrMcp/Services/`):
   - Business logic lives here (repository, validation, graph, search,
     templates, lifecycle, code-link providers)
   - Depend on interfaces from `Interfaces/`, not concrete types

3. **Interfaces** (`AdrMcp/Interfaces/`):
   - Every swappable service has an interface (e.g. `IAdrRepository`,
     `IAdrValidator`, `ISearchService`, `ICodeLinkProvider`,
     `IEmbeddingProvider`)

4. **Storage / Models**:
   - ADRs are Markdown files in MADR 4.0 format under `docs/adr/`
   - Keep DTOs and records simple and well-documented

### Testing Guidelines

- Place tests under `AdrMcp.Tests/`, mirroring the source layout
- Name test classes with a `Tests` suffix and use descriptive method names
- Follow the Arrange-Act-Assert pattern
- Cover edge cases and error conditions, not just the happy path
- CI gates merges at **80% line coverage** — keep new code covered

## Pull Request Process

1. **Before submitting**:
   - Ensure all tests pass and coverage stays at or above 80%
   - Update documentation (README.md, CLAUDE.md, CHANGELOG.md) as needed
   - Rebase on the latest `main`
   - Confirm every commit is authored by `phmatray@gmail.com`

2. **PR description**:
   - Link related issues
   - Describe what changed and why
   - Call out any breaking changes

3. **Review process**:
   - Address reviewer feedback promptly and keep discussions professional

4. **Merge requirements**:
   - All CI checks pass (build, tests, 80% coverage gate)
   - At least one approving review
   - No unresolved conversations

## Getting Help

- **GitHub Issues**: For bug reports and feature requests
- **GitHub Discussions**: For questions and ideas

Thank you for contributing to AdrMcp!
