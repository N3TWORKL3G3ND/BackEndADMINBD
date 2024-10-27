using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Requests
{
    public class ReqRecResEsquema
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string nombreEsquema { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del archivo de respaldo es obligatorio.")]
        public string nombreArchivoRespaldo { get; set; } = string.Empty;
    }
}
