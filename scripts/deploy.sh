#!/usr/bin/env bash
# Despliegue candidato -> produccion para NeuroZen Backend.
# Uso: deploy.sh <imagen@digest>
# Configuracion en ~/neurozen/api.env (solo en la VM, nunca en Git).
set -euo pipefail

IMAGE="${1:?Uso: deploy.sh <imagen@digest>}"
CANDIDATE="neurozen-backend-candidate"
PROD="neurozen-backend"
NETWORK="neurozen-network"
ENV_FILE="$HOME/neurozen/api.env"
APP_PORT=10000

if [ ! -f "$ENV_FILE" ]; then
  echo "=> ERROR: no existe $ENV_FILE con la configuracion de la API."
  exit 1
fi

docker network inspect "$NETWORK" >/dev/null 2>&1 || docker network create "$NETWORK"

echo "=> Descargando imagen: $IMAGE"
docker pull "$IMAGE"

echo "=> Limpiando candidato anterior si existe"
docker rm -f "$CANDIDATE" >/dev/null 2>&1 || true

echo "=> Levantando candidato en 127.0.0.1:18080 (smoke test)"
docker run -d --name "$CANDIDATE" --network "$NETWORK" \
  --env-file "$ENV_FILE" \
  -p 127.0.0.1:18080:$APP_PORT "$IMAGE"

echo "=> Esperando a que /health responda (max. 90 s)..."
OK=0
for i in $(seq 1 30); do
  if curl -fsS http://127.0.0.1:18080/health >/dev/null 2>&1; then OK=1; break; fi
  sleep 3
done

if [ "$OK" != "1" ]; then
  echo "=> Smoke test FALLO. ROLLBACK: se mantiene la version actual."
  docker inspect -f 'Status: {{.State.Status}} | ExitCode: {{.State.ExitCode}} | Error: {{.State.Error}}' "$CANDIDATE" || true
  echo "=> --- LOGS DEL CANDIDATO ---"
  docker logs --tail 80 "$CANDIDATE" 2>&1 || true
  echo "=> --------------------------"
  docker rm -f "$CANDIDATE" >/dev/null 2>&1 || true
  exit 1
fi

echo "=> Smoke test PASO. Promoviendo a produccion (127.0.0.1:8080)."
docker rm -f "$PROD" >/dev/null 2>&1 || true
docker run -d --name "$PROD" --network "$NETWORK" \
  --env-file "$ENV_FILE" \
  --restart unless-stopped \
  -p 127.0.0.1:8080:$APP_PORT "$IMAGE"
docker rm -f "$CANDIDATE" >/dev/null 2>&1 || true

echo "=> Verificacion final..."
for i in $(seq 1 20); do
  if curl -fsS http://127.0.0.1:8080/health; then
    echo
    echo "=> Despliegue exitoso: $IMAGE"
    exit 0
  fi
  sleep 3
done

echo "=> ERROR: produccion no respondio tras la promocion."
docker logs --tail 80 "$PROD" 2>&1 || true
exit 1