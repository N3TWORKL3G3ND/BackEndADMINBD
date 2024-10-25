using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logica.Responses
{
    public class ResListarBase<T>
    {
        public virtual bool resultado { get; set; }

        public virtual List<T> datos { get; set; } = new List<T>();

        public virtual string detalle { get; set; } = string.Empty;

        public virtual List<string> errores { get; set; } = new List<string>();
    }
}




