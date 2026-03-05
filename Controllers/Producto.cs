using Microsoft.AspNetCore.Mvc;
using ConsultasSQL.Model;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;
using ConsultasSQL.Logic;

namespace ConsultasSQL.Controllers{
    [ApiController]
    [Route("Productos")]
    public class ProductosController : ControllerBase
    {
        private BPCSGB usuarioLogic = new BPCSGB();

        [HttpGet]
        [Route("objePorHoraProductoActualEstandar/{codProducto}")]
        public dynamic objePorHoraProductoActualEstandar(string codProducto){
            string JSONString = string.Empty;
            string descripcion = "";
            codProducto = codProducto.ToUpper();
            descripcion = usuarioLogic.ObtenerDescripcionDelProductoPorSuCodigo(codProducto);
            JSONString = JsonConvert.SerializeObject(descripcion);
            return JSONString;
        }
    }
}