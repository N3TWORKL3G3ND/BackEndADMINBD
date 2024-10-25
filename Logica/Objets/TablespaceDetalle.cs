using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Objets
{
    public class TablespaceDetalle
    {
        public string nombre { get; set; } = string.Empty; // Nombre del tablespace
        public string estado { get; set; } = string.Empty; // Estado del tablespace (ej. ONLINE, OFFLINE)
        public string tipo { get; set; } = string.Empty; // Tipo de tablespace (PERMANENT, TEMPORARY, UNDO)
        public decimal tamano_total_mb { get; set; } // Tamaño total en MB
        public decimal tamano_usado_mb { get; set; } // Tamaño usado en MB
        public decimal tamano_libre_mb { get; set; } // Tamaño libre en MB
    }
}
