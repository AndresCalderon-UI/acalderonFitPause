using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acalderonFitPause.Models
{
    public class HistorialPausa
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }         // FK a Usuario.Id
        public int EjercicioId { get; set; }       // FK a Ejercicio.Id
        public DateTime FechaHora { get; set; }
        public int DuracionReal { get; set; }      // en minutos
        public string FraseMotivacional { get; set; }
    }
}

