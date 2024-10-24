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



        public async Task<ResListarTablespacesConDetalles> ListarTablespacesConDetalles()
        {
            ResListarTablespacesConDetalles res = new ResListarTablespacesConDetalles();

            try
            {
                // Llamar al método que obtiene la lista de tablespaces con detalles
                var tablespacesConDetalles = await _adminContext.ListarTablespacesConDetallesAsync();

                // Verificar si se obtuvieron resultados
                if (tablespacesConDetalles == null || tablespacesConDetalles.Count == 0)
                {
                    res.resultado = false;
                    res.errores.Add("No se obtuvieron detalles de los tablespaces.");
                    return res;
                }

                // Asignar los detalles de los tablespaces a la respuesta
                res.detalle = tablespacesConDetalles;

                res.resultado = true; // Todo salió bien
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al procesar la solicitud: {ex.Message}");
            }

            return res;
        }





        public async Task<ResEliminarTablespace> EliminarTablespace(string nombre)
        {
            ResEliminarTablespace res = new ResEliminarTablespace();

            try
            {
                // Llamar al método que elimina el tablespace
                var resultadoEliminacion = await _adminContext.EliminarTablespaceAsync(nombre);

                // Verificar el resultado de la eliminación
                if (!resultadoEliminacion)
                {
                    res.resultado = false;
                    res.errores.Add("No se pudo eliminar el tablespace.");
                    return res;
                }

                res.detalle = $"Tablespace '{nombre}' eliminado correctamente."; // Detalle del resultado
                res.resultado = true; // Todo salió bien
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"{ex.Message}");
            }

            return res;
        }




    }
}
