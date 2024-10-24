using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Requests
{
    public class ReqRedimensionarTablespace
    {
        [Required(ErrorMessage = "El nombre del tablespace es obligatorio.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nuevo tamanno del tablespace es obligatorio.")]
        public int tamanno { get; set; }
    }
}
