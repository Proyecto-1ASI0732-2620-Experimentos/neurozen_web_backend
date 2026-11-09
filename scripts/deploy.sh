#!/bin/bash
set -e

IMAGE=$1
JWT_SECRET=$2
CANDIDATE="neurozen-backend-candidate"
PROD="neurozen-backend"
NETWORK="neurozen-network"
DB_CONN="Server=neurozen-mysql;Port=3306;Database=neurozen;User=root;Password=root;"

echo "=> Descargando imagen: $IMAGE"
docker pull $IMAGE

echo "=> Limpiando candidato anterior si existe"
docker rm -f $CANDIDATE || true

echo "=> Levantando candidato en puerto 18080 (Smoke Test)"
docker run -d --name $CANDIDATE --network $NETWORK -p 18080:10000 \
  -e ConnectionStrings__DefaultConnection="$DB_CONN" \
  -e TokenSettings__Secret="$JWT_SECRET" $IMAGE

echo "=> Esperando 20segundos para que .NET arranque..."
sleep 20

echo "=> Ejecutando Smoke Test interno..."
if curl -s -f http://127.0.0.1:18080/health; then
  echo "=> Smoke test PASÓ. Promoviendo a Producción (Puerto 8080)."
  docker rm -f $PROD || true
  
  docker run -d --name $PROD --network $NETWORK -p 8080:10000 \
    -e ConnectionStrings__DefaultConnection="$DB_CONN" \
    -e TokenSettings__Secret="$JWT_SECRET" $IMAGE
    
  docker rm -f $CANDIDATE
  echo "=> ¡Despliegue Cero-Downtime exitoso!"
else
  echo "=> Smoke test FALLÓ. Abortando despliegue."
  echo "=> Estado real del contenedor:"
  docker inspect -f 'Status: {{.State.Status}} | ExitCode: {{.State.ExitCode}} | Error: {{.State.Error}}' $CANDIDATE
  echo "=> --- LOGS COMPLETOS ---"
  docker logs $CANDIDATE 2>&1
  echo "=> ----------------------"
  echo "=> Prueba de red detallada (Curl):"
  curl -m 10 -v http://127.0.0.1:18080/health || true
  docker rm -f $CANDIDATE
  exit 1
fi