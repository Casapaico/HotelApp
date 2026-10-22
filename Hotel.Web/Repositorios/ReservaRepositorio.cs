using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class ReservaRepositorio
{
    private readonly HotelContext _context;

    public ReservaRepositorio(HotelContext context)
    {
        _context = context;
    }

    // Habitaciones activas libres en el rango pedido, en una sola consulta (!Any).
    // Una reserva se cruza si no está cancelada, empieza antes de la salida pedida
    // y termina después del ingreso pedido.
    public async Task<List<Habitacion>> ListarDisponiblesAsync(DateOnly ingreso, DateOnly salida)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo && !h.Reservas.Any(r =>
                r.Estado != "Cancelada"
                && r.FechaIngreso < salida
                && r.FechaSalida > ingreso))
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    // Misma regla de cruce que ListarDisponiblesAsync, para una sola habitación
    public async Task<bool> EstaDisponibleAsync(int idHabitacion, DateOnly ingreso, DateOnly salida)
    {
        return !await _context.Reservas
            .AnyAsync(r => r.IdHabitacion == idHabitacion
                && r.Estado != "Cancelada"
                && r.FechaIngreso < salida
                && r.FechaSalida > ingreso);
    }

    // Reservas con huésped y habitación (y su tipo, para el total), filtro opcional por estado
    public async Task<List<Reserva>> ListarAsync(string? estado)
    {
        var consulta = _context.Reservas
            .AsNoTracking()
            .Include(r => r.IdHuespedNavigation)
            .Include(r => r.IdHabitacionNavigation)
                .ThenInclude(h => h.IdTipoHabitacionNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(r => r.Estado == estado);

        return await consulta
            .OrderByDescending(r => r.FechaIngreso)
            .ThenBy(r => r.IdHabitacionNavigation.Numero)
            .ToListAsync();
    }

    // Datos de las listas desplegables (solo activos)
    public async Task<List<Huesped>> ObtenerHuespedesParaListaAsync()
    {
        return await _context.Huespeds
            .AsNoTracking()
            .Where(h => h.Activo)
            .OrderBy(h => h.Apellidos)
            .ThenBy(h => h.Nombres)
            .ToListAsync();
    }

    public async Task<List<Habitacion>> ObtenerHabitacionesParaListaAsync()
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo)
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    public async Task<bool> ExisteHuespedActivoAsync(int idHuesped)
    {
        return await _context.Huespeds.AnyAsync(h => h.IdHuesped == idHuesped && h.Activo);
    }

    public async Task<bool> ExisteHabitacionActivaAsync(int idHabitacion)
    {
        return await _context.Habitacions.AnyAsync(h => h.IdHabitacion == idHabitacion && h.Activo);
    }

    // Toda reserva nueva nace como Pendiente
    public async Task CrearAsync(Reserva reserva)
    {
        reserva.Estado = "Pendiente";
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
    }

    // Solo se cancelan reservas Pendiente o Confirmada; nunca se borran
    public async Task<bool> CancelarAsync(int id)
    {
        var reserva = await _context.Reservas.FindAsync(id);
        if (reserva == null || (reserva.Estado != "Pendiente" && reserva.Estado != "Confirmada"))
            return false;

        reserva.Estado = "Cancelada";
        await _context.SaveChangesAsync();
        return true;
    }
}
