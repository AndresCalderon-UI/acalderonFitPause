using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acalderonFitPause.Models
{
        public class Ejercicio
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public int Duracion { get; set; }
            public string Categoria { get; set; }
            public string ImagenUrl { get; set; }
            public string Dificultad { get; set; }
        }
}
