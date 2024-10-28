using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Requests
{
    public class ReqRecResTabla
    {
        [Required(ErrorMessage = "El nombre de la tabla es obligatorio.")]
        public string nombreTabla { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del archivo de respaldo es obligatorio.")]
        public string nombreArchivoRespaldo { get; set; } = string.Empty;
    }
}
