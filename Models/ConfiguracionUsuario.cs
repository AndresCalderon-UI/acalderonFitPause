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
        public int UsuarioId { get; set; }          // FK a Usuario.Id
        public int TiempoAlerta { get; set; }       // minutos sin moverse (ej: 45)
        public bool NotificacionesActivas { get; set; }
        public string Tema { get; set; }            // "Claro", "Oscuro", etc.
    }
}

