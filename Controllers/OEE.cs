using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ConsultasSQL.Model;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;
using ConsultasSQL.Logic;
namespace ConsultasSQL.Controllers{
    [ApiController]
    [Route("OEE")]
    [Route("Ing")]

    public class IngDocController : ControllerBase
    {
        private DbIngDoc conexionIngDoc = new DbIngDoc();
        private SqlCommand CommandIngDoc = new SqlCommand();
        private SqlDataReader? DataReaderIngDoc;

        private DBconexionBPCS conexionBPCS = new DBconexionBPCS();
        private OleDbCommand CommandBPCS = new OleDbCommand();
        OleDbDataReader? DataReaderBPCS;


        private DbSIPDATABASE2 conexionSIPDATABASE2 = new DbSIPDATABASE2();
        private SqlCommand comandSIPDATABASE2 = new SqlCommand();

        private BPCS bpcs = new BPCS();

        private Gespline gespline = new Gespline();

        SqlDataReader? DataReaderSIPDATABASE2;
        

        [HttpGet]
        [Route("objePorHoraProductoActualEstandar/{periodo}")]
        public dynamic objePorHoraProductoActualEstandar(int periodo){
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(bpcs.ObjetivoPorHoraSegunProducto(periodo));
            return JSONString;
        }

        [HttpGet]
        [Route("ProduccionCajaActualPropia1turno")]
        public dynamic ProduccionCajaActualPorHoraPropia1turno(){
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(bpcs.MaquinaProductosProduccionActual1turno());
            return JSONString;
        }

        [HttpGet]
        [Route("ProduccionCajaActualEstandar1turno")]
        public dynamic ProduccionCajaActualPorHoraEstandar1turno(){
            string JSONString = string.Empty;
            Dictionary<string, Dictionary<string, int>>  a = bpcs.MaquinaProductosProduccionActual1turno();
            JSONString = JsonConvert.SerializeObject(bpcs.conversionTotalAEstandarPormaquinaYproducto(a));
            return JSONString;
        }

        [HttpGet]
        [Route("ProduccionCajaActualPropia2turno/{band}")]
        public dynamic ProduccionCajaActualPorHoraPropia2turno(bool band){
            string JSONString = string.Empty;
            if(band){
                JSONString = JsonConvert.SerializeObject(bpcs.MaquinaProductosProduccionActual2turnoAntes0am());
            }else{
                JSONString = JsonConvert.SerializeObject(bpcs.MaquinaProductosProduccionActual2turnoDespues0am());
            }
            return JSONString;
        }

        [HttpGet]
        [Route("ProduccionCajaActualEstanadar2turno/{band}")]
        public dynamic ProduccionCajaActualPorHoraEstanadar2turno(bool band){
            string JSONString = string.Empty;
            Dictionary<string, Dictionary<string, int>>  a;
            if(band){
                a = bpcs.MaquinaProductosProduccionActual2turnoAntes0am();
            }else{
                a = bpcs.MaquinaProductosProduccionActual2turnoDespues0am();
            }
            JSONString = JsonConvert.SerializeObject(bpcs.conversionTotalAEstandarPormaquinaYproducto(a));
            return JSONString;
        }

        [HttpGet]
        [Route("ProduccionCajaActualPorHoraPropia/{tiempo}")]
        public dynamic ProduccionCajaActualPorHoraPropia(int tiempo){
            string JSONString = string.Empty;
            Dictionary<string,Dictionary<string,int>> produccion = new Dictionary<string,Dictionary<string,int>>();
            if(tiempo == 1){ //* si es igual a 1 es primer turno
                produccion = bpcs.MaquinaProductosProduccionActual1turno();
            }else if(tiempo == 2){//* si es igual es 2 es primer turno antes de 0 am
                produccion = bpcs.MaquinaProductosProduccionActual2turnoAntes0am();
            }else if(tiempo == 3){ //* si es igual es 2 es primer turno despues de 0 am
                produccion = bpcs.MaquinaProductosProduccionActual2turnoDespues0am();
            }else{
                return JSONString;
            }
            JSONString = JsonConvert.SerializeObject(bpcs.ProduccionActualPorMaquinaPorHora(produccion,tiempo));
            return JSONString;
        }

        [HttpGet]
        [Route("ProduccionCajaActualPorHoraEstandar/{tiempo}")]
        public dynamic ProduccionCajaActualPorHoraEstanadar(int tiempo){
            string JSONString = string.Empty;
            Dictionary<string,Dictionary<string,int>> produccion = new Dictionary<string,Dictionary<string,int>>();
            if(tiempo == 1){ //* si es igual a 1 es primer turno
                produccion = bpcs.MaquinaProductosProduccionActual1turno();
            }else if(tiempo == 2){//* si es igual es 2 es primer turno antes de 0 am
                produccion = bpcs.MaquinaProductosProduccionActual2turnoAntes0am();
            }else if(tiempo == 3){ //* si es igual es 2 es primer turno despues de 0 am
                produccion = bpcs.MaquinaProductosProduccionActual2turnoDespues0am();
            }else{
                return JSONString;
            }
            produccion = bpcs.conversionTotalAEstandarPormaquinaYproducto(produccion);
            JSONString = JsonConvert.SerializeObject(bpcs.ProduccionActualPorMaquinaPorHora(produccion,tiempo));
            return JSONString;
        }
                

            [HttpGet]
            [Route("obtenerCajasPorHoraPropia/{tiempo}")]
            public dynamic obtenerCajasPorHoraPropia(int tiempo){
                string JSONString = string.Empty;
                Dictionary<string, Dictionary<string, List<int>>> produccion = new Dictionary<string, Dictionary<string, List<int>>>();
                
                if(tiempo == 1){
                    produccion = bpcs.obtenerLaProduccionActual1turno();
                }else if(tiempo == 2){
                    produccion = bpcs.obtenerLaProduccionActual2turno(true);
                }else if(tiempo == 3){
                    produccion = bpcs.obtenerLaProduccionActual2turno(false);
                }else{
                    return JSONString;
                }
                JSONString = JsonConvert.SerializeObject(produccion);
                return JSONString;
            }

            [HttpGet]
            [Route("obtenerCajasPorHoraEstandar/{tiempo}")]
            public dynamic obtenerCajasPorHoraEstandar(int tiempo){
                string JSONString = string.Empty;
                Dictionary<string, Dictionary<string, List<int>>> produccion = new Dictionary<string, Dictionary<string, List<int>>>();
                
                if(tiempo == 1){
                    produccion = bpcs.obtenerLaProduccionActual1turno();
                }else if(tiempo == 2){
                    produccion = bpcs.obtenerLaProduccionActual2turno(true);
                }else if(tiempo == 3){
                    produccion = bpcs.obtenerLaProduccionActual2turno(false);
                }else{
                    return JSONString;
                }
                JSONString = JsonConvert.SerializeObject(this.bpcs.conversionTotalAEstandarPormaquinaYproducto(produccion));
                return JSONString;
            }

            [HttpGet]
            [Route("obtenerProductosActuales")]
            public dynamic obtenerProductosActuales(){
                string JSONString = string.Empty;
                JSONString = JsonConvert.SerializeObject(bpcs.obtenerLosProductosActuales());
                return JSONString;
            }

            [HttpGet]
            [Route("obtenerProductosActualesDeLaLiena/{centroCosto}")]
            public dynamic obtenerProductosActualesDeLaLiena(string centroCosto){
                string JSONString = string.Empty;
                JSONString = JsonConvert.SerializeObject(bpcs.obtenerLosProductosActualesDeLinea(centroCosto));
                return JSONString;
            }
    }
}