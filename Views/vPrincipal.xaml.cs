using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using System.Text.Json;
using System.Threading;

namespace acalderonFitPause.Views;

public partial class vPrincipal : ContentPage
{
    private readonly Usuario _usuario;
    private ConfiguracionUsuario _configuracion;
    private HistorialPausa _ultimaPausa;
    private string _nombreUltimoEjercicio;
    private ResumenDiario _resumenDiario;

    public vPrincipal(Usuario usuario)
    {
        InitializeComponent();

        _usuario = usuario;

        lblNombreUsuario.Text = _usuario?.Nombre ?? "Usuario";
    }

    // Cargamos datos reales cada vez que la vista aparece
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

    private async Task CargarConfiguracionUsuarioAsync()
    {
        // Ejemplo: SELECT * FROM configuraciones WHERE UsuarioId = _usuario.Id LIMIT 1
        string filtroConfiguracion = $"?UsuarioId=eq.{_usuario.Id}&limit=1";

        var respuesta = await servicioSupabase.ConsultarAsync(
            configSupabase.tblConfiguracion,
            filtroConfiguracion
        );

        if (!respuesta.IsSuccessStatusCode)
        {
            _configuracion = new ConfiguracionUsuario
            {
                UsuarioId = _usuario.Id,
                TiempoAlerta = 45
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
                TiempoAlerta = 45
            };
        }
        else
        {
            _configuracion = listaConfigs[0];
        }
    }

    private async Task CargarResumenDiarioAsync()
    {
        string filtroHistorial = $"?UsuarioId=eq.{_usuario.Id}&order=FechaHora.desc";

        var respuesta = await servicioSupabase.ConsultarAsync(
            configSupabase.tblHistorial,
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

        // Pausas del día
        var pausasHoy = listaPausas
            .Where(p => p.FechaHora >= hoy && p.FechaHora < mañana)
            .ToList();

        int cantidadPausasHoy = pausasHoy.Count;

        int rachaDias = CalcularRachaDias(listaPausas);

        _ultimaPausa = listaPausas.FirstOrDefault();

        if (_ultimaPausa != null)
        {
            _nombreUltimoEjercicio = await ObtenerNombreEjercicioUltimaPausaAsync(_ultimaPausa);
        }
        else
        {
            _nombreUltimoEjercicio = "Nombre ejercicio";
        }

        _resumenDiario = new ResumenDiario
        {
            PausasHoy = cantidadPausasHoy,
            MetaDiariaTotal = _configuracion.Meta,
            MetaDiariaCompletadas = cantidadPausasHoy,
            RachaDias = rachaDias,
            UltimaPausa = _ultimaPausa
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
                break;
            }
        }

        return racha;
    }

    private void PintarResumenEnPantalla()
    {
        if (_resumenDiario == null)
            _resumenDiario = CrearResumenVacio();

        int tiempoSinMovimiento = _configuracion?.TiempoAlerta ?? 45;
        lblTiempoSinMovimiento.Text = $"{tiempoSinMovimiento} min";

        // Pausas de hoy
        lblPausasHoy.Text = _resumenDiario.PausasHoy.ToString();

        // Racha
        lblRachaDias.Text = _resumenDiario.RachaDias.ToString();

        // Meta diaria
        lblMetaDiariaTexto.Text =
            $"{_resumenDiario.MetaDiariaCompletadas} de {_configuracion.Meta} pausas";

        lblMetaDiariaPorcentaje.Text = $"{_resumenDiario.PorcentajeMeta}%";

        double progreso;

        if (_resumenDiario.MetaDiariaTotal == 0)
        {
            progreso = 0;
        }
        else
        {
            progreso = Math.Min(1.0, _resumenDiario.PorcentajeMeta / 100.0);
        }

        pbMetaDiaria.Progress = progreso;

        // Última pausa activa
        if (_resumenDiario.UltimaPausa != null)
        {
            if (string.IsNullOrWhiteSpace(_resumenDiario.UltimaPausa.FraseMotivacional) ||
                string.IsNullOrWhiteSpace(_nombreUltimoEjercicio))
            {
                lblUltimaPausaNombre.Text = "Última pausa realizada";
            }
            else
            {
                lblUltimaPausaNombre.Text = _nombreUltimoEjercicio;
                lblUltimaPausaFrase.Text = _resumenDiario.UltimaPausa.FraseMotivacional;
            }

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

        MarcarMenuSeleccionado("Inicio");
    }

    private async Task<string> ObtenerNombreEjercicioUltimaPausaAsync(HistorialPausa pausa)
    {
        if (pausa == null)
            return null;

        // Ajusta "EjercicioId" y "tblEjercicio" según tus nombres reales
        string filtro = $"?Id=eq.{pausa.EjercicioId}&select=Nombre&limit=1";

        var respuesta = await servicioSupabase.ConsultarAsync(
            configSupabase.tblEjercicio,
            filtro
        );

        if (!respuesta.IsSuccessStatusCode)
            return null;

        var contenido = await respuesta.Content.ReadAsStringAsync();
        var listaEjercicios = JsonSerializer.Deserialize<List<Ejercicio>>(contenido);

        var ejercicio = listaEjercicios?.FirstOrDefault();
        return ejercicio?.Nombre;
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

    private void MarcarMenuSeleccionado(string opcion)
    {
        btnMenuInicio.TextColor = Color.FromArgb("#6B7280");
        btnMenuMonitor.TextColor = Color.FromArgb("#6B7280");
        btnMenuEjercicio.TextColor = Color.FromArgb("#6B7280");
        btnMenuHistorial.TextColor = Color.FromArgb("#6B7280");
        btnMenuAjustes.TextColor = Color.FromArgb("#6B7280");

        switch (opcion)
        {
            case "Inicio":
                btnMenuInicio.TextColor = Color.FromArgb("#2563FF"); break;
            case "Monitor":
                btnMenuMonitor.TextColor = Color.FromArgb("#2563FF"); break;
            case "Ejercicio":
                btnMenuEjercicio.TextColor = Color.FromArgb("#2563FF"); break;
            case "Historial":
                btnMenuHistorial.TextColor = Color.FromArgb("#2563FF"); break;
            case "Ajustes":
                btnMenuAjustes.TextColor = Color.FromArgb("#2563FF"); break;
        }
    }

    private void btnVerDetalle_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new vMonitor(_usuario));
    }

    private void btnMenuEjercicio_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new vEjercicio(_usuario));
    }

    private void btnHistorial_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new vHistorial(_usuario));
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
        Navigation.PushAsync(new vHistorial(_usuario));
    }

    private void btnMenuAjustes_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Ajustes");
        Navigation.PushAsync(new vConfiguracion(_usuario));
    }
}
