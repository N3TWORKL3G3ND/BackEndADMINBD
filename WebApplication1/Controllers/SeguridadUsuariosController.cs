using Logica.Models;
using Logica.Objets;
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
























    }
}
