using Logica.Requests;
using Logica.Responses;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class ControlRespaldos
    {
        private readonly AdminContext _adminContext;
        private readonly IConfiguration _configuration;

        public ControlRespaldos(IConfiguration configuration, AdminContext adminContext)
        {
            _configuration = configuration;
            _adminContext = adminContext;
        }



        public async Task<ResBase> RespaldarEsquema(ReqBase req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que interactua con la base de datos
                var resultado = await _adminContext.RespaldarEsquemaAsync(req.nombre);

                // Asignar los detalles a la respuesta
                res.detalle = resultado; // Mensaje devuelto por RedimensionarTablespaceAsync

                // Verificar el resultado y asignar el estado correspondiente
                if (resultado.Contains("Error"))
                {
                    res.resultado = false;
                    res.errores.Add(res.detalle);
                }
                else
                {
                    res.resultado = true; // La operación fue exitosa
                }
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al procesar la solicitud: {ex.Message}");
            }

            return res;
        }
































    }
}
