#!/bin/bash
set -e # 出错立即停止

echo "1. 正在还原后端依赖 (Restore .NET)..."
dotnet restore SelfCerts.slnx

echo "2. 配置 .NET HTTPS 开发证书..."
dotnet dev-certs https --trust

echo "3. 配置 Git..."
git config --global core.filemode false
git config --global core.autocrlf input
git config --global --add safe.directory '*'

echo "4. 安装前端依赖 (NPM)..."
# 安装 npm-check-updates 以便后续使用 ncu 命令检查前端依赖更新 (ncu -u 选项会更新 package.json 中的版本号)
npm install -g npm-check-updates
PROJECT_ROOT=$(pwd)
cd src/frontend
npm install
cd "$PROJECT_ROOT"

echo "环境初始化完成！"