using Logica.Models;
using Logica.Objets;
using Logica.Requests;
using Logica.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BackEndADMINBD.Controllers
{
    public class SeguridadUsuariosController : Controller
    {
        private readonly ControlSeguridadUsuarios _controlSeguridadUsuarios;
        public SeguridadUsuariosController(ControlSeguridadUsuarios controlSeguridadUsuarios)
        {

            _controlSeguridadUsuarios = controlSeguridadUsuarios ?? throw new ArgumentNullException(nameof(controlSeguridadUsuarios));
        }



        [HttpGet]
        [Route("API/SeguridadUsuarios/ListarUsuarios")]
        public async Task<IActionResult> ListarUsuarios()
        {

            ResListarBase<UsuarioDto> res = await _controlSeguridadUsuarios.ListarUsuarios();

            if (res.resultado)
            {
                return Ok(res.datos);
            }
            else
            {
                return BadRequest(string.Join(", ", res.errores));
            }
        }



        [HttpGet]
        [Route("API/SeguridadUsuarios/ListarRoles")]
        public async Task<IActionResult> ListarRoles()
        {

            ResListarBase<RoleDto> res = await _controlSeguridadUsuarios.ListarRoles();

            if (res.resultado)
            {
                return Ok(res.datos);
            }
            else
            {
                return BadRequest(string.Join(", ", res.errores));
            }
        }



        [HttpPost]
        [Route("API/SeguridadUsuarios/CrearUsuario")]
        public async Task<IActionResult> CrearUsuario([FromBody] ReqCrearUsuario req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResBase res = await _controlSeguridadUsuarios.CrearUsuario(req);

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(res.detalle);
            }
        }



        [HttpPost]
        [Route("API/SeguridadUsuarios/EliminarUsuario")]
        public async Task<IActionResult> EliminarUsuario([FromBody] ReqBase req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResBase res = await _controlSeguridadUsuarios.EliminarUsuario(req);

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(res.detalle);
            }
        }



        [HttpPost]
        [Route("API/SeguridadUsuarios/CrearRol")]
        public async Task<IActionResult> CrearRol([FromBody] ReqBase req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResBase res = await _controlSeguridadUsuarios.CrearRol(req);

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(res.detalle);
            }
        }



        [HttpPost]
        [Route("API/SeguridadUsuarios/EliminarRol")]
        public async Task<IActionResult> EliminarRol([FromBody] ReqBase req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResBase res = await _controlSeguridadUsuarios.EliminarRol(req);

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(res.detalle);
            }
        }











    }
}
