using acalderonFitPause.Models;
using acalderonFitPause.Utils;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace acalderonFitPause.Views;

public partial class vLogReg : ContentPage
{
    public vLogReg()
    {
        InitializeComponent();
    }

    private async void BtnFormLogin_Clicked(object sender, EventArgs e)
    {
        frmLogin.IsVisible = true;
        frmRegistro.IsVisible = false;

        LoginTabButton.BackgroundColor = Color.FromArgb("#2563FF");
        LoginTabButton.TextColor = Colors.White;

        RegisterTabButton.BackgroundColor = Colors.Transparent;
        RegisterTabButton.TextColor = Color.FromArgb("#2563FF");
        RegisterTabButton.BorderColor = Color.FromArgb("#2563FF");
        RegisterTabButton.BorderWidth = 1;

        try
        {
            string correo = txtLoginCorreo.Text?.Trim();
            string pass = txtLoginPass.Text?.Trim();

            string buscarUsuario = $"?Correo=eq.{correo}&Contrasena=eq.{pass}";

            var respuesta = await servicioSupabase.ConsultarAsync(configSupabase.tblUsuario, buscarUsuario);
        }
        catch (Exception ex)
        {

            Console.WriteLine("Error al iniciar la sesion" + ex.Message);
        }    
    }

    private void BtnFormRegistro_Clicked(object sender, EventArgs e)
    {
        frmLogin.IsVisible = false;
        frmRegistro.IsVisible = true;

        RegisterTabButton.BackgroundColor = Color.FromArgb("#2563FF");
        RegisterTabButton.TextColor = Colors.White;
        RegisterTabButton.BorderWidth = 0;

        LoginTabButton.BackgroundColor = Colors.Transparent;
        LoginTabButton.TextColor = Color.FromArgb("#2563FF");
    }

    private async void BtnLogin_Clicked(object sender, EventArgs e)
    {
        try
        {
            string correo = txtLoginCorreo.Text?.Trim().ToLower();
            string pass = txtLoginPass.Text?.Trim();

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(pass))
            {
                await DisplayAlert("Aviso", "El correo y contraseña no pueden quedar vacíos", "OK");
                return;
            }

            string cifraPass = hashPass.HashSHA256(pass);

            // Consulta por correo
            string buscaCorreo = $"?Correo=eq.{correo}";
            var respuesta = await servicioSupabase.ConsultarAsync(configSupabase.tblUsuario, buscaCorreo);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudo conectar con la base de datos", "OK");
                return;
            }

            var contenido = await respuesta.Content.ReadAsStringAsync();
            var usuarios = JsonSerializer.Deserialize<List<Usuario>>(contenido);

            if (usuarios == null || usuarios.Count == 0)
            {
                await DisplayAlert("Error", "El correo ingresado no existe", "OK");
                return;
            }

            var usuario = usuarios[0];

            if (usuario.Pass != cifraPass)
            {
                await DisplayAlert("Error", "El correo o contraseña ingresados son incorrectos", "OK");
                return;
            }

            await Navigation.PushAsync(new Views.vPrincipal(usuario));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al iniciar sesión: " + ex.Message);
        }
    }

    private async void BtnRegistro_Clicked(object sender, EventArgs e)
    {
        try
        {
            string nombre = txtRegNombre.Text;
            string correo = txtRegCorreo.Text?.Trim().ToLower();
            string pass = txtRegPass.Text;
            string pregunta = pkPregunta.SelectedItem?.ToString();
            string resPregunta = txtRespuesta.Text?.Trim();

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(resPregunta) ||
                string.IsNullOrWhiteSpace(pregunta))
            {
                await DisplayAlert("Mensaje", "Completa todos los datos del registro", "OK");
                return;
            }

            string cifraPass = hashPass.HashSHA256(pass);
            string cifraRes = hashPass.HashSHA256(resPregunta);

            var datos = new
            {   
                Nombre = nombre,
                Correo = correo,
                Pass = cifraPass,
                Pregunta = pregunta,
                Respuesta = cifraRes

            };

            var respuesta = await servicioSupabase.InsertarAsync(configSupabase.tblUsuario, datos);

            if (respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Listo!", "tu cuenta ha sido creada, por favor inicia sesión", "OK");
            }
            else
            {
                var error = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"La cuenta no pudo ser registrada: {error}", "OK");
            }

        }
        catch (Exception ex)
        {

            Console.WriteLine("Error al registrar usuario " + ex.Message);
        }
    }

    private async void btnRecuperaPass(object sender, EventArgs e)
    {
        try
        {
            string correo = await DisplayPromptAsync("Recuperar acceso", "Ingresa tu correo registrado");

            if (string.IsNullOrWhiteSpace(correo))
                return;

            correo = correo.Trim().ToLower();

            string buscaCorreo = $"?Correo=eq.{correo}";
            var respuesta = await servicioSupabase.ConsultarAsync(configSupabase.tblUsuario, buscaCorreo);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudo verificar el correo", "OK");
                return;
            }

            var contenido = await respuesta.Content.ReadAsStringAsync();
            var usuarios = JsonSerializer.Deserialize<List<Usuario>>(contenido);

            if (usuarios == null || usuarios.Count == 0)
            {
                await DisplayAlert("Error", "Correo no registrado", "OK");
                return;
            }

            var usuario = usuarios[0];

            string respuestaUsuario = await DisplayPromptAsync("Verificación", usuario.Pregunta);

            if (string.IsNullOrWhiteSpace(respuestaUsuario))
                return;

            string hashRespuesta = hashPass.HashSHA256(respuestaUsuario.Trim());

            if (hashRespuesta != usuario.Respuesta)
            {
                await DisplayAlert("Error", "Respuesta incorrecta", "OK");
                return;
            }

            string nuevaPass = await DisplayPromptAsync("Nueva contraseña", "Ingresa tu nueva contraseña", maxLength: 30);

            if (string.IsNullOrWhiteSpace(nuevaPass))
                return;

            string hashNuevaPass = hashPass.HashSHA256(nuevaPass.Trim());

            var datos = new { Pass = hashNuevaPass };
            var filtroUpdate = $"?Correo=eq.{usuario.Correo.Trim().ToLower()}";

            var resultado = await servicioSupabase.ActualizarAsync(configSupabase.tblUsuario, filtroUpdate, datos);

            if (resultado.IsSuccessStatusCode)
            {
                await DisplayAlert("Listo", "Tu contraseña ha sido actualizada", "OK");
            }
            else
            {
                var error = await resultado.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo actualizar la contraseña: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Problema al recuperar acceso: " + ex.Message, "OK");
        }
    }

}