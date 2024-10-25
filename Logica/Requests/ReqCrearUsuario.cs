using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Requests
{
    public class ReqCrearUsuario
    {
        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasenna del usuario es obligatoria.")]
        public string contrasenna { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del tablespace es obligatorio.")]
        public string nombreTablespace { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del tablespace temporal es obligatorio.")]
        public string nombreTablespaceTemporal { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        public string nombreRol { get; set; } = string.Empty;
    }
}
