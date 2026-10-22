#!/usr/bin/env bash
# (Re)crea HotelDB con db/HotelDB.sql en el SQL Server local (puerto 1433)
# Uso: ./scripts/cargar-bd.sh                           -> crea la BD con datos de prueba
#      ./scripts/cargar-bd.sh AgregarObservaciones.sql  -> ejecuta otro script de db/
set -euo pipefail
cd "$(dirname "$0")/.."

SCRIPT="${1:-HotelDB.sql}"

# La contraseña de sa no se sube al repo: va en scripts/.env (SA_PASSWORD=...)
[ -f scripts/.env ] && source scripts/.env
: "${SA_PASSWORD:?Crea scripts/.env con SA_PASSWORD=tu_password}"

/opt/mssql-tools18/bin/sqlcmd -C -S localhost,1433 -U sa -P "$SA_PASSWORD" -b -i "db/$SCRIPT"
echo "Listo: $SCRIPT ejecutado."
