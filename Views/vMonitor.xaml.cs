using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using Microsoft.Maui.Dispatching;
using Plugin.LocalNotification;
using Microsoft.Maui.Devices.Sensors;

namespace acalderonFitPause.Views
{
    public partial class vMonitor : ContentPage
    {
        // ==========================================
        // ESTADO COMPARTIDO ENTRE TODAS LAS vMonitor
        // ==========================================
        private static bool _monitorActivo = true;
        private static int _tiempoSinMovimientoSeg = 0;  // contador REAL en segundos
        private static int _tiempoObjetivoMin = 1;       // meta (en minutos)
        private static DateTime _ultimaActividad = DateTime.Now;
        private static bool _estadoInicializado = false;

        // DispatcherTimer que dispara cada segundo
        private readonly IDispatcherTimer _temporizador;
        private static bool _enMovimiento = false;

        private readonly Usuario _usuario;
        private ConfiguracionUsuario _configuracion;

        public vMonitor(Usuario usuario)
        {
            InitializeComponent();
            _usuario = usuario;

            // SOLO LA PRIMERA VEZ se inicializa el estado
            if (!_estadoInicializado)
            {
                _monitorActivo = true;
                _tiempoSinMovimientoSeg = 0;
                _tiempoObjetivoMin = 1;
                _ultimaActividad = DateTime.Now;
                _estadoInicializado = true;
            }

            // DispatcherTimer de 1 segundo
            _temporizador = Dispatcher.CreateTimer();
            _temporizador.Interval = TimeSpan.FromSeconds(1);
            _temporizador.Tick += contTemporizador;

            MarcarMenuSeleccionado("Monitor");
        }

        // ==========================================
        // Cargar configuracion cuando entras a la vista
        // ==========================================
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Accelerometer.ReadingChanged -= leeAcelerometro;
            Accelerometer.ReadingChanged += leeAcelerometro;

            try
            {
                if (!Accelerometer.IsMonitoring)
                    Accelerometer.Start(SensorSpeed.UI);
            }
            catch
            {
            }

            await CargarConfiguracionUsuarioAsync();
            lblMaxTiempo.Text = "de " + _tiempoObjetivoMin + " min";

            if (_monitorActivo)
                _temporizador.Start();

            ActualizarTextoBotonMonitor();
            ActualizarPantalla();
        }

        // ==========================================
        // CONSULTAR CONFIGURACION EN BD
        // ==========================================
        private async Task CargarConfiguracionUsuarioAsync()
        {
            try
            {
                string consultaConfigUsr = "?UsuarioId=eq." + _usuario.Id;
                var codResp = await servicioSupabase.ConsultarAsync(configSupabase.tblConfiguracion, consultaConfigUsr);

                if (codResp.IsSuccessStatusCode)
                {
                    var json = await codResp.Content.ReadAsStringAsync();
                    var lista = System.Text.Json.JsonSerializer.Deserialize<List<ConfiguracionUsuario>>(json);

                    if (lista != null && lista.Count > 0)
                    {
                        _configuracion = lista[0];
                        _tiempoObjetivoMin = _configuracion.TiempoAlerta;
                    }
                }
            }
            catch
            {
                _tiempoObjetivoMin = 1;
            }
        }

        // ==========================================
        // Metodo para el manejo del acelerometro
        // ==========================================
        private const double UMBRAL_MOVIMIENTO = 0.15;

