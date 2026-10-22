using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class HabitacionRepositorio
{
    private readonly HotelContext _context;

    public HabitacionRepositorio(HotelContext context)
    {
        _context = context;
    }

    // Habitaciones activas con su tipo, filtro opcional por tipo, orden por número
    public async Task<List<Habitacion>> ListarAsync(int? idTipoHabitacion)
    {
        var consulta = _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo);

        if (idTipoHabitacion.HasValue)
            consulta = consulta.Where(h => h.IdTipoHabitacion == idTipoHabitacion.Value);

        return await consulta
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    public async Task<Habitacion?> ObtenerAsync(int id)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .FirstOrDefaultAsync(h => h.IdHabitacion == id && h.Activo);
    }

    // Datos de la lista desplegable de tipos (solo activos)
    public async Task<List<TipoHabitacion>> ObtenerTiposParaListaAsync()
    {
        return await _context.TipoHabitacions
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.PrecioNoche)
            .ToListAsync();
    }

    public async Task<bool> ExisteTipoActivoAsync(int idTipoHabitacion)
    {
        return await _context.TipoHabitacions
            .AnyAsync(t => t.IdTipoHabitacion == idTipoHabitacion && t.Activo);
    }

    // Incluye habitaciones inactivas: el UNIQUE de la tabla también las alcanza
    public async Task<bool> ExisteNumeroAsync(string numero, int? excluirIdHabitacion = null)
    {
        return await _context.Habitacions
            .AnyAsync(h => h.Numero == numero && h.IdHabitacion != (excluirIdHabitacion ?? 0));
    }

    // Reservas que todavía ocupan la habitación (pendientes o confirmadas, de hoy en adelante)
    public async Task<int> ContarReservasVigentesAsync(int idHabitacion)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Reservas
            .CountAsync(r => r.IdHabitacion == idHabitacion
                && (r.Estado == "Pendiente" || r.Estado == "Confirmada")
                && r.FechaSalida > hoy);
    }

    public async Task CrearAsync(Habitacion habitacion)
    {
        habitacion.Activo = true;
        _context.Habitacions.Add(habitacion);
        await _context.SaveChangesAsync();
    }

    // Se busca la entidad (con seguimiento) y se copian solo los campos editables
    public async Task<bool> ActualizarAsync(Habitacion habitacion)
    {
        var actual = await _context.Habitacions.FindAsync(habitacion.IdHabitacion);
        if (actual == null || !actual.Activo)
            return false;

        actual.Numero = habitacion.Numero;
        actual.Piso = habitacion.Piso;
        actual.IdTipoHabitacion = habitacion.IdTipoHabitacion;
        await _context.SaveChangesAsync();
        return true;
    }

    // Eliminación lógica: Activo = 0, nunca un DELETE
    public async Task<bool> DesactivarAsync(int id)
    {
        var habitacion = await _context.Habitacions.FindAsync(id);
        if (habitacion == null || !habitacion.Activo)
            return false;

        habitacion.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
