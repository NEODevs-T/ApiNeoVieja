using Microsoft.AspNetCore.Mvc;
using ConsultasSQL.Logic;

namespace ConsultasSQL.Controllers
{
    [ApiController]
    [Route("OEE")]
    [Route("Ing")]
    public class IngDocController : ControllerBase
    {
        private readonly BPCS _bpcs;

        // Inyecta BPCS (que a su vez usa IBpcsConnectionFactory)
        public IngDocController(BPCS bpcs)
        {
            _bpcs = bpcs;
        }

        [HttpGet("objePorHoraProductoActualEstandar/{periodo:int}")]
        public IActionResult ObjePorHoraProductoActualEstandar(int periodo)
        {
            // Si este "warm-up" no es necesario, elimínalo:
            var _ = _bpcs.MaquinaProductosProduccionActual1turno();

            var data = _bpcs.ObjetivoPorHoraSegunProducto(periodo);
            return Ok(data);
        }

        [HttpGet("ProduccionCajaActualPropia1turno")]
        public IActionResult ProduccionCajaActualPorHoraPropia1turno()
        {
            var data = _bpcs.MaquinaProductosProduccionActual1turno();
            return Ok(data);
        }

        [HttpGet("ProduccionCajaActualEstandar1turno")]
        public IActionResult ProduccionCajaActualPorHoraEstandar1turno()
        {
            var actual = _bpcs.MaquinaProductosProduccionActual1turno();
            var estandar = _bpcs.conversionTotalAEstandarPormaquinaYproducto(actual);
            return Ok(estandar);
        }

        [HttpGet("ProduccionCajaActualPropia2turno/{band:bool}")]
        public IActionResult ProduccionCajaActualPorHoraPropia2turno(bool band)
        {
            var data = band
                ? _bpcs.MaquinaProductosProduccionActual2turnoAntes0am()
                : _bpcs.MaquinaProductosProduccionActual2turnoDespues0am();
            return Ok(data);
        }

        [HttpGet("ProduccionCajaActualEstanadar2turno/{band:bool}")]
        public IActionResult ProduccionCajaActualPorHoraEstanadar2turno(bool band)
        {
            var actual = band
                ? _bpcs.MaquinaProductosProduccionActual2turnoAntes0am()
                : _bpcs.MaquinaProductosProduccionActual2turnoDespues0am();

            var estandar = _bpcs.conversionTotalAEstandarPormaquinaYproducto(actual);
            return Ok(estandar);
        }

        [HttpGet("ProduccionCajaActualPorHoraPropia/{tiempo:int}")]
        public IActionResult ProduccionCajaActualPorHoraPropia(int tiempo)
        {
            Dictionary<string, Dictionary<string, int>> produccion;

            if (tiempo == 1)
                produccion = _bpcs.MaquinaProductosProduccionActual1turno();
            else if (tiempo == 2)
                produccion = _bpcs.MaquinaProductosProduccionActual2turnoAntes0am();
            else if (tiempo == 3)
                produccion = _bpcs.MaquinaProductosProduccionActual2turnoDespues0am();
            else
                return Ok(new { });

            var porHora = _bpcs.ProduccionActualPorMaquinaPorHora(produccion, tiempo);
            return Ok(porHora);
        }

        [HttpGet("ProduccionCajaActualPorHoraEstandar/{tiempo:int}")]
        public IActionResult ProduccionCajaActualPorHoraEstanadar(int tiempo)
        {
            Dictionary<string, Dictionary<string, int>> produccion;

            if (tiempo == 1)
                produccion = _bpcs.MaquinaProductosProduccionActual1turno();
            else if (tiempo == 2)
                produccion = _bpcs.MaquinaProductosProduccionActual2turnoAntes0am();
            else if (tiempo == 3)
                produccion = _bpcs.MaquinaProductosProduccionActual2turnoDespues0am();
            else
                return Ok(new { });

            var estandar = _bpcs.conversionTotalAEstandarPormaquinaYproducto(produccion);
            var porHora  = _bpcs.ProduccionActualPorMaquinaPorHora(estandar, tiempo);
            return Ok(porHora);
        }

        [HttpGet("obtenerCajasPorHoraPropia/{tiempo:int}")]
        public IActionResult ObtenerCajasPorHoraPropia(int tiempo)
        {
            Dictionary<string, Dictionary<string, List<int>>> produccion;

            if (tiempo == 1)
                produccion = _bpcs.obtenerLaProduccionActual1turno();
            else if (tiempo == 2)
                produccion = _bpcs.obtenerLaProduccionActual2turno(true);
            else if (tiempo == 3)
                produccion = _bpcs.obtenerLaProduccionActual2turno(false);
            else
                return Ok(new { });

            return Ok(produccion);
        }

        [HttpGet("obtenerCajasPorHoraEstandar/{tiempo:int}")]
        public IActionResult ObtenerCajasPorHoraEstandar(int tiempo)
        {
            Dictionary<string, Dictionary<string, List<int>>> produccion;

            if (tiempo == 1)
                produccion = _bpcs.obtenerLaProduccionActual1turno();
            else if (tiempo == 2)
                produccion = _bpcs.obtenerLaProduccionActual2turno(true);
            else if (tiempo == 3)
                produccion = _bpcs.obtenerLaProduccionActual2turno(false);
            else
                return Ok(new { });

            var estandar = _bpcs.conversionTotalAEstandarPormaquinaYproducto(produccion);
            return Ok(estandar);
        }

        [HttpGet("obtenerProductosActuales")]
        public IActionResult ObtenerProductosActuales()
        {
            var data = _bpcs.obtenerLosProductosActuales();
            return Ok(data);
        }

        [HttpGet("obtenerProductosActualesDeLaLiena/{centroCosto}")]
        public IActionResult ObtenerProductosActualesDeLaLiena(string centroCosto)
        {
            var data = _bpcs.obtenerLosProductosActualesDeLinea(centroCosto);
            return Ok(data);
        }
    }
}