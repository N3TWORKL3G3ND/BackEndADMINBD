using Logica.Models;
using Logica.Requests;

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

            ResListarBase<string> res = await _controlTablespaces.ListarTablespaces();

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
        [Route("API/Tablespaces/EliminarTablespace")]
        public async Task<IActionResult> EliminarTablespace([FromBody] ReqEliminarTablespace req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }
            

            ResEliminarTablespace res = await _controlTablespaces.EliminarTablespace(req.nombre);

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(string.Join(", ", res.errores));
            }
        }



        [HttpGet]
        [Route("API/Tablespaces/ListarTablespacesConDetalles")]
        public async Task<IActionResult> ListarTablespacesConDetalles()
        {

            ResListarTablespacesConDetalles res = await _controlTablespaces.ListarTablespacesConDetalles();

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(string.Join(", ", res.errores));
            }
        }



        [HttpPost]
        [Route("API/Tablespaces/RedimencionarTablespace")]
        public async Task<IActionResult> RedimencionarTablespace([FromBody] ReqRedimensionarTablespace req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResRedimensionarTablespace res = await _controlTablespaces.RedimensionarTablespace(req);

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
        [Route("API/Tablespaces/CrearTablespace")]
        public async Task<IActionResult> CrearTablespace([FromBody] ReqRedimensionarTablespace req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResBase res = await _controlTablespaces.CrearTablespace(req);

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
