//using Logica.Requests;
using Logica.Responses;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class ControlTablespaces
    {
        private readonly AdminContext _adminContext;
        private readonly IConfiguration _configuration;

        public ControlTablespaces(IConfiguration configuration, AdminContext adminContext)
        {
            _configuration = configuration;
            _adminContext = adminContext;
        }



        public async Task<ResListarTablespaces> ListarTablespaces()
        {
            ResListarTablespaces res = new ResListarTablespaces();
            

            try
            {
                // Llamar al método que obtiene la lista de tablespaces
                var tablespaces = await _adminContext.ListarTablespacesAsync();

                if (tablespaces == null || tablespaces.Count == 0)
                {
                    res.resultado = false;
                    res.errores.Add("No se obtuvieron tablespaces.");
                    return res;
                }

                // Asignar los tablespaces al detalle
                res.detalle = tablespaces; // Asignar la lista directamente a la propiedad detalle

                res.resultado = true; // Todo salió bien
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al procesar la solicitud:: {ex.Message}");
            }

            return res;
        }



    }
}
