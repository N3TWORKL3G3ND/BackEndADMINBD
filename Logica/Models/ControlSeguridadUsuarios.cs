using Logica.Objets;
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































    }
}
