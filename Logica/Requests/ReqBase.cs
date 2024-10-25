using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Requests
{
    public class ReqBase
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string nombre { get; set; } = string.Empty;
    }
}
