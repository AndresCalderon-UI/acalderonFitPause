using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Devices.Sensors;
using Plugin.LocalNotification;

namespace acalderonFitPause.Views
{
    public partial class vMonitor : ContentPage
    {

        private static double _limiteDeMovimiento = 0.15;


        private static bool _monitorActivo = true;
        private static int _tiempoSinMovimientoSeg = 0;
        private static int _tiempoObjetivoMin = 45;
        private static DateTime _ultimaActividad = DateTime.Now;
        private static bool _estadoInicializado = false;
        private static bool _enMovimiento = false;
        private static vMonitor _instanciaActiva;

        private const bool modoPruebas = true;

        private static int ObtenerTiempoLimite()
        {
            if (modoPruebas)
            {
                return _tiempoObjetivoMin;
            }
            else
            {
                return _tiempoObjetivoMin * 60;
            }
        }


        #region Campos de instancia

        private readonly IDispatcherTimer _temporizador;
        private readonly Usuario _usuario;
        private ConfiguracionUsuario _configuracion;
        private bool _notificacionesActivas = true;

        #endregion

        #region Constructor

        public vMonitor(Usuario usuario)
        {
            InitializeComponent();
            _usuario = usuario;

            InicializarEstadoCompartido();

            // DispatcherTimer de 1 segundo
            _temporizador = Dispatcher.CreateTimer();
            _temporizador.Interval = TimeSpan.FromSeconds(1);
            _temporizador.Tick += contTemporizador;

            MarcarMenuSeleccionado("Monitor");
        }

        private void InicializarEstadoCompartido()
        {
            // SOLO LA PRIMERA VEZ se inicializa el estado
            if (_estadoInicializado)
                return;

            _monitorActivo = true;
            _tiempoSinMovimientoSeg = 0;
            _tiempoObjetivoMin = 45;
            _ultimaActividad = DateTime.Now;
            _estadoInicializado = true;
        }

        #endregion

        #region Ciclo de vida de la página

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _instanciaActiva = this;

            // Re-suscribimos el acelerómetro para evitar handlers duplicados
            Accelerometer.ReadingChanged -= leeAcelerometro;
            Accelerometer.ReadingChanged += leeAcelerometro;

            try
            {
                if (!Accelerometer.IsMonitoring)
                    Accelerometer.Start(SensorSpeed.UI);
            }
            catch
            {
                // Podrías loguear el error si lo necesitas
            }

            await CargarConfiguracionUsuarioAsync();

            lblMaxTiempo.Text = "de " + _tiempoObjetivoMin + " min";

            if (_monitorActivo)
                _temporizador.Start();

            ActualizarTextoBotonMonitor();
            ActualizarPantalla();
        }

        #endregion

        #region Métodos estáticos llamados desde otras vistas

