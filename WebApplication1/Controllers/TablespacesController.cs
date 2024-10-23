using Logica.Models;
//using Logica.Requests;
using Logica.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEndADMINBD.Controllers
{
    [AllowAnonymous]
    public class TablespacesController : ControllerBase
    {
        private readonly ControlTablespaces _controlTablespaces;
        public TablespacesController(ControlTablespaces controlTablespaces)
        {

            _controlTablespaces = controlTablespaces ?? throw new ArgumentNullException(nameof(controlTablespaces));
        }


        [HttpGet]
        [Route("API/Tablespaces/ListarTablespaces")]
        public async Task<IActionResult> ListarTablespaces()
        {

            ResListarTablespaces res = await _controlTablespaces.ListarTablespaces();

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(string.Join(", ", res.errores));
            }
        }


    }
}
