# HotelApp — Reservas de hotel con ASP.NET Core MVC y EF Core (Database First)

Laboratorio de la **Semana 10** del curso *Desarrollo de Aplicaciones Empresariales Avanzadas* (Tecsup).

Aplicación web para gestionar las habitaciones de un hotel, consultar disponibilidad por fechas y registrar reservas.
La base de datos ya existe (script `db/HotelDB.sql`) y a partir de ella genero el modelo con **scaffold**,
sin migraciones ni procedimientos almacenados.

**Autor:** Alex Casapaico

## Tecnologías

- .NET 10 · ASP.NET Core MVC
- Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer` y `.Design`)
- SQL Server 2022
- VS Code con C# Dev Kit y la extensión SQL Server (mssql)

## Estructura

```
HotelApp/
├── db/
│   ├── HotelDB.sql                 esquema + datos de prueba (script del docente)
│   └── AgregarObservaciones.sql    ALTER TABLE del último paso
├── scripts/
│   ├── cargar-bd.sh                ejecuta un script de db/ con sqlcmd
│   ├── scaffold.sh                 regenera Models/
│   └── comandos.txt                comandos usados en el laboratorio
└── Hotel.Web/
    ├── Models/                     generado con scaffold (no se edita a mano)
    ├── Repositorios/               HabitacionRepositorio · ReservaRepositorio
    ├── Controllers/                Home · Habitaciones · Reservas
    └── Views/
```

## Cómo ejecutarlo

La contraseña de `sa` no está en el repositorio. Antes de la primera ejecución:

```bash
# 1. Para los scripts
echo 'SA_PASSWORD=tu_password' > scripts/.env

# 2. Para la aplicación (se guarda fuera del repo con user-secrets)
cd Hotel.Web
dotnet user-secrets set "ConnectionStrings:Hotel" "Server=localhost,1433;Database=HotelDB;User Id=sa;Password=tu_password;TrustServerCertificate=True"
cd ..
```

Luego:

```bash
./scripts/cargar-bd.sh                            # crea HotelDB con datos de prueba
./scripts/cargar-bd.sh AgregarObservaciones.sql   # agrega la columna Observaciones
cd Hotel.Web
dotnet run                                        # http://localhost:5080
```

En VS Code también se puede abrir la carpeta y presionar **F5**.

## Comando de scaffold utilizado

```bash
dotnet tool install --global dotnet-ef

cd Hotel.Web
dotnet ef dbcontext scaffold "Server=localhost,1433;Database=HotelDB;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext --no-onconfiguring --force
```

- `--context HotelContext`: nombre del contexto que pide la guía.
- `--no-onconfiguring`: no genera el método `OnConfiguring`, porque la cadena de conexión va en la configuración y el contexto se registra con `AddDbContext` en `Program.cs`.
- `--force`: sobrescribe los archivos de `Models/` cuando se regenera. No hace falta borrar la carpeta; si se borra, el proyecto deja de compilar y el scaffold falla.
- `TrustServerCertificate=True`: sin esto SQL Server rechaza la conexión por el certificado autofirmado.

## Qué hace la aplicación

**Habitaciones**
- Listado de habitaciones activas con su tipo, filtro por tipo y orden por número.
- Detalle, registro y edición con lista desplegable de tipos.
- Si el número ya existe, se muestra el error en el formulario (`ModelState.AddModelError`).
- Eliminación lógica (`Activo = 0`). No deja eliminar una habitación con reservas pendientes o confirmadas.

**Disponibilidad** (`/Reservas/Disponibles?ingreso=...&salida=...`)
- Muestra las habitaciones activas libres en el rango, con número de noches y total estimado.
- Se resuelve en el repositorio con una sola consulta LINQ usando `!Any`: una reserva se cruza cuando no está cancelada,
  su `FechaIngreso` es menor que la salida pedida y su `FechaSalida` es mayor que el ingreso pedido.

**Reservas**
- Registro con listas de huéspedes y habitaciones activas. Valida que la salida sea posterior al ingreso,
  que el ingreso no sea anterior a hoy y que la habitación no esté ocupada en ese rango. Se guarda como *Pendiente*.
- Listado con huésped, habitación, fechas, estado y total; permite cancelar las reservas *Pendiente* o *Confirmada*.
- Campo **Observaciones**, agregado al final con `ALTER TABLE` y regenerando el modelo.

En todos los formularios uso el patrón Post/Redirect/Get y muestro el resultado con `TempData`.

## Observaciones

- El scaffold mapeó las columnas `DATE` de SQL Server a `DateOnly`, así que el número de noches lo calculo con
  `FechaSalida.DayNumber - FechaIngreso.DayNumber`.
- Los nombres de los `DbSet` salen con la "s" del inglés (`Habitacions`, `Huespeds`, `TipoHabitacions`). No los cambié
  porque los archivos de `Models/` no se deben modificar: se perderían al regenerar.
- `ModelState.IsValid` fallaba por las propiedades de navegación (`IdHabitacionNavigation`, `IdHuespedNavigation`)
  y por `Estado`, que no vienen del formulario. Las quité con `ModelState.Remove`.
- Con .NET instalado por snap en Ubuntu, `dotnet ef` no arrancaba hasta definir `DOTNET_ROOT=/var/snap/dotnet/common/dotnet`
  y agregar `~/.dotnet/tools` al `PATH`.
- En la extensión SQL Server de VS Code hay que marcar *Trust server certificate*; si no, la conexión a `localhost` falla.
- Al agregar `Observaciones`, la propiedad apareció sola en `Reserva.cs` después del scaffold, pero tuve que agregarla
  a mano en el `[Bind]` del controlador y en el formulario `Create.cshtml`.

## Conclusiones

1. Con Database First el esquema manda: primero se diseña la base en SQL y EF Core genera las entidades y el contexto
   con un solo comando, lo que ahorra escribir las clases a mano y evita errores de mapeo.
2. Separar el acceso a datos en repositorios deja a los controladores solo con la lógica de la petición y las
   validaciones; además, todas las consultas LINQ quedan en un mismo lugar y son más fáciles de revisar.
3. La consulta de disponibilidad con `!Any` mostró que LINQ se traduce a un solo `NOT EXISTS` en SQL, sin traer
   todas las reservas a memoria. Lo mismo pasa con `AsNoTracking()`, que hace más livianas las consultas de solo lectura.
4. La eliminación lógica conserva el historial: una habitación dada de baja sigue apareciendo en las reservas antiguas.
5. Cuando cambia la base de datos basta con regenerar con `--force`, pero el scaffold solo actualiza `Models/`;
   los controladores, el `[Bind]` y las vistas hay que actualizarlos a mano.
