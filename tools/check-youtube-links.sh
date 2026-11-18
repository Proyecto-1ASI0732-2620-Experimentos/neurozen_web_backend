#!/usr/bin/env bash
# Comprueba con el oEmbed público de YouTube (sin clave) que cada enlace de la biblioteca existe y permite insertarse.
#   200 = OK (disponible e insertable) · 401 = el dueño bloqueó la inserción · 403/404 = privado, eliminado o no existe
# Si el elegido falla, muestra la primera alternativa que funciona; cámbiala con
#   PATCH /api/v1/resource-libraries/{id} { "contentUrl": "https://www.youtube.com/watch?v=ID" } (token de ADMIN).
# Uso: bash tools/check-youtube-links.sh
set -u
dir="$(cd "$(dirname "$0")" && pwd)"
status() { curl -s -o /dev/null -w "%{http_code}" --max-time 15 \
  "https://www.youtube.com/oembed?format=json&url=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3D$1"; }
if [[ "$(status CVMBSsqTGOk)" == "000" ]]; then
  echo "Sin conexión con www.youtube.com: ejecuta el script desde una red con acceso a YouTube." >&2
  exit 2
fi
failed=0
while IFS='|' read -r title chosen alternatives; do
  [[ -z "${title// }" || "$title" == \#* ]] && continue
  code=$(status "$chosen")
  if [[ "$code" == "200" ]]; then
    echo "OK     $title -> https://www.youtube.com/watch?v=$chosen"
    continue
  fi
  failed=1
  replacement=""
  for alt in $alternatives; do
    if [[ "$(status "$alt")" == "200" ]]; then replacement="$alt"; break; fi
  done
  echo "FALLA  $title -> $chosen (HTTP $code). Alternativa: ${replacement:+https://www.youtube.com/watch?v=$replacement}${replacement:-ninguna disponible}"
done < "$dir/youtube-links.txt"
exit $failed
