#!/usr/bin/env bash
# Database First: genera/regenera Models/ a partir de HotelDB.
# Es el mismo comando de la guía (dotnet ef dbcontext scaffold).
set -euo pipefail
cd "$(dirname "$0")/.."

# La contraseña de sa no se sube al repo: va en scripts/.env (SA_PASSWORD=...)
[ -f scripts/.env ] && source scripts/.env
: "${SA_PASSWORD:?Crea scripts/.env con SA_PASSWORD=tu_password}"

cd Hotel.Web

dotnet ef dbcontext scaffold \
  "Server=localhost,1433;Database=HotelDB;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir Models \
  --context HotelContext \
  --no-onconfiguring \
  --force
