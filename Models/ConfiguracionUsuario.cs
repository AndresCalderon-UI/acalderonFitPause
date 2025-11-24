using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acalderonFitPause.Models
{
    public class ConfiguracionUsuario
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int TiempoAlerta { get; set; }
        public bool NotificacionFlag { get; set; }
        public int Meta { get; set; }
    }
}

