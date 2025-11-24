using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using Microsoft.Maui.Dispatching;

namespace acalderonFitPause.Views
{
    public partial class vRealizaEjercicio : ContentPage
    {
        private readonly Usuario _usuario;
        private readonly Ejercicio _ejercicio;
        private readonly IDispatcherTimer _timer;
        private int _segundos;

        public vRealizaEjercicio(Usuario usuario, Ejercicio ejercicio)
        {
            InitializeComponent();

            _usuario = usuario;
            _ejercicio = ejercicio;

            lblNombreEjercicio.Text = ejercicio.Nombre;

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                _segundos++;
                lblSegundos.Text = _segundos + " s";
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _segundos = 0;
            lblSegundos.Text = "0 s";
            _timer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer.Stop();
        }

        private async void btnFinalizar_Clicked(object sender, EventArgs e)
        {
            _timer.Stop();

            int minutos = (int)Math.Ceiling(_segundos / 60.0);

            var registro = new
            {
                UsuarioId = _usuario.Id,
                EjercicioId = _ejercicio.Id,
                FechaHora = DateTime.Now,
                DuracionReal = minutos,
                FraseMotivacional = "¡Buen trabajo!"
            };

            try
            {
                var resp = await servicioSupabase.InsertarAsync(configSupabase.tblHistorial, registro);

                //if (!resp.IsSuccessStatusCode)
                //{
                //    await DisplayAlert("Error", "No se pudo guardar el historial.", "Aceptar");
                //    return;
                //}
                if (!resp.IsSuccessStatusCode)
                {
                    var contenido = await resp.Content.ReadAsStringAsync();
                    await DisplayAlert("Error al guardar historial",
                        $"Código HTTP: {(int)resp.StatusCode}\n\nDetalle:\n{contenido}",
                        "Aceptar");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
                return;
            }

            // Reanudar el monitor de actividad
            vMonitor.ReanudarMonitorDespuesRutina();

            await Navigation.PopModalAsync();
        }

        private async void btnCancelar_Clicked(object sender, EventArgs e)
        {
            _timer.Stop();
            vMonitor.ReanudarMonitorDespuesRutina();
            await Navigation.PopModalAsync();
        }
    }
}
