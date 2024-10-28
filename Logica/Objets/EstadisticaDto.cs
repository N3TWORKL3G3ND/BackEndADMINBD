using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Objets
{
    public class EstadisticaDto
    {
        public string NombreIndice { get; set; } = string.Empty; // Cambiado para reflejar que ahora es un índice
        public int NumeroFilas { get; set; }
        public int Bloques { get; set; }
        public string DuennoDeTabla { get; set; } = string.Empty;
        public DateTime? UltimoAnalisis { get; set; }
    }
}
