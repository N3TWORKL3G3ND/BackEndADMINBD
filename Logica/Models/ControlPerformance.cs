﻿using Logica.Responses;
using Logica.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class ControlPerformance
    {
        private readonly AdminContext _adminContext;
        private readonly IConfiguration _configuration;
        private readonly FileService _fileService;

        public ControlPerformance(IConfiguration configuration, AdminContext adminContext, FileService fileService)
        {
            _configuration = configuration;
            _adminContext = adminContext;
            _fileService = fileService;
        }


        public async Task<ResBase> VerVersionDeOracle()
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que interactua con la base de datos
                var resultado = await _adminContext.ObtenerVersionCompatibleAsync();

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









    }
}