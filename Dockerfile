# ================================
# Stage 1: 构建前端 (Vue)
# ================================
FROM node:24 AS frontend-build
WORKDIR /src/frontend

# 安装依赖 (利用 Docker 缓存)
COPY src/frontend/package*.json ./
RUN npm install

# 复制源码并构建
COPY src/frontend/ .
# 注入版本号 (Vite 环境变量)
ARG APP_VER_TAG=1.0.0
# 使用 :- 语法，防止传入空字符串导致环境变量为空
ENV VITE_APP_VERSION=${APP_VER_TAG:-1.0.0}

RUN npm run build

# Generate version info file (used for frontend polling)
RUN echo "{\"version\": \"${VITE_APP_VERSION}\", \"buildTime\": \"$(date -u +'%Y-%m-%dT%H:%M:%SZ')\"}" > dist/version.json

# ================================
# Stage 2: 构建后端 (.NET)
# ================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /source

# 复制源码并还原依赖 (保持目录结构以匹配 slnx 路径)
COPY SelfCerts.slnx ./
COPY src/backend/ src/backend/
RUN dotnet restore "SelfCerts.slnx"

# 发布
WORKDIR /source/src/backend/SelfCerts.Api

ARG APP_VER_TAG=1.0.0
# 使用 ${APP_VER_TAG:-1.0.0} 确保即使传入空值也能正常构建
RUN dotnet publish "SelfCerts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:Version=${APP_VER_TAG:-1.0.0}

# ================================
# Stage 3: 最终运行时
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 1. 复制后端发布产物 (直接设置所有者，避免后续 chown 产生额外层)
COPY --from=backend-build --chown=selfcerts:selfcerts /app/publish .
# 2. 复制前端构建产物到 wwwroot
COPY --from=frontend-build --chown=selfcerts:selfcerts /src/frontend/dist ./wwwroot

# 安装 OpenSSL 依赖
RUN apt-get update && apt-get install -y openssl && rm -rf /var/lib/apt/lists/*


# 配置环境
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "SelfCerts.Api.dll"]