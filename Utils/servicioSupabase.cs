using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace acalderonFitPause.Utils
{
    public static class servicioSupabase
    {
        private static HttpClient CrearCliente()
        {
            var cliente = new HttpClient();
            cliente.DefaultRequestHeaders.Add("apikey", Utils.configSupabase.SupabaseKey);
            cliente.DefaultRequestHeaders.Add("Authorization", $"Bearer {Utils.configSupabase.SupabaseKey}");
            return cliente;
        }

        public static async Task<HttpResponseMessage> InsertarAsync(string tabla, object datos)
        {
            var cliente = CrearCliente();
            var json = JsonSerializer.Serialize(datos);
            var contenido = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{Utils.configSupabase.SupabaseUrl}/rest/v1/{tabla}";
            return await cliente.PostAsync(url, contenido);
        }

        public static async Task<HttpResponseMessage> ConsultarAsync(string tabla, string filtro = "")
        {
            var cliente = CrearCliente();
            var url = $"{Utils.configSupabase.SupabaseUrl}/rest/v1/{tabla}{filtro}";
            return await cliente.GetAsync(url);
        }

        public static async Task<HttpResponseMessage> ActualizarAsync(string tabla, string filtro, object datos)
        {
            var cliente = CrearCliente();
            var json = JsonSerializer.Serialize(datos);
            var contenido = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{Utils.configSupabase.SupabaseUrl}/rest/v1/{tabla}{filtro}";
            var metodo = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(metodo, url) { Content = contenido };
            return await cliente.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> EliminarAsync(string tabla, string filtro)
        {
            var cliente = CrearCliente();
            var url = $"{Utils.configSupabase.SupabaseUrl}/rest/v1/{tabla}{filtro}";
            return await cliente.DeleteAsync(url);
        }
    }
}