        public static void PausarMonitorPorRutina()
        {
            if (_instanciaActiva == null)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _monitorActivo = false;
                _instanciaActiva._temporizador.Stop();
                _instanciaActiva.ActualizarTextoBotonMonitor();
                _instanciaActiva.ActualizarPantalla();
            });
        }

        public static void ReanudarMonitorDespuesRutina()
        {
            if (_instanciaActiva == null)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Se reinicia el conteo por inactividad y se retoma el monitor
                _tiempoSinMovimientoSeg = 0;
                _ultimaActividad = DateTime.Now;
                _monitorActivo = true;

                _instanciaActiva._temporizador.Start();
                _instanciaActiva.ActualizarTextoBotonMonitor();
                _instanciaActiva.ActualizarPantalla();
            });
        }

        #endregion

        #region Carga de configuración desde la BD

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
                        _notificacionesActivas = _configuracion.NotificacionFlag;
                        
                        if (_configuracion.LimiteMovimiento > 0)
                            _limiteDeMovimiento = _configuracion.LimiteMovimiento;
                        else
                            _limiteDeMovimiento = 0.15;

                        return;
                    }
                }

                // Sin configuración en BD, valores por defecto
                _tiempoObjetivoMin = 45;
                _notificacionesActivas = true;
                _limiteDeMovimiento = 0.15;
            }
            catch
            {
                // En caso de error, un valor seguro para pruebas
                _tiempoObjetivoMin = 45;
                _notificacionesActivas = true;
                _limiteDeMovimiento = 0.15;
            }
        }

        #endregion

        #region Manejo del acelerómetro

        private void leeAcelerometro(object sender, AccelerometerChangedEventArgs e)
        {
            var eje = e.Reading.Acceleration;

            // Se mide el total de la aceleración
            double fuerzaAceleracion = Math.Sqrt(
                eje.X * eje.X +
                eje.Y * eje.Y +
                eje.Z * eje.Z);

            // Reposo ~ 1g. Si se aleja de 1 => movimiento
            bool actividad = Math.Abs(fuerzaAceleracion - 1.0) > _limiteDeMovimiento;

            // Sólo si cambia el estado, actualizamos UI
            if (actividad == _enMovimiento)
                return;

            _enMovimiento = actividad;

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

        #endregion

        #region Temporizador (cada 1 segundo)

        private void contTemporizador(object sender, EventArgs e)
        {
            if (!_monitorActivo)
                return;

            // Si se detecta movimiento, no contamos inactividad
            if (_enMovimiento)
            {
                ActualizarPantalla();
                return;
            }

            _tiempoSinMovimientoSeg++;

            int tiempoLimite = ObtenerTiempoLimite();

            if (_tiempoSinMovimientoSeg >= tiempoLimite)
            {
                // Fijamos el contador en el límite y detenemos el monitor
                _tiempoSinMovimientoSeg = tiempoLimite;
                _temporizador.Stop();

                EnviarNotificacionPausaActiva();
                ActualizarTextoBotonMonitor();
            }

            ActualizarPantalla();
        }

        private void EnviarNotificacionPausaActiva()
        {
            if (!_notificacionesActivas)
                return;

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

        #endregion

        #region Actualización de elementos en pantalla

        private void ActualizarPantalla()
        {
            // En tu caso estás mostrando "min" aunque cuentes segundos,
            // si luego quieres, aquí puedes cambiar a minutos reales.
            int minutos = _tiempoSinMovimientoSeg;
            lblTiempoSinMovimiento.Text = minutos + " min";

            var diff = DateTime.Now - _ultimaActividad;
            lblUltimaActividad.Text = "Hace " + (int)diff.TotalMinutes + " min";

            //int tiempoLimite = ObtenerTiempoLimite();
            //double progreso = tiempoLimite > 0
            //    ? (double)_tiempoSinMovimientoSeg / tiempoLimite
            //    : 0;

            //progreso = Math.Clamp(progreso, 0, 1);
            int tiempoLimite = ObtenerTiempoLimite();

            double progreso;

            if (tiempoLimite > 0)
            {
                progreso = Convert.ToDouble(_tiempoSinMovimientoSeg) / tiempoLimite;

            }
            else
            {
                progreso = 0;
            }

            // 2. Limitar el valor entre 0 y 1
            if (progreso < 0)
            {
                progreso = 0;
            }
            else if (progreso > 1)
            {
                progreso = 1;
            }

            // 3. Asignar a la barra de progreso
            pbProgreso.Progress = progreso;

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
                mensaje = "Todo bien!";
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

            if (_monitorActivo)//hacer la prueba con el estado del monitor
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

            // Mostrar botón de pausa activa sólo cuando llegue al 100%
            btnIniciarPausaActiva.IsVisible = progreso >= 1.0;
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

        #endregion

        #region Handlers de botones (monitor y navegación)

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

        private async void btnIniciarPausaActiva_Clicked(object sender, EventArgs e)
        {
           await Navigation.PushAsync(new vEjercicio(_usuario));

            //_tiempoSinMovimientoSeg = 0;
            //_ultimaActividad = DateTime.Now;

            //_monitorActivo = true;
            //_temporizador.Start();

            //ActualizarTextoBotonMonitor();
            //ActualizarPantalla();
        }

        private void btnAtras_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        #endregion

        #region Menú inferior

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
                    btnMenuInicio.TextColor = Color.FromArgb("#2563FF");
                    break;
                case "Monitor":
                    btnMenuMonitor.TextColor = Color.FromArgb("#2563FF");
                    break;
                case "Ejercicio":
                    btnMenuEjercicio.TextColor = Color.FromArgb("#2563FF");
                    break;
                case "Historial":
                    btnMenuHistorial.TextColor = Color.FromArgb("#2563FF");
                    break;
                case "Ajustes":
                    btnMenuAjustes.TextColor = Color.FromArgb("#2563FF");
                    break;
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

        private async void btnMenuEjercicio_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new vEjercicio(_usuario));
        }
        private async void btnMenuHistorial_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Historial");
            await Navigation.PushAsync(new vHistorial(_usuario));
        }

        private async void btnMenuAjustes_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Ajustes");
            await Navigation.PushAsync(new vConfiguracion(_usuario));
        }

        #endregion
    }
}
