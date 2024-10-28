using Logica.Objets;
using Logica.Requests;
using Logica.Responses;
using Logica.Services;
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
        private readonly FileService _fileService;

        public ControlRespaldos(IConfiguration configuration, AdminContext adminContext, FileService fileService)
        {
            _configuration = configuration;
            _adminContext = adminContext;
            _fileService = fileService;
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



        public async Task<ResBase> RespaldarFull()
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que interactua con la base de datos
                var resultado = await _adminContext.RespaldarFullAsync();

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



        public async Task<ResListarBase<string>> ListarTablasDePadron()
        {
            ResListarBase<string> res = new ResListarBase<string>();

            try
            {
                res.datos = await _adminContext.ListarTablasDeEsquemaAsync("PADRON");
                res.resultado = true;
                res.detalle = "Tablas listadas correctamente.";
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al listar las tablas: {ex.Message}");
            }

            return res;
        }



        public async Task<ResBase> RespaldarTabla(ReqBase req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que interactua con la base de datos
                var resultado = await _adminContext.RespaldarTablaAsync("PADRON", req.nombre);

                // Asignar los detalles a la respuesta
                res.detalle = resultado; // Mensaje devuelto

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



        public async Task<ResBase> RecuperarRespaldoEsquema(ReqRecResEsquema req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que interactua con la base de datos
                var resultado = await _adminContext.RecuperarRespaldoEsquemaAsync(req.nombreEsquema, req.nombreArchivoRespaldo);

                // Asignar los detalles a la respuesta
                res.detalle = resultado; // Mensaje devuelto

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



        public async Task<ResListarBase<string>> ListarArchivosDmp()
        {
            ResListarBase<string> res = new ResListarBase<string>();

            try
            {
                res.datos = await _fileService.ListarArchivosDmpAsync();
                res.resultado = true;
                res.detalle = "Nombres de archivos listados correctamente.";
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al listar los nombres de archivos: {ex.Message}");
            }

            return res;
        }












    }
}
