using Microsoft.AspNetCore.Mvc;
using ConsultasSQL.Logic;

namespace ConsultasSQL.Controllers
{
    [ApiController]
    [Route("EAM")]
    public class EAMController : ControllerBase
    {
        private readonly EAM _eam;

        // Inyección por constructor ✅
        public EAMController(EAM eam)
        {
            _eam = eam;
        }

        [HttpGet("equipos/{centroCosto}")]
        public IActionResult ObtenerEquiposEAMSegunCentroDeCosto(string centroCosto)
        {
            var data = _eam.ObtenerEquiposEAMSegunCentroDeCosto(centroCosto);
            return Ok(data);
        }
    }
}