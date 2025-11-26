using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using Microsoft.Maui.Controls;

namespace acalderonFitPause.Views
{
    public partial class vHistorial : ContentPage
    {
        private readonly Usuario _usuario;

        public ObservableCollection<HistorialPausa> HistorialPausas { get; set; }

        public vHistorial(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));

            HistorialPausas = new ObservableCollection<HistorialPausa>();
            BindingContext = this;

            // Rango inicial de fechas: última semana
            dtpDesde.Date = DateTime.Today.AddDays(-7);
            dtpHasta.Date = DateTime.Today;

            MarcarMenuSeleccionado("Historial");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorialAsync();
        }

        private async Task CargarHistorialAsync()
        {
            try
            {
                DateTime fechaDesde = dtpDesde.Date.Date;
                DateTime fechaHasta = dtpHasta.Date.Date.AddDays(1).AddTicks(-1);

                string fechaDesdeTexto = fechaDesde.ToString("yyyy-MM-ddTHH:mm:ss");
                string fechaHastaTexto = fechaHasta.ToString("yyyy-MM-ddTHH:mm:ss");

                // Filtro por UsuarioId + rango de fechas + select al final
                string filtro =
                    $"?UsuarioId=eq.{_usuario.Id}" +
                    $"&FechaHora=gte.{fechaDesdeTexto}" +
                    $"&FechaHora=lte.{fechaHastaTexto}" +
                    $"&select=*";

                var respuesta = await servicioSupabase.ConsultarAsync(
                    configSupabase.tblHistorial,
                    filtro);

                if (!respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", "No se pudo consultar el historial", "Aceptar");
                    return;
                }

                string json = await respuesta.Content.ReadAsStringAsync();
                var lista = JsonSerializer.Deserialize<HistorialPausa[]>(json);

                HistorialPausas.Clear();

                if (lista != null)
                {
                    foreach (var item in lista.OrderByDescending(h => h.FechaHora))
                    {
                        HistorialPausas.Add(item);
                    }
                }

                ActualizarEstadisticasYEstadoVacio();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al consultar el historial " + ex.Message, "Aceptar");
            }
        }


        private void ActualizarEstadisticasYEstadoVacio()
        {
            int totalPausas = HistorialPausas.Count;
            int tiempoTotal = HistorialPausas.Sum(h => h.DuracionReal);

            lblTotalPausas.Text = totalPausas.ToString();
            lblTiempoTotal.Text = tiempoTotal.ToString() + " min";

            bool hayDatos = totalPausas > 0;
            frmVacio.IsVisible = !hayDatos;
        }

        private async void btnEliminar_Clicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            if (boton == null)
                return;

            var contexto = boton.BindingContext as HistorialPausa;
            if (contexto == null)
                return;

            bool confirma = await DisplayAlert("Confirmación",
                                               "¿Desea eliminar este registro?",
                                               "Sí",
                                               "No");
            if (!confirma)
                return;

            try
            {
                string filtro = "?Id=eq." + contexto.Id.ToString();
                var respuesta = await servicioSupabase.EliminarAsync(configSupabase.tblHistorial, filtro);

                if (!respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error",
                                       "No se pudo eliminar el registro.",
                                       "Aceptar");
                    return;
                }

                HistorialPausas.Remove(contexto);
                ActualizarEstadisticasYEstadoVacio();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                                   "Ocurrió un error al eliminar el registro: " + ex.Message,
                                   "Aceptar");
            }
        }

        private async void dtpDesde_DateSelected(object sender, DateChangedEventArgs e)
        {
            await CargarHistorialAsync();
        }

        private async void dtpHasta_DateSelected(object sender, DateChangedEventArgs e)
        {
            await CargarHistorialAsync();
        }

        private async void btnBack_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

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
            MarcarMenuSeleccionado("Inicio");
            await Navigation.PushAsync(new vPrincipal(_usuario));
        }

        private async void btnMenuMonitor_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Monitor");
            await Navigation.PushAsync(new vMonitor(_usuario));
        }

        private async void btnMenuEjercicio_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new vEjercicio(_usuario));
        }

        private void btnMenuHistorial_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Historial");
        }

        private async void btnMenuAjustes_Clicked(object sender, EventArgs e)
        {
            MarcarMenuSeleccionado("Ajustes");
            await Navigation.PushAsync(new vConfiguracion(_usuario));
        }

        #endregion
    }
}
