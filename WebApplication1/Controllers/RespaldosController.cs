using Logica.Models;
using Logica.Requests;
using Logica.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BackEndADMINBD.Controllers
{
    public class RespaldosController : Controller
    {
        private readonly ControlRespaldos _controlRespaldos;
        public RespaldosController(ControlRespaldos controlRespaldos)
        {

            _controlRespaldos = controlRespaldos ?? throw new ArgumentNullException(nameof(controlRespaldos));
        }



        [HttpPost]
        [Route("API/Respaldos/RespaldarEsquema")]
        public async Task<IActionResult> RespaldarEsquema([FromBody] ReqBase req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }


            ResBase res = await _controlRespaldos.RespaldarEsquema(req);

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
        [Route("API/Respaldos/RespaldarFull")]
        public async Task<IActionResult> RespaldarFull()
        {
            ResBase res = await _controlRespaldos.RespaldarFull();

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
