using Microsoft.AspNetCore.Mvc;
using ConsultasSQL.Logic;
using System.Data.Odbc;      // importante

namespace ConsultasSQL.Controllers
{
    [ApiController]
    [Route("BPCS")]
    public class BPCSController : ControllerBase
    {
        private readonly IBpcsConnectionFactory _bpcsConn;
        private readonly BPCSGB _bpcsGB;

        public BPCSController(IBpcsConnectionFactory connFactory, BPCSGB bpcsGB)
        {
            _bpcsConn = connFactory;
            _bpcsGB   = bpcsGB;
        }

        // GET BPCS/Disponibilidad
        [HttpGet("Disponibilidad")]
        public IActionResult ObtenerDisponibilidad()
        {
            // SQL corregido a System Naming
            string sql = @"
                SELECT 
                    ITH.TPROD, ITH.TTYPE, ITH.TTDTE, ITH.TQTY, 
                    ITH.TWHS,  ITH.TRES,  ITH.THTIME, ITH.THORD, ITH.THWRKC
                FROM GBYLX835F/ITH ITH
                WHERE 
                    ITH.TTYPE = 'R'
                    AND ITH.TTDTE >= 20260101
                    AND RTRIM(ITH.TWHS) = 'VVA'
            ";

            using var conn = _bpcsConn.CreateOpen();
            using var cmd  = new OdbcCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var table = new System.Data.DataTable();
            table.Load(reader);

            return Ok(table);
        }

        // GET BPCS/obtenerProductoConOrdenDeFabricacionAbierta/...
        [HttpGet("obtenerProductoConOrdenDeFabricacionAbierta/{centroCosto}")]
        public IActionResult ObtenerProductoConOrdenDeFabricacionAbierta(string centroCosto)
        {
            var productos = _bpcsGB.ObtenerProductosActualesSegunCentroDeCosto(centroCosto);
            return Ok(productos);
        }

        // GET BPCS/ObtenerDescripcionDelProductoPorSuCodigo/...
        [HttpGet("ObtenerDescripcionDelProductoPorSuCodigo/{CodProduc}")]
        public IActionResult ObtenerDescripcionDelProductoPorSuCodigo(string CodProduc)
        {
            var data = _bpcsGB.ObtenerDescripcionDelProductoPorSuCodigo(CodProduc);
            return Ok(data);
        }
    }
}
