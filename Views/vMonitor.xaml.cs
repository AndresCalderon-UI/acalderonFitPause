using System.Timers;
using acalderonFitPause.Utils;
using acalderonFitPause.Models;

namespace acalderonFitPause.Views;

public partial class vMonitor : ContentPage
{
    private readonly Usuario _usuario;
    private ConfiguracionUsuario _configuracion;

    private bool _monitorActivo = true;
    private int _tiempoSinMovimientoMin = 0;
    private int _tiempoObjetivoMin = 45;   // por defecto
    private DateTime _ultimaActividad = DateTime.Now;

    private readonly System.Timers.Timer _temporizador;

    public vMonitor(Usuario usuario)
    {
        InitializeComponent();

        _usuario = usuario;

        // Timer: aquí 60 000 ms = 1 minuto. Para pruebas, puedes poner 10 000 (10 seg).
        _temporizador = new System.Timers.Timer(60000);
        _temporizador.Elapsed += Temporizador_Elapsed;
        _temporizador.AutoReset = true;

        // Texto inicial
        lblTiempoSinMovimiento.Text = "0 min";
        lblUltimaActividad.Text = "Hace 0 min";

        // Marcar menú
        MarcarMenuSeleccionado("Monitor");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Cargar configuración desde Supabase
        await CargarConfiguracionUsuarioAsync();

        // Iniciar monitor
        _monitorActivo = true;
        _temporizador.Start();
        ActualizarTextoBotonMonitor();
        ActualizarPantalla();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _temporizador.Stop();
    }

    // ========== CARGA CONFIGURACIÓN ==========

    private async Task CargarConfiguracionUsuarioAsync()
    {
        try
        {
            string filtro = $"?UsuarioId=eq.{_usuario.Id}&limit=1";

            var respuesta = await servicioSupabase.ConsultarAsync(
                configSupabase.tblConfiguracion,
                filtro
            );

            if (respuesta.IsSuccessStatusCode)
            {
                var contenido = await respuesta.Content.ReadAsStringAsync();
                var lista = System.Text.Json.JsonSerializer
                    .Deserialize<List<ConfiguracionUsuario>>(contenido);

                if (lista != null && lista.Count > 0)
                    _configuracion = lista[0];
            }

            if (_configuracion == null)
            {
                // Configuración por defecto
                _configuracion = new ConfiguracionUsuario
                {
                    UsuarioId = _usuario.Id,
                    TiempoAlerta = 45,
                    NotificacionesActivas = true,
                    Tema = "Claro"
                };
            }

            _tiempoObjetivoMin = _configuracion.TiempoAlerta;
            lblMaxTiempo.Text = $"de {_tiempoObjetivoMin} min";
        }
        catch
        {
            // Si hay error, seguimos con valores por defecto
            _tiempoObjetivoMin = 45;
            lblMaxTiempo.Text = $"de {_tiempoObjetivoMin} min";
        }
    }

    // ========== TEMPORIZADOR ==========

