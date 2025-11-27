using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acalderonFitPause.Utils
{
    public static class configSupabase
    {
        public const string SupabaseUrl = "https://gvydbwiqsromdolpnwfv.supabase.co";

        public const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd2eWRid2lxc3JvbWRvbHBud2Z2Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjE0MjM4NTAsImV4cCI6MjA3Njk5OTg1MH0.tnD8NOJh7vLehITDSxAh5HuvaPlL4B4_710zyU91mvQ";

        public const string tblUsuario = "Usuario";
        public const string tblEjercicio = "Ejercicio";
        public const string tblHistorial = "Historial";
        public const string tblConfiguracion = "Configuracion";

        public static HttpClient CrearCliente()
        {
            var cliente = new HttpClient();
            cliente.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            cliente.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            return cliente;
        }

        // Método para construir la URL completa de una tabla
        public static string ObtenerUrlTabla(string _tabla)
        {
            return $"{SupabaseUrl}/rest/v1/{_tabla}";
        }
    }
}
