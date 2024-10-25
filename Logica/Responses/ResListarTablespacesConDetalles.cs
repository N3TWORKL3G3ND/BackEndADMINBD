using Logica.Objets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Responses
{
    public class ResListarTablespacesConDetalles
    {
        public bool resultado { get; set; } // Indica si la operación fue exitosa
        public List<TablespaceDto> detalle { get; set; } = new List<TablespaceDto>(); // Lista de detalles de tablespaces
        public List<string> errores { get; set; } = new List<string>(); // Lista de errores en caso de fallo
    }
}
