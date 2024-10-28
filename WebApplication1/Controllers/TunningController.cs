using Logica.Models;
using Logica.Requests;
using Logica.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BackEndADMINBD.Controllers
{
    public class TunningController : Controller
    {
        private readonly ControlTunning _controlTunning;
        public TunningController(ControlTunning controlTunning)
        {

            _controlTunning = controlTunning ?? throw new ArgumentNullException(nameof(controlTunning));
        }



        [HttpGet]
        [Route("API/Tunning/ListarIndicesDePadron")]
        public async Task<IActionResult> ListarIndicesDeAlajuela()
        {

            ResListarBase<string> res = await _controlTunning.ListarIndicesDeTabla();

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
        [Route("API/Tunning/GenerarIndiceAAlajuela")]
        public async Task<IActionResult> GenerarIndiceAAlajuela()
        {
            ResBase res = await _controlTunning.GenerarIndiceALAJUELA();

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
        [Route("API/Tunning/EliminarIndice")]
        public async Task<IActionResult> EliminarIndice([FromBody] ReqBase req)
        {
            ResBase res = await _controlTunning.EliminarIndice(req);

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