        private void leeAcelerometro(object sender, AccelerometerChangedEventArgs e)
        {
            var acc = e.Reading.Acceleration;

            // Magnitud total de la aceleracion (en g)
            double magnitud = Math.Sqrt(
                acc.X * acc.X +
                acc.Y * acc.Y +
                acc.Z * acc.Z);

            // Reposo ~ 1g. Si se aleja de 1 => movimiento
            bool hayMovimiento = Math.Abs(magnitud - 1.0) > UMBRAL_MOVIMIENTO;

            if (hayMovimiento != _enMovimiento)
            {
                _enMovimiento = hayMovimiento;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_enMovimiento)
                    {
                        lblEstadoSensor.Text = "Telefono en movimiento";
                        frmEstadoSensor.BackgroundColor = Color.FromArgb("#FEF3C7");
                        lblEstadoSensor.TextColor = Color.FromArgb("#F59E0B");
                    }
                    else
                    {
                        lblEstadoSensor.Text = "Telefono quieto";
                        frmEstadoSensor.BackgroundColor = Color.FromArgb("#D1FAE5");
                        lblEstadoSensor.TextColor = Color.FromArgb("#10B981");
                    }
                });
            }
        }

        // ==========================================
        // TEMPORIZADOR (1 segundo)
        // ==========================================
        private void contTemporizador(object sender, EventArgs e)
        {
            if (!_monitorActivo)
                return;

            if (_enMovimiento)
            {
                ActualizarPantalla();
                return;
            }

            _tiempoSinMovimientoSeg++;

            int tiempoLimite = _tiempoObjetivoMin * 60;
            if (_tiempoSinMovimientoSeg >= tiempoLimite)
            {
                _tiempoSinMovimientoSeg = tiempoLimite;
                _temporizador.Stop();

                var notification = new NotificationRequest
                {
                    NotificationId = 1,
                    Title = "FitPause",
                    Description = "Es hora de una pausa activa!",
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTime.Now
                    }
                };

                notification.Show();
            }

            ActualizarPantalla();
        }

        // ==========================================
        // ACTUALIZAR ELEMENTOS EN LA PANTALLA
        // ==========================================
        private void ActualizarPantalla()
        {
            int minutos = _tiempoSinMovimientoSeg;
            lblTiempoSinMovimiento.Text = minutos + " min";

            var diff = DateTime.Now - _ultimaActividad;
            lblUltimaActividad.Text = "Hace " + (int)diff.TotalMinutes + " min";

            double progreso = (double)_tiempoSinMovimientoSeg / (_tiempoObjetivoMin * 60);
            progreso = Math.Clamp(progreso, 0, 1);
            pbProgreso.Progress = progreso;

            Color color;
            string mensaje;

            if (!_monitorActivo)
            {
                color = Color.FromArgb("#6B7280");
                mensaje = "Monitor en pausa";
            }
            else if (progreso < 0.5)
            {
                color = Color.FromArgb("#10B981");
                mensaje = "Todo bajo control";
            }
            else if (progreso < 1.0)
            {
                color = Color.FromArgb("#F59E0B");
                mensaje = "Se acerca la hora de una pausa";
            }
            else
            {
                color = Color.FromArgb("#EF4444");
                mensaje = "Es hora de una pausa activa!";
            }

            lblTiempoSinMovimiento.TextColor = color;
            lblMensajeEstado.Text = mensaje;
            lblMensajeEstado.TextColor = color;
            pbProgreso.ProgressColor = color;

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

            btnIniciarPausaActiva.IsVisible = progreso >= 1.0;
        }

        // ==========================================
        // BOTON PAUSAR / INICIAR MONITOR
        // ==========================================
        private void btnPausarIniciarMonitor_Clicked(object sender, EventArgs e)
        {
            _monitorActivo = !_monitorActivo;

            if (_monitorActivo)
                _temporizador.Start();
            else
                _temporizador.Stop();

            ActualizarTextoBotonMonitor();
            ActualizarPantalla();
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

        // ==========================================
        // INICIAR PAUSA ACTIVA
        // ==========================================
        private async void btnIniciarPausaActiva_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Pausa activa", "Aqui empieza la pausa activa.", "OK");

            _tiempoSinMovimientoSeg = 0;
            _ultimaActividad = DateTime.Now;

            _monitorActivo = true;
            _temporizador.Start();

            ActualizarTextoBotonMonitor();
            ActualizarPantalla();
        }

        private void btnAtras_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        // ==========================================
        // MENU INFERIOR
        // ==========================================
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
            await Navigation.PushAsync(new vPrincipal(_usuario));
        }

        private void btnMenuMonitor_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Monitor");
        }

        private async void btnMenuHistorial_Clicked(object sender, EventArgs e)
        {
        }

        private async void btnMenuAjustes_Clicked(object sender, EventArgs e)
        {
        }
    }
}
