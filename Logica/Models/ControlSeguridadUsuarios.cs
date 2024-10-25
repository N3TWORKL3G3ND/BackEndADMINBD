using Logica.Objets;
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
    public class ControlSeguridadUsuarios
    {
        private readonly AdminContext _adminContext;
        private readonly IConfiguration _configuration;

        public ControlSeguridadUsuarios(IConfiguration configuration, AdminContext adminContext)
        {
            _configuration = configuration;
            _adminContext = adminContext;
        }



        public async Task<ResListarBase<UsuarioDto>> ListarUsuarios()
        {
            ResListarBase<UsuarioDto> res = new ResListarBase<UsuarioDto>();

            try
            {
                res.datos = await _adminContext.ListarUsuariosAsync();
                res.resultado = true;
                res.detalle = "Usuarios listados correctamente.";
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al listar usuarios: {ex.Message}");
            }

            return res;
        }



        public async Task<ResListarBase<RoleDto>> ListarRoles()
        {
            ResListarBase<RoleDto> res = new ResListarBase<RoleDto>();

            try
            {
                res.datos = await _adminContext.ListarRolesAsync();
                res.resultado = true;
                res.detalle = "Roles listados correctamente.";
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.errores.Add($"Error al listar roles: {ex.Message}");
            }

            return res;
        }



        public async Task<ResBase> CrearUsuario(ReqCrearUsuario req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que redimensiona el tablespace
                var resultado = await _adminContext.CrearUsuarioAsync(req.nombre, req.contrasenna, req.nombreTablespace, req.nombreTablespaceTemporal, req.nombreRol);

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



        public async Task<ResBase> EliminarUsuario(ReqBase req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que redimensiona el tablespace
                var resultado = await _adminContext.EliminarUsuarioAsync(req.nombre);

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



        public async Task<ResBase> CrearRol(ReqBase req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que redimensiona el tablespace
                var resultado = await _adminContext.CrearRolAsync(req.nombre);

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



        public async Task<ResBase> EliminarRol(ReqBase req)
        {
            var res = new ResBase();

            try
            {
                // Llamar al método que interactua con la base de datos
                var resultado = await _adminContext.EliminarRolAsync(req.nombre);

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
