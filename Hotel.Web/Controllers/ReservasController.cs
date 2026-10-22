using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers;

public class ReservasController : Controller
{
    private static readonly string[] Estados = ["Pendiente", "Confirmada", "Cancelada", "Finalizada"];

    private readonly ReservaRepositorio _reservas;

    public ReservasController(ReservaRepositorio reservas)
    {
        _reservas = reservas;
    }

    public async Task<IActionResult> Index(string? estado)
    {
        ViewBag.Estados = new SelectList(Estados, estado);
        return View(await _reservas.ListarAsync(estado));
    }

    // Filtro por GET: /Reservas/Disponibles?ingreso=2026-10-25&salida=2026-10-28
    public async Task<IActionResult> Disponibles(DateOnly? ingreso, DateOnly? salida)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var desde = ingreso ?? hoy;
        var hasta = salida ?? desde.AddDays(1);

        ViewBag.Ingreso = desde;
        ViewBag.Salida = hasta;
        ViewBag.Noches = hasta.DayNumber - desde.DayNumber;

        if (hasta <= desde)
            ModelState.AddModelError("salida", "La fecha de salida debe ser posterior a la de ingreso.");
        if (desde < hoy)
            ModelState.AddModelError("ingreso", "La fecha de ingreso no puede ser anterior a hoy.");

        if (!ModelState.IsValid)
            return View(new List<Habitacion>());

        return View(await _reservas.ListarDisponiblesAsync(desde, hasta));
    }

    // Puede llegar con datos desde Disponibles (botón "Reservar")
    public async Task<IActionResult> Create(int? idHabitacion, DateOnly? ingreso, DateOnly? salida)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var reserva = new Reserva
        {
            IdHabitacion = idHabitacion ?? 0,
            FechaIngreso = ingreso ?? hoy,
            FechaSalida = salida ?? (ingreso ?? hoy).AddDays(1)
        };

        await CargarListasAsync(reserva);
        return View(reserva);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdHuesped,IdHabitacion,FechaIngreso,FechaSalida,Observaciones")] Reserva reserva)
    {
        // Estado y navegaciones no vienen del formulario: se quitan de la validación
        ModelState.Remove(nameof(Reserva.Estado));
        ModelState.Remove(nameof(Reserva.IdHuespedNavigation));
        ModelState.Remove(nameof(Reserva.IdHabitacionNavigation));
        await ValidarReglasAsync(reserva);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(reserva);
            return View(reserva);
        }

        await _reservas.CrearAsync(reserva);
        TempData["Mensaje"] = $"Reserva N.° {reserva.IdReserva} registrada como Pendiente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        if (await _reservas.CancelarAsync(id))
            TempData["Mensaje"] = $"Reserva N.° {id} cancelada correctamente.";
        else
            TempData["Error"] = $"La reserva N.° {id} no existe o ya no se puede cancelar.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarReglasAsync(Reserva reserva)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        if (reserva.IdHuesped <= 0)
            ModelState.AddModelError(nameof(Reserva.IdHuesped), "Seleccione un huésped.");
        else if (!await _reservas.ExisteHuespedActivoAsync(reserva.IdHuesped))
            ModelState.AddModelError(nameof(Reserva.IdHuesped), "El huésped seleccionado no existe o está inactivo.");

        var habitacionValida = false;
        if (reserva.IdHabitacion <= 0)
            ModelState.AddModelError(nameof(Reserva.IdHabitacion), "Seleccione una habitación.");
        else if (!await _reservas.ExisteHabitacionActivaAsync(reserva.IdHabitacion))
            ModelState.AddModelError(nameof(Reserva.IdHabitacion), "La habitación seleccionada no existe o está fuera de servicio.");
        else
            habitacionValida = true;

        var fechasValidas = true;
        if (reserva.FechaIngreso < hoy)
        {
            ModelState.AddModelError(nameof(Reserva.FechaIngreso), "La fecha de ingreso no puede ser anterior a hoy.");
            fechasValidas = false;
        }
        if (reserva.FechaSalida <= reserva.FechaIngreso)
        {
            ModelState.AddModelError(nameof(Reserva.FechaSalida), "La fecha de salida debe ser posterior a la de ingreso.");
            fechasValidas = false;
        }

        if (reserva.Observaciones?.Length > 200)
            ModelState.AddModelError(nameof(Reserva.Observaciones), "Las observaciones no pueden superar los 200 caracteres.");

        // Solo tiene sentido buscar cruces si la habitación y el rango son válidos
        if (habitacionValida && fechasValidas
            && !await _reservas.EstaDisponibleAsync(reserva.IdHabitacion, reserva.FechaIngreso, reserva.FechaSalida))
        {
            ModelState.AddModelError(nameof(Reserva.IdHabitacion),
                "La habitación ya está reservada en ese rango de fechas.");
        }
    }

    private async Task CargarListasAsync(Reserva reserva)
    {
        var huespedes = await _reservas.ObtenerHuespedesParaListaAsync();
        ViewBag.Huespedes = huespedes.Select(h => new SelectListItem(
            $"{h.Apellidos}, {h.Nombres} - DNI {h.Dni}",
            h.IdHuesped.ToString(),
            h.IdHuesped == reserva.IdHuesped));

        var habitaciones = await _reservas.ObtenerHabitacionesParaListaAsync();
        ViewBag.Habitaciones = habitaciones.Select(h => new SelectListItem(
            $"{h.Numero} - {h.IdTipoHabitacionNavigation.Nombre} ({h.IdTipoHabitacionNavigation.PrecioNoche:C} por noche)",
            h.IdHabitacion.ToString(),
            h.IdHabitacion == reserva.IdHabitacion));
    }
}
