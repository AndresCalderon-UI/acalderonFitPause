using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acalderonFitPause.Models
{
    public class ResumenDiario
    {
        public int PausasHoy { get; set; }
        public int RachaDias { get; set; }

        public int MetaDiariaTotal { get; set; }
        public int MetaDiariaCompletadas { get; set; }

        public HistorialPausa UltimaPausa { get; set; }

        public int PorcentajeMeta =>
            MetaDiariaTotal == 0 ? 0 :
            (int)Math.Round((double)MetaDiariaCompletadas / MetaDiariaTotal * 100.0);
    }
}

