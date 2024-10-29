using Logica.Models;
using Logica.Objets;
using Logica.Requests;
using Logica.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BackEndADMINBD.Controllers
{
    public class AuditoriaController : Controller
    {
        private readonly ControlAuditoria _controlAuditoria;
        public AuditoriaController(ControlAuditoria controlAuditoria)
        {

            _controlAuditoria = controlAuditoria ?? throw new ArgumentNullException(nameof(controlAuditoria));
        }



        [HttpPost]
        [Route("API/Auditoria/ActivarAuditoria")]
        public async Task<IActionResult> ActivarAuditoria()
        {
            ResBase res = await _controlAuditoria.ActivarAuditoria();

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
        [Route("API/Auditoria/ReiniciarBaseDatos")]
        public async Task<IActionResult> ReiniciarBaseDatos()
        {
            ResBase res = await _controlAuditoria.ReiniciarBaseDatos();

            if (res.resultado)
            {
                return Ok(res.detalle);
            }
            else
            {
                return BadRequest(res.detalle);
            }
        }



        [HttpGet]
        [Route("API/Auditoria/ComprobarEstadoAuditoria")]
        public async Task<IActionResult> ComprobarEstadoAuditoria()
        {

            ResListarBase<AuditoriaDto> res = await _controlAuditoria.ComprobarEstadoAuditoria();

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
        [Route("API/Auditoria/DesactivarAuditoria")]
        public async Task<IActionResult> DesactivarAuditoria()
        {
            ResBase res = await _controlAuditoria.DesactivarAuditoria();

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
