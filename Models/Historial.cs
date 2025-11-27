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
        public int UsuarioId { get; set; }
        public int EjercicioId { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionReal { get; set; }
        public string FraseMotivacional { get; set; }
    }
}

