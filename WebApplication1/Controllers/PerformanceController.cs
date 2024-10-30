using Logica.Models;
using Logica.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BackEndADMINBD.Controllers
{
    public class PerformanceController : Controller
    {
        private readonly ControlPerformance _controlPerformance;
        public PerformanceController(ControlPerformance controlPerformance)
        {

            _controlPerformance = controlPerformance ?? throw new ArgumentNullException(nameof(controlPerformance));
        }



        [HttpGet]
        [Route("API/Performance/VerVersionDeOracle")]
        public async Task<IActionResult> VerVersionDeOracle()
        {
            ResBase res = await _controlPerformance.VerVersionDeOracle();

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
