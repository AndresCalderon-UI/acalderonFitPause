using System.Collections.ObjectModel;
using System.Text.Json;
using acalderonFitPause.Models;
using acalderonFitPause.Utils;


namespace acalderonFitPause.Views
{
    public partial class vEjercicio : ContentPage
    {
        private ObservableCollection<Ejercicio> _listaEjercicios;
        private readonly Usuario _usuario;
        private Ejercicio _ejercicioSeleccionado;

        public vEjercicio(Usuario usuario)
        {
            InitializeComponent();
            _usuario = usuario;

            _listaEjercicios = new ObservableCollection<Ejercicio>();
            cvEjercicios.ItemsSource = _listaEjercicios;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarEjerciciosAsync();
        }

        private async Task CargarEjerciciosAsync()
        {
            try
            {
                var respuesta = await servicioSupabase.ConsultarAsync(
                    configSupabase.tblEjercicio,
                    "?select=*"
                );

                if (!respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", "Problema al cargar los ejercicios", "Aceptar");
                    return;
                }

                var contenido = await respuesta.Content.ReadAsStringAsync();
                var opciones = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var lista = JsonSerializer.Deserialize<List<Ejercicio>>(contenido, opciones);

                _listaEjercicios.Clear();

                if (lista != null)
                {
                    foreach (var item in lista)
                    {
                        _listaEjercicios.Add(item);
                    }
                }

                lblCantidadEjercicios.Text = _listaEjercicios.Count + " ejercicios disponibles";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }

        // Selección por cambio de selección del CollectionView (click en PC, selección normal)
        private void cvElegirEjercicio(object sender, SelectionChangedEventArgs e)
        {
            _ejercicioSeleccionado = e.CurrentSelection?.FirstOrDefault() as Ejercicio;

            if (_ejercicioSeleccionado != null)
            {
                lblBannerTitulo.Text = _ejercicioSeleccionado.Nombre;
                lblBannerDescripcion.Text = _ejercicioSeleccionado.Descripcion;
            }
        }

        // Selección por tap en el Frame (ideal para celular)
        private void OnEjercicioTapped(object sender, TappedEventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is Ejercicio ejercicio)
            {
                _ejercicioSeleccionado = ejercicio;

                lblBannerTitulo.Text = ejercicio.Nombre;
                lblBannerDescripcion.Text = ejercicio.Descripcion;

                // Opcional: sincronizar la selección visual del CollectionView
                cvEjercicios.SelectedItem = ejercicio;
            }
        }

        private async void btnComenzarRutina_Clicked(object sender, EventArgs e)
        {
            if (_ejercicioSeleccionado == null)
            {
                await DisplayAlert("Mensaje", "Selecciona un ejercicio de tu lista.", "Aceptar");
                return;
            }

            // Pausar el monitor de actividad
            vMonitor.PausarMonitorPorRutina();

            await Navigation.PushModalAsync(new vRealizaEjercicio(_usuario, _ejercicioSeleccionado));
        }

        private async void btnAgregarEjercicio_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new vFormularioEjercicio(_usuario));
        }

        private async void btnEditarEjercicio_Clicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            var ejercicio = boton?.CommandParameter as Ejercicio;

            if (ejercicio == null)
                return;

            await Navigation.PushModalAsync(new vFormularioEjercicio(_usuario, ejercicio));
        }

        private async void btnEliminarEjercicio_Clicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            var ejercicio = boton?.CommandParameter as Ejercicio;

            if (ejercicio == null)
                return;

            bool confirmar = await DisplayAlert
            (
                "Advertencia",
                "Eliminar el ejercicio?",
                "Si",
                "No"
            );

            if (!confirmar)
                return;

            try
            {
                string consultaEjId = "?Id=eq." + ejercicio.Id;

                var respCod = await servicioSupabase.EliminarAsync(
                    configSupabase.tblEjercicio,
                    consultaEjId
                );

                if (!respCod.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", "Problema al eliminar el ejercicio", "Aceptar");
                    return;
                }

                await CargarEjerciciosAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error al eliminar ejercicio", ex.Message, "Aceptar");
            }
        }

        private async void btnAtras_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
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

        private async void btnMenuMonitor_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new vMonitor(_usuario));
        }

        private async void btnMenuEjercicio_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Ejercicio");
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

    }
}
