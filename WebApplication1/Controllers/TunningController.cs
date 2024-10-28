using Logica.Models;
using Logica.Objets;
using Logica.Requests;
using Logica.Responses;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
        [Route("API/Tunning/ListarIndicesDeAlajuela")]
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
            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }

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



        [HttpPost]
        [Route("API/Tunning/CrearEstadisticasAIndice")]
        public async Task<IActionResult> CrearEstadisticasAIndice([FromBody] ReqBase req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }

            ResBase res = await _controlTunning.CrearEstadisticasAIndice(req);

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
        [Route("API/Tunning/ListarEstadisticasDeIndice")]
        public async Task<IActionResult> ListarEstadisticasDeIndice([FromBody] ReqBase req)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }

            ResListarBase<EstadisticaDto> res = await _controlTunning.ListarEstadisticasIndice(req);

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
        [Route("API/Tunning/GenerarPlanEjecucion")]
        [SwaggerOperation(Summary = "Genera un plan de ejecución para una consulta SQL.")]
        public async Task<IActionResult> GenerarPlanEjecucion([FromBody] ReqPlanEjecucion req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Ningun campo puede estar vacío.");  // Devuelve errores si el JSON no contiene los campos correctos
            }

            ResBase res = await _controlTunning.GenerarPlanEjecucion(req);

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