    private void Temporizador_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_monitorActivo)
            return;

        // Simulamos que no hay movimiento ? aumenta el tiempo sin moverse
        _tiempoSinMovimientoMin++;

        // Actualizar UI en el hilo principal
        MainThread.BeginInvokeOnMainThread(ActualizarPantalla);
    }

    // Aquí luego podrías enganchar el acelerómetro y cuando detectes movimiento:
    //  - Resetear _tiempoSinMovimientoMin = 0
    //  - _ultimaActividad = DateTime.Now;

    // ========== ACTUALIZAR PANTALLA ==========

    private void ActualizarPantalla()
    {
        // Tiempo
        lblTiempoSinMovimiento.Text = $"{_tiempoSinMovimientoMin} min";

        var diff = DateTime.Now - _ultimaActividad;
        lblUltimaActividad.Text = $"Hace {Math.Max(0, (int)diff.TotalMinutes)} min";

        // Progreso
        double progreso = _tiempoObjetivoMin <= 0
            ? 0
            : (double)_tiempoSinMovimientoMin / _tiempoObjetivoMin;

        if (progreso < 0) progreso = 0;
        if (progreso > 1) progreso = 1;

        pbProgreso.Progress = progreso;

        // Color y mensaje según progreso
        Color colorEstado;
        string mensaje;

        if (!_monitorActivo)
        {
            colorEstado = Color.FromArgb("#6B7280");
            mensaje = "Monitor en pausa";
        }
        else if (progreso < 0.5)
        {
            colorEstado = Color.FromArgb("#10B981"); // verde
            mensaje = "Todo bajo control";
        }
        else if (progreso < 1.0)
        {
            colorEstado = Color.FromArgb("#F59E0B"); // amarillo
            mensaje = "Se acerca la hora de una pausa";
        }
        else
        {
            colorEstado = Color.FromArgb("#EF4444"); // rojo
            mensaje = "¡Es hora de una pausa activa!";
        }

        lblTiempoSinMovimiento.TextColor = colorEstado;
        lblMensajeEstado.TextColor = colorEstado;
        lblMensajeEstado.Text = mensaje;
        pbProgreso.ProgressColor = colorEstado;

        // Estado del sensor
        if (_monitorActivo)
        {
            frmEstadoSensor.BackgroundColor = Color.FromArgb("#D1FAE5");
            lblEstadoSensor.Text = "Activo";
            lblEstadoSensor.TextColor = Color.FromArgb("#10B981");
        }
        else
        {
            frmEstadoSensor.BackgroundColor = Color.FromArgb("#F3F4F6");
            lblEstadoSensor.Text = "En pausa";
            lblEstadoSensor.TextColor = Color.FromArgb("#6B7280");
        }

        // Mostrar u ocultar botón "Iniciar Pausa Activa"
        btnIniciarPausaActiva.IsVisible = progreso >= 0.8;
    }

    private void ActualizarTextoBotonMonitor()
    {
        if (_monitorActivo)
        {
            btnPausarIniciarMonitor.Text = "Pausar Monitor";
            btnPausarIniciarMonitor.BackgroundColor = Color.FromArgb("#F59E0B");
        }
        else
        {
            btnPausarIniciarMonitor.Text = "Iniciar Monitor";
            btnPausarIniciarMonitor.BackgroundColor = Color.FromArgb("#10B981");
        }
    }

    // ========== EVENTOS DE BOTONES ==========

    private void btnPausarIniciarMonitor_Clicked(object sender, EventArgs e)
    {
        _monitorActivo = !_monitorActivo;

        if (_monitorActivo && !_temporizador.Enabled)
            _temporizador.Start();
        else if (!_monitorActivo && _temporizador.Enabled)
            _temporizador.Stop();

        ActualizarTextoBotonMonitor();
        ActualizarPantalla();
    }

    private async void btnIniciarPausaActiva_Clicked(object sender, EventArgs e)
    {
        // Aquí más adelante puedes:
        //  - registrar una pausa en la tabla Historial
        //  - navegar a la vista de ejercicios
        await DisplayAlert("Pausa activa", "Aquí iniciarías la pausa activa.", "OK");

        // Simulamos que el usuario se movió ? reseteamos contador
        _tiempoSinMovimientoMin = 0;
        _ultimaActividad = DateTime.Now;
        ActualizarPantalla();
    }

    private void btnAtras_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    // ===== Menú inferior =====

    private void MarcarMenuSeleccionado(string opcion)
    {
        btnMenuInicio.TextColor = Color.FromArgb("#6B7280");
        btnMenuMonitor.TextColor = Color.FromArgb("#6B7280");
        btnMenuHistorial.TextColor = Color.FromArgb("#6B7280");
        btnMenuAjustes.TextColor = Color.FromArgb("#6B7280");

        switch (opcion)
        {
            case "Inicio": btnMenuInicio.TextColor = Color.FromArgb("#2563FF"); break;
            case "Monitor": btnMenuMonitor.TextColor = Color.FromArgb("#2563FF"); break;
            case "Historial": btnMenuHistorial.TextColor = Color.FromArgb("#2563FF"); break;
            case "Ajustes": btnMenuAjustes.TextColor = Color.FromArgb("#2563FF"); break;
        }
    }

    private async void btnMenuInicio_Clicked(object sender, EventArgs e)
    {
        // Volver a Inicio (vPrincipal) pasando el mismo usuario
        await Navigation.PushAsync(new vPrincipal(_usuario));
    }

    private async void btnMenuMonitor_Clicked(object sender, EventArgs e)
    {
        // Ya estás en Monitor
        MarcarMenuSeleccionado("Monitor");
    }

    private async void btnMenuHistorial_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Historial");
        //await Navigation.PushAsync(new vHistorial());
    }

    private async void btnMenuAjustes_Clicked(object sender, EventArgs e)
    {
        MarcarMenuSeleccionado("Ajustes");
        //await Navigation.PushAsync(new vAjustes());
    }
}
