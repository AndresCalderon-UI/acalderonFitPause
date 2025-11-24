using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using System.Text.Json;

namespace acalderonFitPause.Views
{
    public partial class vConfiguracion : ContentPage
    {
        private readonly Usuario _usuario;
        private int _usuarioId => _usuario.Id;

        public vConfiguracion(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));

            _ = CargarConfiguracionAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            MarcarMenuSeleccionado("Ajustes");
        }

        // ====================== CARGA INICIAL ======================

        private async Task CargarConfiguracionAsync()
        {
            try
            {
                string filtro = $"?UsuarioId=eq.{_usuarioId}&limit=1";

                var respuesta = await servicioSupabase.ConsultarAsync(
                    configSupabase.tblConfiguracion,
                    filtro);

                if (!respuesta.IsSuccessStatusCode)
                {
                    EstablecerValoresPorDefecto();
                    return;
                }

                string json = await respuesta.Content.ReadAsStringAsync();
                var lista = JsonSerializer.Deserialize<List<ConfiguracionUsuario>>(json);

                if (lista != null && lista.Count > 0)
                {
                    var config = lista[0];

                    // Notificaciones
                    swNotificaciones.IsToggled = config.NotificacionFlag;

                    // Intervalo
                    int minutos = config.TiempoAlerta;
                    int indice = 1;

                    switch (minutos)
                    {
                        case 30: indice = 0; break;
                        case 45: indice = 1; break;
                        case 60: indice = 2; break;
                        case 90: indice = 3; break;
                    }

                    pkIntervalo.SelectedIndex = indice;
                    ActualizarTextoIntervalo(minutos);

                    // Meta diaria
                    sldMeta.Value = config.Meta;
                    lblMetaValor.Text = config.Meta.ToString();
                }
                else
                {
                    EstablecerValoresPorDefecto();
                }
            }
            catch
            {
                EstablecerValoresPorDefecto();
                await DisplayAlert("Error",
                    "Se produjo un problema al consultar la configuracion.",
                    "Aceptar");
            }
        }

        private void EstablecerValoresPorDefecto()
        {
            swNotificaciones.IsToggled = true;

            pkIntervalo.SelectedIndex = 1; // 45 min
            ActualizarTextoIntervalo(45);

            sldMeta.Value = 6;
            lblMetaValor.Text = "6";
        }

        // ====================== LOGICA DE CONTROLES ======================

        private int ObtenerMinutosSeleccionados()
        {
            switch (pkIntervalo.SelectedIndex)
            {
                case 0: return 30;
                case 1: return 45;
                case 2: return 60;
                case 3: return 90;
                default: return 45;
            }
        }

        private void ActualizarTextoIntervalo(int minutos)
        {
            lblTextoIntervalo.Text =
                $"Recibiras un recordatorio cada {minutos} minutos de inactividad";
        }

        private void pkIntervalo_SelectedIndexChanged(object sender, EventArgs e)
        {
            int minutos = ObtenerMinutosSeleccionados();
            ActualizarTextoIntervalo(minutos);
        }

        private void sldMeta_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int meta = (int)Math.Round(e.NewValue);
            lblMetaValor.Text = meta.ToString();
        }

        // ====================== GUARDAR (INSERT/UPDATE) ======================

        private async void btnGuardar_Clicked(object sender, EventArgs e)
        {
            try
            {
                int minutos = ObtenerMinutosSeleccionados();
                int meta = (int)Math.Round(sldMeta.Value);

                // Payload limpio sin Id
                var payload = new
                {
                    UsuarioId = _usuarioId,
                    TiempoAlerta = minutos,
                    NotificacionFlag = swNotificaciones.IsToggled,
                    Meta = meta
                };

                string filtroConsulta = $"?UsuarioId=eq.{_usuarioId}&limit=1";
                var respuestaConsulta = await servicioSupabase.ConsultarAsync(
                    configSupabase.tblConfiguracion,
                    filtroConsulta);

                HttpResponseMessage respuestaGuardar;

                if (respuestaConsulta.IsSuccessStatusCode)
                {
                    string json = await respuestaConsulta.Content.ReadAsStringAsync();
                    var lista = JsonSerializer.Deserialize<List<ConfiguracionUsuario>>(json);

                    if (lista != null && lista.Count > 0)
                    {
                        // Ya existe: UPDATE
                        int idExistente = lista[0].Id;
                        string filtroActualizar = $"?Id=eq.{idExistente}";

                        respuestaGuardar = await servicioSupabase.ActualizarAsync(
                            configSupabase.tblConfiguracion,
                            filtroActualizar,
                            payload);
                    }
                    else
                    {
                        // No existe: INSERT
                        respuestaGuardar = await servicioSupabase.InsertarAsync(
                            configSupabase.tblConfiguracion,
                            payload);
                    }
                }
                else
                {
                    // Consulta fallo: intentar insert
                    respuestaGuardar = await servicioSupabase.InsertarAsync(
                        configSupabase.tblConfiguracion,
                        payload);
                }

                if (respuestaGuardar.IsSuccessStatusCode)
                {
                    await DisplayAlert("Mensaje",
                        "Configuracion guardada correctamente",
                        "Aceptar");
                }
                else
                {
                    string detalle = await respuestaGuardar.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    "Se produjo un problema al guardar la configuracion " + ex.Message,
                    "Aceptar");
            }
        }

        private async void btnCerrarSesion_Clicked(object sender, EventArgs e)
        {
            bool confirma = await DisplayAlert(
                "Mensaje",
                "Cerrar la sesion actual?",
                "Si",
                "No");

            if (!confirma)
                return;

            Application.Current.MainPage = new NavigationPage(new vInicio());
        }


        private async void btnRegresar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // ====================== MENU INFERIOR ======================

        private void MarcarMenuSeleccionado(string opcion)
        {
            btnMenuInicio.TextColor = Color.FromArgb("#6B7280");
            btnMenuMonitor.TextColor = Color.FromArgb("#6B7280");
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
            MarcarMenuSeleccionado("Inicio");
            await Navigation.PushAsync(new vPrincipal(_usuario));
        }

        private async void btnMenuMonitor_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Monitor");
            await Navigation.PushAsync(new vMonitor(_usuario));
        }

        private async void btnMenuHistorial_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Historial");
            await Navigation.PushAsync(new vHistorial(_usuario));
        }

        private void btnMenuAjustes_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Ajustes");
        }
    }
}
