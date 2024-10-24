using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Responses
{
    public class ResBase
    {
        public bool resultado { get; set; }

        public string detalle { get; set; } = string.Empty;

        public List<string> errores { get; set; } = new List<string>();
    }
}
