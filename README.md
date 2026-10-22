# Hotel — ASP.NET Core MVC + EF Core (Database First)

Laboratorio **Semana 10** — Desarrollo de Aplicaciones Empresariales Avanzadas (Tecsup).

- Base de datos primero: el esquema lo define `db/HotelDB.sql` (sin migraciones ni procedimientos almacenados).
- Modelo generado con **scaffold** en `Models/` con el contexto `HotelContext` (no se edita a mano).
- Acceso a datos solo en `Repositorios/` (`HabitacionRepositorio`, `ReservaRepositorio`); los controladores no usan el `DbContext`.
- Eliminación lógica (`Activo = 0`), patrón Post/Redirect/Get y mensajes con `TempData`.
- Todo async (`ToListAsync`, `AnyAsync`, `SaveChangesAsync`) y lecturas con `AsNoTracking()`.

## Requisitos

- .NET SDK 10
- SQL Server 2022 en `localhost,1433`
- VS Code con C# Dev Kit y SQL Server (mssql)

## Ejecutar

```bash
./scripts/cargar-bd.sh          # crea HotelDB con datos de prueba
cd Hotel.Web
dotnet run                      # http://localhost:5080
```

## Scaffold (Database First)

```bash
dotnet tool install --global dotnet-ef
cd Hotel.Web
dotnet ef dbcontext scaffold \
  "Server=localhost,1433;Database=HotelDB;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir Models --context HotelContext --no-onconfiguring --force
```

- `--no-onconfiguring` evita que se genere `OnConfiguring`; la cadena está en `appsettings.json`.
- `--force` sobrescribe `Models/` al regenerar (no hace falta borrar la carpeta).

## Funcionalidad

| Módulo | Acciones |
|---|---|
| Habitaciones | Index (filtro por tipo, orden por número), Details, Create, Edit, Delete lógico. Número duplicado → `AddModelError`. |
| Disponibilidad | `Reservas/Disponibles?ingreso=&salida=`: habitaciones activas libres con noches y total estimado, en una sola consulta LINQ con `!Any`. |
| Reservas | Create (valida rango, ingreso ≥ hoy y cruces; se guarda como Pendiente), Index con total y Cancelar (solo Pendiente o Confirmada). |

Una reserva se cruza cuando `Estado != "Cancelada"`, `FechaIngreso < salida pedida` y `FechaSalida > ingreso pedido`.

## Cambio de esquema

```bash
./scripts/cargar-bd.sh AgregarObservaciones.sql   # ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL
./scripts/scaffold.sh                             # aparece Reserva.Observaciones en Models/
```

Luego se agrega `Observaciones` al `[Bind]` de `ReservasController.Create` y al formulario `Views/Reservas/Create.cshtml`.
