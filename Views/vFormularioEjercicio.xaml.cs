using acalderonFitPause.Models;
using acalderonFitPause.Utils;

namespace acalderonFitPause.Views
{
    public partial class vFormularioEjercicio : ContentPage
    {
        private readonly Usuario _usuario;
        private readonly Ejercicio _ejercicio;
        private readonly bool _esEdicion;

        public vFormularioEjercicio(Usuario usuario, Ejercicio ejercicio = null)
        {
            InitializeComponent();

            _usuario = usuario;
            _ejercicio = ejercicio;
            _esEdicion = ejercicio != null;

            if (_esEdicion)
            {
                Title = "Editar ejercicio";
                lblTituloFormulario.Text = "Editar ejercicio";
                btnGuardar.Text = "Guardar cambios";

                txtNombre.Text = ejercicio.Nombre;
                txtDescripcion.Text = ejercicio.Descripcion;
                txtDuracion.Text = ejercicio.Duracion.ToString();
                txtCategoria.Text = ejercicio.Categoria;
                txtDificultad.Text = ejercicio.Dificultad;
                txtImagenUrl.Text = ejercicio.ImagenUrl;
            }
            else
            {
                Title = "Nuevo ejercicio";
                lblTituloFormulario.Text = "Nuevo ejercicio";
                btnGuardar.Text = "Guardar";
            }
        }

        private async void btnGuardar_Clicked(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text?.Trim();
            string descripcion = txtDescripcion.Text?.Trim();
            string categoria = txtCategoria.Text?.Trim();
            string dificultad = txtDificultad.Text?.Trim();
            string imagenUrl = txtImagenUrl.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                await DisplayAlert("Validación", "El nombre es obligatorio", "Aceptar");
                return;
            }

            int duracion = 0;
            int.TryParse(txtDuracion.Text, out duracion);

            try
            {
                if (_esEdicion)
                {
                    var datosActualizados = new
                    {
                        Nombre = nombre,
                        Descripcion = descripcion,
                        Duracion = duracion,
                        Categoria = categoria,
                        Dificultad = dificultad,
                        ImagenUrl = imagenUrl
                    };

                    string filtro = "?Id=eq." + _ejercicio.Id;

                    var resp = await servicioSupabase.ActualizarAsync(
                        configSupabase.tblEjercicio,
                        filtro,
                        datosActualizados);

                    if (!resp.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Error", "El ejercicio no fue actualizado", "Aceptar");
                        return;
                    }
                }
                else
                {
                    var nuevo = new Ejercicio
                    {
                        Nombre = nombre,
                        Descripcion = descripcion,
                        Duracion = duracion,
                        Categoria = categoria,
                        Dificultad = dificultad,
                        ImagenUrl = imagenUrl
                    };

                    var resp = await servicioSupabase.InsertarAsync(
                        configSupabase.tblEjercicio,
                        nuevo);

                    if (!resp.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Error", "Problema al guardar el ejercicio", "Aceptar");
                        return;
                    }
                }

                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }

        private async void btnCancelar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
