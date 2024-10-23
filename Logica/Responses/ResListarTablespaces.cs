using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logica.Responses
{
    public class ResListarTablespaces
    {
        public bool resultado { get; set; }

        public List<string> detalle { get; set; } = new List<string>();

        public List<string> errores { get; set; } = new List<string>();
    }
}
