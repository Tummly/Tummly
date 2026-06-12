#!/bin/bash
set -eu

echo "==> Installing Docker (Ubuntu)..."
curl -fsSL https://get.docker.com | sh
systemctl enable docker
systemctl start docker

echo "==> Installing Docker Compose plugin..."
apt-get update -qq
apt-get install -y docker-compose-plugin

echo "==> Configuring firewall..."
ufw allow OpenSSH
ufw allow 1433/tcp comment 'SQL Server QA'
echo "y" | ufw enable || ufw --force enable

if [ ! -f .env ]; then
  cp .env.example .env
  echo ""
  echo "IMPORTANT: Edit .env and set MSSQL_SA_PASSWORD before starting SQL Server."
  echo "  nano .env"
  exit 1
fi

echo "==> Starting SQL Server..."
docker compose up -d

echo ""
echo "Done. Check logs with: docker compose logs -f"
echo "Wait for: SQL Server is now ready for client connections"
