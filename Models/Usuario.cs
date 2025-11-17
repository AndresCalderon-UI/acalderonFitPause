using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acalderonFitPause.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Alias { get; set; }
        public string Correo { get; set; }
        public string Pass { get; set; }
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
    }
}
