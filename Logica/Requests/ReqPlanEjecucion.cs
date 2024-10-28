using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;


namespace Logica.Requests
{
    public class ReqPlanEjecucion
    {
        [Required(ErrorMessage = "La consulta es obligatoria.")]
        [SwaggerSchema("Consulta SQL", Description = "La consulta SQL para la cual se generará el plan de ejecución.")]
        public string consultaSQL { get; set; } = string.Empty;
    }
}
