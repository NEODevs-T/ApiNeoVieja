using Microsoft.AspNetCore.Mvc;
using ConsultasSQL.Model;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;
using ConsultasSQL.Logic;

namespace ConsultasSQL.Controllers{
    [ApiController]
    [Route("BPCS")]
    public class BPCSController : ControllerBase
    {
        private DBconexionBPCS conexionBPCS = new DBconexionBPCS();
        private OleDbCommand obj = new OleDbCommand();

        private BPCSGB dbBpcsGB = new BPCSGB();
        OleDbDataReader objResult;

        [HttpGet]
        [Route("Disponibilidad")]
        public dynamic obtenerDisponibilidad(){
            obj.Connection = conexionBPCS.CodAbrirConex();
            obj.CommandText =  "SELECT ITH.TPROD, ITH.TTYPE, ITH.TTDTE, ITH.TQTY, ITH.TWHS, ITH.TRES, ITH.THTIME, ITH.THORD, ITH.THWRKC FROM X7073a51.GBYLX835F.ITH ITH WHERE (ITH.TTYPE='R') AND (ITH.TTDTE>=20260101) AND (ITH.TWHS='VVA')";
            objResult = obj.ExecuteReader();

            var dataTable = new DataTable();

            dataTable.Load(objResult);
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dataTable);
            obj.Connection = conexionBPCS.CodCerrarConex();
            return JSONString;
        }

        [HttpGet]
        [Route("obtenerProductoConOrdenDeFabricacionAbierta/{centroCosto}")]
        public IActionResult obtenerProductoConOrdenDeFabricacionAbierta([FromRoute] string centroCosto)
        {
            var productos = dbBpcsGB.ObtenerProductosActualesSegunCentroDeCosto(centroCosto);
            return Ok(productos);
        }

        [HttpGet]
        [Route("ObtenerDescripcionDelProductoPorSuCodigo/{CodProduc}")]
        public dynamic ObtenerDescripcionDelProductoPorSuCodigo(string CodProduc){
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dbBpcsGB.ObtenerDescripcionDelProductoPorSuCodigo(CodProduc));
            return JSONString;
        }
    }
}