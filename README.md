### use tool to upgrade nuget pkg
```bash
cd src/backend
dotnet tool install --global dotnet-outdated-tool
cd ../../
dotnet outdated -u
```

### Build docker image

```bash
$SCVER = "0.0.0"

cd selfcerts
$env:APP_VER_TAG=$SCVER;docker compose -f .devcontainer/docker-compose.yml up -d --build

export APP_VER_TAG=0.0.0
APP_DB_PORT=127.0.0.1:0 APP_WEB_PORT=18083 APP_PROD_TAG=preview docker compose -f .devcontainer/docker-compose.yml up -d --build

docker build -f Dockerfile --build-arg APP_VER_TAG=$SCVER -t $REGISTRY/selfcerts:latest -t $REGISTRY/selfcerts:$SCVER .
```

### Preview (端口 18083):

```bash
# export SCVER=0.0.0-dev
# docker pull selfcerts:$SCVER

export SCVER=0.0.0
APP_DB_PORT=127.0.0.1:0 APP_VER_TAG=$SCVER APP_WEB_PORT=18083 APP_PROD_TAG=preview docker compose -f /app/selfcerts/docker-compose-selfcerts.yml -p selfcerts-preview up -d --pull always
```

### Production release (端口 8083)

```bash
export SCVER=0.0.0
APP_DB_PORT=127.0.0.1:0 APP_VER_TAG=$SCVER APP_WEB_PORT=8083 APP_PROD_TAG=prod docker compose -f /app/selfcerts/docker-compose-selfcerts.yml -p selfcerts-prod up -d --pull always
```