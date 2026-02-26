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

echo "4. 启动并检测 PostgreSQL..."
sudo chown -R postgres:postgres /var/lib/postgresql
sudo service postgresql start || {
    sudo mkdir -p /var/run/postgresql
    sudo chown postgres:postgres /var/run/postgresql
    sudo service postgresql start
}

# 等待数据库就绪
MAX_RETRIES=15
COUNT=0
until pg_isready -h localhost -p 5432 || [ $COUNT -eq $MAX_RETRIES ]; do
  echo "等待数据库就绪 ($COUNT/$MAX_RETRIES)..."
  sleep 2
  ((COUNT++))
done

echo "正在执行数据库初始化逻辑..."
# 定义初始化标记文件路径
INIT_MARKER="/var/lib/postgresql/.dev_init_done"

if ! sudo test -f "$INIT_MARKER"; then
    echo "检测到新数据库环境，开始初始化..."

    # --- 核心修复：无论是否有 SQL 脚本，都先确保用户和库存在 ---
    # 使用 sudo psql (root 身份) 连接到默认的 postgres 数据库执行管理命令 
    sudo psql -d postgres -c "CREATE USER selfcerts WITH SUPERUSER PASSWORD 'postgres';" || true
    sudo psql -d postgres -c "CREATE DATABASE selfcerts OWNER selfcerts;" || true

    # --- 执行额外的脚本 ---
    if [ -d "/docker-entrypoint-initdb.d" ]; then
        echo "正在运行 /docker-entrypoint-initdb.d 中的脚本..."
        for f in $(ls /docker-entrypoint-initdb.d/*.sql 2>/dev/null | sort); do
            echo "正在运行脚本: $f"
            # 指定数据库为 selfcerts 
            sudo psql -d selfcerts -f "$f"
        done
    fi

    sudo touch "$INIT_MARKER"
    echo "基础数据库环境初始化完成。"
else
    echo "数据库已存在（宿主机目录），跳过初始化。"
fi

echo "5. 安装前端依赖 (NPM)..."
# 安装 npm-check-updates 以便后续使用 ncu 命令检查前端依赖更新 (ncu -u 选项会更新 package.json 中的版本号)
npm install -g npm-check-updates
PROJECT_ROOT=$(pwd)
cd src/frontend
npm install
cd "$PROJECT_ROOT"

echo "6. 同步数据库密码 (Sync Database Password)..."
PASSWORD_FILE=".devcontainer/secrets/db_password.txt"
if [ -f "$PASSWORD_FILE" ]; then
    DB_PASS=$(cat "$PASSWORD_FILE" | tr -d '\n\r')
    # 同步密码通常是针对整个实例的，保持默认连接即可
    sudo psql -c "ALTER USER selfcerts WITH PASSWORD '$DB_PASS';"
    echo "数据库密码同步成功！"
else
    echo "警告: 未找到密码文件 $PASSWORD_FILE"
fi

echo "环境初始化完成！"