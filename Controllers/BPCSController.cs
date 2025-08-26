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

        private BPCSVen dbBpcsVen = new BPCSVen();
        OleDbDataReader objResult;

        [HttpGet]
        [Route("Disponibilidad")]
        public dynamic obtenerDisponibilidad(){
            obj.Connection = conexionBPCS.CodAbrirConex();
            obj.CommandText =  "SELECT ITH.TPROD, ITH.TTYPE, ITH.TTDTE, ITH.TQTY, ITH.TWHS, ITH.TRES, ITH.THTIME, ITH.THORD, ITH.THWRKC FROM C20A237W.VENLX835F.ITH ITH WHERE (ITH.TTYPE='R') AND (ITH.TTDTE>=20240804) AND (ITH.TWHS='VVA')";
            objResult = obj.ExecuteReader();

            var dataTable = new DataTable();

            dataTable.Load(objResult);
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dataTable);
            obj.Connection = conexionBPCS.CodCerrarConex();
            return JSONString;
        }

        [HttpGet]
        [Route("obtenerProductoConOrdenDeFabricacionAbierta/{CentroCosto}")]
        public dynamic obtenerProductoConOrdenDeFabricacionAbierta(string centroCosto){
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dbBpcsVen.ObtenerProductosActualesSegunCentroDeCosto(centroCosto));
            return JSONString;
        }

        [HttpGet]
        [Route("ObtenerDescripcionDelProductoPorSuCodigo/{CodProduc}")]
        public dynamic ObtenerDescripcionDelProductoPorSuCodigo(string CodProduc){
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dbBpcsVen.ObtenerDescripcionDelProductoPorSuCodigo(CodProduc));
            return JSONString;
        }
    }
}