# Docker Deployment Guide

This repository provides Docker images for both MCP servers on Docker Hub:
- `ancplua/ancplua-mcp:workstation-latest` - WorkstationServer (stdio)
- `ancplua/ancplua-mcp:http-latest` - HttpServer (ASP.NET Core)

## Quick Start

### Using Docker Compose (Recommended)

```bash
# Set workspace path
export WORKSPACE_PATH=/path/to/your/workspace

# Start both servers
docker-compose up -d

# View logs
docker-compose logs -f

# Stop servers
docker-compose down
```

### Using Docker Run

#### WorkstationServer (stdio)

```bash
docker run -i --rm \
  -v $(pwd):/workspace \
  -v ~/.gitconfig:/root/.gitconfig:ro \
  ancplua/ancplua-mcp:workstation-latest
```

#### HttpServer

```bash
docker run -d \
  -p 5000:5000 \
  -p 5001:5001 \
  -v $(pwd):/workspace \
  -v ~/.gitconfig:/root/.gitconfig:ro \
  --name ancplua-mcp-http \
  ancplua/ancplua-mcp:http-latest
```

## Building Locally

### Build WorkstationServer

```bash
docker build -f Dockerfile.workstation -t ancplua-mcp:workstation .
```

### Build HttpServer

```bash
docker build -f Dockerfile.http -t ancplua-mcp:http .
```

### Build Both with Docker Compose

```bash
docker-compose build
```

## Multi-Platform Builds

The images are built for both AMD64 and ARM64 architectures:

```bash
# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f Dockerfile.workstation \
  -t ancplua/ancplua-mcp:workstation-latest \
  --push .
```

## Configuration

### Environment Variables

#### WorkstationServer
- `DOTNET_ENVIRONMENT`: Set to `Production` or `Development`

#### HttpServer
- `ASPNETCORE_URLS`: HTTP/HTTPS endpoints (default: `http://+:5000;https://+:5001`)
- `ASPNETCORE_ENVIRONMENT`: Set to `Production` or `Development`
- `DOTNET_ENVIRONMENT`: .NET environment setting

### Volume Mounts

Both servers require volume mounts for functionality:

1. **Workspace**: Mount your working directory to `/workspace`
   ```bash
   -v /path/to/workspace:/workspace
   ```

2. **Git Config** (optional): Mount your git config for git operations
   ```bash
   -v ~/.gitconfig:/root/.gitconfig:ro
   ```

## Using with Claude Desktop

### WorkstationServer Configuration

Add to your Claude Desktop MCP settings:

```json
{
  "mcpServers": {
    "ancplua-workstation": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-v",
        "/path/to/workspace:/workspace",
        "-v",
        "/Users/you/.gitconfig:/root/.gitconfig:ro",
        "ancplua/ancplua-mcp:workstation-latest"
      ]
    }
  }
}
```

### HttpServer Configuration

Add to your Claude Desktop MCP settings:

```json
{
  "mcpServers": {
    "ancplua-http": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

**Note**: Start the HttpServer container before using with Claude Desktop:
```bash
docker-compose up -d http-server
```

## Automated Builds

Docker images are automatically built and pushed to Docker Hub when:
- Code is pushed to `main` branch
- A version tag is created (e.g., `v1.0.0`)

Images are tagged as:
- `workstation-latest` / `http-latest` - Latest main branch
- `workstation-v1.0.0` / `http-v1.0.0` - Specific version tags
- `workstation-main` / `http-main` - Main branch builds

## Health Checks

### HttpServer Health Check

```bash
curl http://localhost:5000/health
```

The HttpServer includes a health check endpoint that returns 200 OK when healthy.

## Troubleshooting

### Container won't start

Check logs:
```bash
docker logs ancplua-mcp-http
# or
docker-compose logs http-server
```

### Git operations fail

Ensure git config is mounted:
```bash
docker run -i --rm \
  -v ~/.gitconfig:/root/.gitconfig:ro \
  ancplua/ancplua-mcp:workstation-latest
```

### File operations fail

Ensure workspace is mounted with correct permissions:
```bash
docker run -i --rm \
  -v $(pwd):/workspace \
  --user $(id -u):$(id -g) \
  ancplua/ancplua-mcp:workstation-latest
```

### HttpServer not accessible

Check port mapping:
```bash
docker ps
netstat -an | grep 5000
```

Ensure ports 5000/5001 are exposed and not blocked by firewall.

## Security Considerations

1. **Git Config**: Mounted as read-only (`:ro`)
2. **Workspace**: Grant minimal necessary permissions
3. **Network**: HttpServer only exposes ports 5000/5001
4. **User**: Containers run as root by default; consider using `--user` flag

## Performance

- Multi-stage builds minimize image size
- Layer caching speeds up rebuilds
- Multi-platform support via Docker Buildx
- GitHub Actions cache reduces CI build time

## Reference

- [Dockerfile.workstation](../Dockerfile.workstation)
- [Dockerfile.http](../Dockerfile.http)
- [docker-compose.yml](../docker-compose.yml)
- [GitHub Actions Workflow](../.github/workflows/docker-build-push.yml)
