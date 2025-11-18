using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using System.Text.Json;
using System.Threading;

namespace acalderonFitPause.Views;

public partial class vPrincipal : ContentPage
{
    private readonly Usuario _usuario;                 // NO se toca este modelo
    private ConfiguracionUsuario _configuracion;
    private ResumenDiario _resumenDiario;

    public vPrincipal(Usuario usuario)
    {
        InitializeComponent();

        _usuario = usuario;

        // Mostrar nombre apenas carga
        lblNombreUsuario.Text = _usuario?.Nombre ?? "Usuario";
    }

    // Aquí cargamos datos reales cada vez que la vista aparece
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarDatosDesdeServidorAsync();
    }

    private async Task CargarDatosDesdeServidorAsync()
    {
        try
        {
            await CargarConfiguracionUsuarioAsync();
            await CargarResumenDiarioAsync();
            PintarResumenEnPantalla();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los datos de inicio: " + ex.Message, "OK");
        }
    }

    // ========= CONFIGURACIÓN DEL USUARIO =========

    private async Task CargarConfiguracionUsuarioAsync()
    {
        // Ejemplo: SELECT * FROM configuraciones WHERE UsuarioId = _usuario.Id LIMIT 1
        string filtroConfiguracion = $"?UsuarioId=eq.{_usuario.Id}&limit=1";

        var respuesta = await servicioSupabase.ConsultarAsync(
            configSupabase.tblConfiguracion,   // define esto en tu config
            filtroConfiguracion
        );

        if (!respuesta.IsSuccessStatusCode)
        {
            // Si no hay config, ponemos una por defecto
            _configuracion = new ConfiguracionUsuario
            {
                UsuarioId = _usuario.Id,
                TiempoAlerta = 45,
                NotificacionesActivas = true,
                Tema = "Claro"
            };
            return;
        }

        var contenido = await respuesta.Content.ReadAsStringAsync();
        var listaConfigs = JsonSerializer.Deserialize<List<ConfiguracionUsuario>>(contenido);

        if (listaConfigs == null || listaConfigs.Count == 0)
        {
            _configuracion = new ConfiguracionUsuario
            {
                UsuarioId = _usuario.Id,
                TiempoAlerta = 45,
                NotificacionesActivas = true,
                Tema = "Claro"
            };
        }
        else
        {
            _configuracion = listaConfigs[0];
        }
    }

    // ========= RESUMEN DIARIO / HISTORIAL =========

    private async Task CargarResumenDiarioAsync()
    {
        // 1. Obtener historial de pausas del usuario, ordenado desc por fecha
        string filtroHistorial = $"?UsuarioId=eq.{_usuario.Id}&order=FechaHora.desc";

        var respuesta = await servicioSupabase.ConsultarAsync(
            configSupabase.tblHistorial,   // define esto en tu config
            filtroHistorial
        );

        if (!respuesta.IsSuccessStatusCode)
        {
            _resumenDiario = CrearResumenVacio();
            return;
        }

        var contenido = await respuesta.Content.ReadAsStringAsync();
        var listaPausas = JsonSerializer.Deserialize<List<HistorialPausa>>(contenido);

        if (listaPausas == null || listaPausas.Count == 0)
        {
            _resumenDiario = CrearResumenVacio();
            return;
        }

        DateTime hoy = DateTime.Today;
        DateTime mañana = hoy.AddDays(1);

        // Pausas SOLO del día de hoy
        var pausasHoy = listaPausas
            .Where(p => p.FechaHora >= hoy && p.FechaHora < mañana)
            .ToList();

        int cantidadPausasHoy = pausasHoy.Count;

        // Racha de días seguidos con al menos 1 pausa
        int rachaDias = CalcularRachaDias(listaPausas);

        // Última pausa (ya viene ordenado desc)
        HistorialPausa ultimaPausa = listaPausas.FirstOrDefault();

        int metaDiaria = 6; // por ahora fija; luego si quieres la guardas en alguna tabla
        _resumenDiario = new ResumenDiario
        {
            PausasHoy = cantidadPausasHoy,
            MetaDiariaTotal = metaDiaria,
            MetaDiariaCompletadas = cantidadPausasHoy,
            RachaDias = rachaDias,
            UltimaPausa = ultimaPausa
        };
    }

    private ResumenDiario CrearResumenVacio()
    {
        int metaDiaria = 6; // meta por defecto si no hay nada definido
        return new ResumenDiario
        {
            PausasHoy = 0,
            MetaDiariaTotal = metaDiaria,
            MetaDiariaCompletadas = 0,
            RachaDias = 0,
            UltimaPausa = null
        };
    }

    private int CalcularRachaDias(List<HistorialPausa> listaPausas)
    {
        // Días con al menos una pausa
        var diasConPausa = listaPausas
            .Select(p => p.FechaHora.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int racha = 0;
        DateTime fechaEsperada = DateTime.Today;

        foreach (var dia in diasConPausa)
        {
            if (dia == fechaEsperada)
            {
                racha++;
                fechaEsperada = fechaEsperada.AddDays(-1);
            }
            else if (dia < fechaEsperada)
            {
                break; // se rompe la racha
            }
        }

        return racha;
    }

    // ========= PINTAR EN LA VISTA =========

    private void PintarResumenEnPantalla()
    {
        if (_resumenDiario == null)
            _resumenDiario = CrearResumenVacio();

        // Tiempo sin movimiento:
        // por ahora usamos el tiempo de alerta como referencia (luego lo cambiaremos por el sensor real)
        int tiempoSinMovimiento = _configuracion?.TiempoAlerta ?? 45;
        lblTiempoSinMovimiento.Text = $"{tiempoSinMovimiento} min";

        // Pausas de hoy
        lblPausasHoy.Text = _resumenDiario.PausasHoy.ToString();

        // Racha
        lblRachaDias.Text = _resumenDiario.RachaDias.ToString();

        // Meta diaria
        lblMetaDiariaTexto.Text =
            $"{_resumenDiario.MetaDiariaCompletadas} de {_resumenDiario.MetaDiariaTotal} pausas";

        lblMetaDiariaPorcentaje.Text = $"{_resumenDiario.PorcentajeMeta}%";

        double progreso = _resumenDiario.MetaDiariaTotal == 0
            ? 0
            : Math.Min(1.0, _resumenDiario.PorcentajeMeta / 100.0);

        pbMetaDiaria.Progress = progreso;

        // Última pausa activa
        if (_resumenDiario.UltimaPausa != null)
        {
            lblUltimaPausaNombre.Text =
                string.IsNullOrWhiteSpace(_resumenDiario.UltimaPausa.FraseMotivacional)
                    ? "Última pausa realizada"
                    : _resumenDiario.UltimaPausa.FraseMotivacional;

            lblUltimaPausaTiempo.Text = FormatearHoraRelativa(
                _resumenDiario.UltimaPausa.FechaHora,
                _resumenDiario.UltimaPausa.DuracionReal
            );
        }
        else
        {
            lblUltimaPausaNombre.Text = "Sin pausas registradas";
            lblUltimaPausaTiempo.Text = "Empieza tu primera pausa hoy";
        }

        // Menú inferior: estás en Inicio
        MarcarMenuSeleccionado("Inicio");
    }

    private string FormatearHoraRelativa(DateTime fechaHora, int duracionMinutos)
    {
        var diff = DateTime.Now - fechaHora;

        if (diff.TotalMinutes < 1)
            return "Hace unos segundos";

        if (diff.TotalMinutes < 60)
            return $"Hace {(int)diff.TotalMinutes} min";

        int horas = (int)diff.TotalHours;
        return $"Hace {horas} hora{(horas > 1 ? "s" : "")} · {duracionMinutos} min";
    }

    // ========= Menú y botones (igual que antes) =========

    private void MarcarMenuSeleccionado(string opcion)
    {
        btnMenuInicio.TextColor = Color.FromArgb("#6B7280");
        btnMenuMonitor.TextColor = Color.FromArgb("#6B7280");
        btnMenuHistorial.TextColor = Color.FromArgb("#6B7280");
        btnMenuAjustes.TextColor = Color.FromArgb("#6B7280");

        switch (opcion)
        {
            case "Inicio":
                btnMenuInicio.TextColor = Color.FromArgb("#2563FF"); break;
            case "Monitor":
                btnMenuMonitor.TextColor = Color.FromArgb("#2563FF"); break;
            case "Historial":
                btnMenuHistorial.TextColor = Color.FromArgb("#2563FF"); break;
            case "Ajustes":
                btnMenuAjustes.TextColor = Color.FromArgb("#2563FF"); break;
        }
    }

    private void btnPerfil_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new vPerfil());
    }

    private void btnVerDetalle_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new vMonitor(_usuario));
    }

    private void btnEjercicios_Clicked(object sender, EventArgs e)
    {
        //Navigation.PushAsync(new vEjercicios());
    }

    private void btnHistorial_Clicked(object sender, EventArgs e)
    {
        //Navigation.PushAsync(new vHistorial());
    }

    private void btnMenuInicio_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Inicio");
    }

    private void btnMenuMonitor_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Monitor");
        Navigation.PushAsync(new vMonitor(_usuario));
    }

    private void btnMenuHistorial_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Historial");
        //Navigation.PushAsync(new vHistorial());
    }

    private void btnMenuAjustes_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Ajustes");
        //Navigation.PushAsync(new vAjustes());
    }
}
