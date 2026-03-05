using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ConsultasSQL.Model;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;

namespace ConsultasSQL.Logic{
    public class BPCS 
    {
        private DbIngDoc conexionIngDoc = new DbIngDoc();
        private SqlCommand CommandIngDoc = new SqlCommand();
        private SqlDataReader? DataReaderIngDoc;

        private DBconexionBPCS conexionBPCS = new DBconexionBPCS();
        private OleDbCommand CommandBPCS = new OleDbCommand();
        OleDbDataReader? DataReaderBPCS;
        private DbSIPDATABASE2 conexionSIPDATABASE2 = new DbSIPDATABASE2();
        private SqlCommand comandSIPDATABASE2 = new SqlCommand();
        private Gespline gespline = new Gespline();
        SqlDataReader? DataReaderSIPDATABASE2;

        public Dictionary<string,Dictionary<string,int>> ObjetivoPorHoraSegunProducto(int tiempo){
            Dictionary<string,Dictionary<string,int>> produccion;
            if(tiempo == 1){
                produccion = MaquinaProductosProduccionActual1turno();
            }else if(tiempo == 2){
                produccion = MaquinaProductosProduccionActual2turnoAntes0am();
            }else if(tiempo == 3){
                produccion = MaquinaProductosProduccionActual2turnoDespues0am();
            }else{
                return null;
            }
            
            string maquina;
            string producto;
            Dictionary<string,int> produccionMaquina;

            foreach (var item in produccion)
            {
                maquina = item.Key;
                produccionMaquina = item.Value;
                foreach (var productosProduccion in produccionMaquina)
                {
                    producto = productosProduccion.Key;

                    CommandIngDoc.Connection = conexionIngDoc.OpeAbrirConex();
                    CommandIngDoc.CommandText = @"
                            SELECT dbo.ObPrConver.OcObjEfic  AS [ObjEstandar] 
                            FROM [DOC_IngI].[dbo].[ObPrConver] INNER JOIN [BD_SeguimientoPlanta].[BPCS].[IIM] ON [DOC_IngI].[dbo].[ObPrConver].OcCprod = [BD_SeguimientoPlanta].[BPCS].[IIM].IPROD 
                            where dbo.ObPrConver.OcCentro = '"+ maquina +"' AND dbo.ObPrConver.OcCprod = '"+ producto +"' ORDER BY OcFecha desc";
                    DataReaderIngDoc = CommandIngDoc.ExecuteReader();

                    if(DataReaderIngDoc.Read()){
                        produccion[maquina][producto] = int.Parse(DataReaderIngDoc.GetValue(0).ToString());
                    }else{
                        produccion[maquina][producto] = -1; 
                    }
                    CommandIngDoc.Connection = conexionIngDoc.OpeCerrarConex();
                }
            }

            return produccion;
        }

        public Dictionary<string,Dictionary<string,int>> MaquinaProductosProduccionActual1turno(){
            var dataTable = new DataTable();
            Dictionary<string,Dictionary<string,int>> producción = new Dictionary<string,Dictionary<string,int>>();
            List<string> maquinas = gespline.MaquinasGesplineActivos1turno();
            Dictionary<string,int> temporal;

            foreach(string maquina in maquinas){
                temporal = new Dictionary<string,int>();
                producción.Add(maquina,temporal);
            }

            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, Sum(ITH.TQTY) AS PRODUCCION 
                        FROM X7073a51.GBYLX835F.ITH ITH 
                        WHERE (ITH.TTYPE='R') AND (ITH.TTDTE ="+ DateTime.Now.ToString("yyyyMMdd") +@") AND (ITH.TWHS='VVA ') AND (ITH.THTIME>=60000 And ITH.THTIME<180000) 
                        GROUP BY ITH.THWRKC, ITH.TPROD 
                        ORDER BY ITH.THWRKC";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();

            foreach (DataRow row in dataTable.Rows)
            {
                if (producción.ContainsKey(row["THWRKC"].ToString()))
                {
                    temporal = producción[row["THWRKC"].ToString()];
                    //var a = row["TPROD"].ToString();
                    temporal.Add(row["TPROD"].ToString(), (int) float.Parse(row["PRODUCCION"].ToString()));
                    producción[row["THWRKC"].ToString()] = temporal;
                }else{
                    continue;
                }
            }
            return producción;
        }

        public Dictionary<string,Dictionary<string,int>> MaquinaProductosProduccionActual2turnoDespues0am(){
            var dataTable = new DataTable();
            Dictionary<string,Dictionary<string,int>> producción = new Dictionary<string,Dictionary<string,int>>();
            List<string> maquinas = gespline.MaquinasGesplineActivos2turnoDespues0am();
            Dictionary<string,int> temporal;

            foreach(string maquina in maquinas){
                temporal = new Dictionary<string,int>();
                producción.Add(maquina,temporal);
            }

            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, Sum(ITH.TQTY) AS PRODUCCION
                        FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                        WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE ='"+ DateTime.Now.ToString("yyyyMMdd") + @"') AND (ITH.TWHS='VVA ') AND (ITH.THTIME>=0 And ITH.THTIME<60000))
                        GROUP BY ITH.THWRKC, ITH.TPROD
                        ORDER BY ITH.THWRKC
                        ";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);

            foreach (DataRow row in dataTable.Rows)
            {
                if (producción.ContainsKey(row["THWRKC"].ToString()))
                {
                    temporal = producción[row["THWRKC"].ToString()];
                    temporal.Add(row["TPROD"].ToString(),(int) float.Parse(row["PRODUCCION"].ToString()));
                    producción[row["THWRKC"].ToString()] = temporal;
                }else{
                    continue;
                }
            }

            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            dataTable = new DataTable();

            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, Sum(ITH.TQTY) AS PRODUCCION
                        FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                        WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE ='"+ DateTime.Now.ToString("yyyyMMdd") +@"') AND (ITH.TWHS='VVA ') AND (ITH.THTIME>=180000 And ITH.THTIME< 235959))
                        GROUP BY ITH.THWRKC, ITH.TPROD
                        ";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            
            int temporalNumero;

            foreach (DataRow row in dataTable.Rows)
            {
                if (producción.ContainsKey(row["THWRKC"].ToString()))
                {
                    temporal = producción[row["THWRKC"].ToString()];

                    if(temporal.ContainsKey(row["TPROD"].ToString())){

                        temporalNumero = temporal[row["TPROD"].ToString()];

                        temporalNumero += (int) float.Parse(row["PRODUCCION"].ToString());

                        temporal[row["TPROD"].ToString()] = temporalNumero;

                    }else{
                        temporal.Add(row["TPROD"].ToString(),(int) float.Parse(row["PRODUCCION"].ToString()));
                    }
                    producción[row["THWRKC"].ToString()] = temporal;
                }else{
                    continue;
                }
            }
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            return producción;
        }

        public Dictionary<string,Dictionary<string,int>> MaquinaProductosProduccionActual2turnoAntes0am(){
            var dataTable = new DataTable();
            Dictionary<string,Dictionary<string,int>> producción = new Dictionary<string,Dictionary<string,int>>();
            List<string> maquinas = gespline.MaquinasGesplineActivos2turnoAntes0am();
            Dictionary<string,int> temporal;

            foreach(string maquina in maquinas){
                temporal = new Dictionary<string,int>();
                producción.Add(maquina,temporal);
            }


            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, Sum(ITH.TQTY) AS PRODUCCION
                        FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                        WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE ='"+ DateTime.Now.AddDays(+1).ToString("yyyyMMdd") +@"') AND (ITH.TWHS='VVA ') AND (ITH.THTIME>=180000 And ITH.THTIME<=235959))
                        GROUP BY ITH.THWRKC, ITH.TPROD
                        ";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            
            int temporalNumero;

            foreach (DataRow row in dataTable.Rows)
            {
                if (producción.ContainsKey(row["THWRKC"].ToString()))
                {
                    temporal = producción[row["THWRKC"].ToString()];

                    if(temporal.ContainsKey(row["TPROD"].ToString())){

                        temporalNumero = temporal[row["TPROD"].ToString()];

                        temporalNumero += (int) float.Parse(row["PRODUCCION"].ToString());

                        temporal[row["TPROD"].ToString()] = temporalNumero;

                    }else{
                        temporal.Add(row["TPROD"].ToString(),(int) float.Parse(row["PRODUCCION"].ToString()));
                    }
                    producción[row["THWRKC"].ToString()] = temporal;
                }else{
                    continue;
                }
            }
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            return producción;
        }

        public Dictionary<string,Dictionary<string,int>> conversionTotalAEstandarPormaquinaYproducto(Dictionary<string,Dictionary<string,int>> produccion){
            Dictionary<string,int> productos;
            List<string> productosLlaves;
            int produccionActual;
            List<string> maquinasllaves = new List<string>(produccion.Keys);
            for (int i = 0; i < produccion.Count(); i++)
            {
                productos = produccion[maquinasllaves[i]];
                productosLlaves = new List<string>(productos.Keys);
                for (int j = 0; j < productosLlaves.Count(); j++)
                {   
                    produccionActual = productos[productosLlaves[j]];

                    CommandIngDoc.Connection = conexionIngDoc.OpeAbrirConex();
                    CommandIngDoc.CommandText = @"
                            SELECT  IIM.IMFLPF
                            FROM  [BD_SeguimientoPlanta].[BPCS].[IIM]                        
                            Where IPROD  = '" + productosLlaves[j] + "';";
                    DataReaderIngDoc = CommandIngDoc.ExecuteReader();

                    if(DataReaderIngDoc.Read()){
                        produccionActual = (int) Math.Round(produccionActual * float.Parse(DataReaderIngDoc.GetValue(0).ToString()));
                        //diccionario.Add(DataReaderIngDoc.GetValue(0).ToString(),DataReaderIngDoc.GetDecimal(3) * Decimal.Parse(DataReaderBPCS.GetValue(3).ToString()));
                    }else{
                        produccionActual = -1;
                    }
                    CommandIngDoc.Connection = conexionIngDoc.OpeCerrarConex();
                    productos[productosLlaves[j]] = produccionActual;

                }
                produccion[maquinasllaves[i]] = productos;
            }
            return produccion;
        }

        //TODO: hacer funcion de convertir produccion por hora a estandar
        public Dictionary<string,Dictionary<string,List<int>>> conversionTotalAEstandarPormaquinaYproducto(Dictionary<string,Dictionary<string,List<int>>> produccion){
            Dictionary<string,List<int>> productoHoras;
            Dictionary<string,List<int>> total = new Dictionary<string,List<int>>();
            List<string> productosLlaves;
            List<int> listaProduccionActual;
            int produccionHora;
            total.Add("Total",new List<int>() {0,0,0,0,0,0,0,0,0,0,0,0,0});
            List<string> maquinasllaves = new List<string>(produccion.Keys);
            for (int i = 0; i < produccion.Count(); i++)
            {
                productoHoras = produccion[maquinasllaves[i]];
                productosLlaves = new List<string>(productoHoras.Keys);
                for (int j = 0; j < productosLlaves.Count(); j++)
                {   
                    listaProduccionActual = productoHoras[productosLlaves[j]];

                    CommandIngDoc.Connection = conexionIngDoc.OpeAbrirConex();
                    CommandIngDoc.CommandText = @"
                            SELECT  IIM.IMFLPF
                            FROM  [BD_SeguimientoPlanta].[BPCS].[IIM]                        
                            Where IPROD  = '" + productosLlaves[j] + "';";
                    DataReaderIngDoc = CommandIngDoc.ExecuteReader();

                    if(DataReaderIngDoc.Read()){
                        for (int k = 0; k < listaProduccionActual.Count(); k++)
                        {
                            produccionHora = listaProduccionActual[k];
                            produccionHora = (int) Math.Round(produccionHora * float.Parse(DataReaderIngDoc.GetValue(0).ToString()));
                            listaProduccionActual[k] = produccionHora;
                            total["Total"][k] = total["Total"][k] + produccionHora;
                        }
                        //diccionario.Add(DataReaderIngDoc.GetValue(0).ToString(),DataReaderIngDoc.GetDecimal(3) * Decimal.Parse(DataReaderBPCS.GetValue(3).ToString()));
                    }else{
                        for (int k = 0; k < listaProduccionActual.Count(); k++)
                        {
                            produccionHora = listaProduccionActual[k];
                            produccionHora = -1;
                            listaProduccionActual[k] = produccionHora;
                        }
                    }
                    CommandIngDoc.Connection = conexionIngDoc.OpeCerrarConex();
                    productoHoras[productosLlaves[j]] = listaProduccionActual;

                }
                produccion[maquinasllaves[i]] = productoHoras;
            }
            produccion.Add("Total",total);
            return produccion;
        }

        public Dictionary<string, int> ProduccionActualPorMaquinaPorHora(Dictionary<string,Dictionary<string,int>> produccion,int Periodotiempo){
            //Dictionary<string, Dictionary<string, int>> produccion = MaquinaProductosProduccionActual1turno();
            int suma = 0;
            float resul = 0;
            Dictionary<string,int> ProduccionPorHora = new Dictionary<string,int>();
            List<string> maquinasllaves = new List<string>(produccion.Keys);
            Dictionary<string, int> cantidadSegunProductos;
            Dictionary<string, float> tiempo;

            if(Periodotiempo == 1){
                tiempo = gespline.tiempoTrabajadoActual1turno();
            }else if(Periodotiempo == 2){
                tiempo = gespline.tiempoTrabajadoActual2turno(true);
            }else if(Periodotiempo == 3){
                tiempo = gespline.tiempoTrabajadoActual2turno(false);
            }else{
                return null;
            }

            for (int i = 0; i < maquinasllaves.Count(); i++)
            {
                cantidadSegunProductos = produccion[maquinasllaves[i]];
                foreach (var item in cantidadSegunProductos)
                {
                    suma += item.Value;
                }
                try{
                    ProduccionPorHora.Add(maquinasllaves[i],(int) Math.Round(suma/tiempo[maquinasllaves[i]]));
                }catch{

                }
                suma = 0;
            }
            return ProduccionPorHora;
        }

        public Dictionary<string, Dictionary<string,List<int>>> obtenerLaProduccionActual1turno(){
            List<string> maquinas = gespline.MaquinasGesplineActivos1turno();
            Dictionary<string, Dictionary<string,List<int>>> producionMaquinaPorHora = new Dictionary<string,Dictionary<string,List<int>>>(); 
            Dictionary<string,List<int>> listaProSuma;
            List<int> listaSuma;
            var dataTable = new DataTable();

            foreach (var item in maquinas)
            {
                listaProSuma = new Dictionary<string,List<int>>();
                producionMaquinaPorHora.Add(item,listaProSuma);
            }

            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, IIM.IDESC, ITH.TQTY, ITH.THTIME
                        FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                        WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE =" + DateTime.Now.ToString("yyyyMMdd") + @") AND (ITH.TWHS='VVA ') AND (ITH.THTIME >=60000 And ITH.THTIME<=180000))
                        ORDER BY ITH.THWRKC";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);

            string maquina;
            string producto;
            int hora;
            foreach (DataRow row in dataTable.Rows)
            {
                maquina = row["THWRKC"].ToString();
                if(!producionMaquinaPorHora.ContainsKey(maquina)){
                    listaProSuma = new Dictionary<string,List<int>>();
                    producionMaquinaPorHora.Add(maquina,listaProSuma);
                }
                    listaProSuma = producionMaquinaPorHora[maquina];
                    producto = row["TPROD"].ToString();
                    if(listaProSuma.ContainsKey(producto)){
                        listaSuma = listaProSuma[producto];
                    }else{
                        listaSuma = new List<int>() {0,0,0,0,0,0,0,0,0,0,0,0,0};
                        listaProSuma.Add(producto,listaSuma);
                    }

                    hora = int.Parse(row["THTIME"].ToString());
                    if (hora >= 60000 && hora < 70000){
                        listaSuma[0] = listaSuma[0] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 80000){
                        listaSuma[1] = listaSuma[1] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 90000){
                        listaSuma[2] = listaSuma[2] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 100000){
                        listaSuma[3] = listaSuma[3] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 110000){
                        listaSuma[4] = listaSuma[4] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 120000){
                        listaSuma[5] = listaSuma[5] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 130000){
                        listaSuma[6] = listaSuma[6] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 140000){
                        listaSuma[7] = listaSuma[7] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 150000){
                        listaSuma[8] = listaSuma[8] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 160000){
                        listaSuma[9] = listaSuma[9] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 170000){
                        listaSuma[10] = listaSuma[10] + int.Parse(row["TQTY"].ToString());
                    }else if(hora <= 180000){
                        listaSuma[11] = listaSuma[11] + int.Parse(row["TQTY"].ToString());
                    }
                    listaSuma[12] = listaSuma[12] + int.Parse(row["TQTY"].ToString());
            }
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            return producionMaquinaPorHora;
        }

        private DataTable obtenerLaProduccionActual2turnoAntes0am(bool band){
            Dictionary<string, Dictionary<string,List<int>>> producionMaquinaPorHora = new Dictionary<string,Dictionary<string,List<int>>>();
            var dataTable = new DataTable();
            //* si band es true estamos a las 6pm a 11:59 am
            if(band){
                CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
                CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, IIM.IDESC, ITH.TQTY, ITH.THTIME
                        FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                        WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE>='"+ DateTime.Now.AddDays(+1).ToString("yyyyMMdd") + @"') AND (ITH.TWHS='VVA ') AND (ITH.THTIME>=180000 And ITH.THTIME < 240000))
                        ORDER BY ITH.THWRKC";
                DataReaderBPCS = CommandBPCS.ExecuteReader();
            //* si band es falso estamos de 11:59 am hasta 6 am
            }else{
                CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
                CommandBPCS.CommandText = @"
                        SELECT ITH.THWRKC, ITH.TPROD, IIM.IDESC, ITH.TQTY, ITH.THTIME
                        FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                        WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE>='"+ DateTime.Now.ToString("yyyyMMdd") + @"') AND (ITH.TWHS='VVA ') AND (ITH.THTIME>=180000 And ITH.THTIME < 240000))
                        ORDER BY ITH.THWRKC";
                DataReaderBPCS = CommandBPCS.ExecuteReader();
            }
            dataTable.Load(DataReaderBPCS); 
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            return dataTable;
        }

        public Dictionary<string, string> obtenerLosProductosActuales(){
            var dataTable = new DataTable();
            Dictionary<string, string> productos = new Dictionary<string, string>();
            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                SELECT ITH.TPROD, IIM.IDESC, Sum(ITH.TQTY)
                FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTDTE="+ DateTime.Now.ToString("yyyyMMdd") + @") AND (ITH.TTYPE='R') AND (ITH.TWHS='VVA'))
                GROUP BY ITH.TPROD, IIM.IDESC";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            foreach (DataRow row in dataTable.Rows)
            {
                productos.Add(row["TPROD"].ToString(),row["IDESC"].ToString());
            }
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            
            return productos;
        }

        public Dictionary<string, string> obtenerLosProductosActualesDeLinea(string centroCosto){
            var dataTable = new DataTable();
            Dictionary<string, string> productos = new Dictionary<string, string>();

            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                SELECT ITH.THWRKC, ITH.TPROD, IIM.IDESC
                FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTDTE='"+ DateTime.Now.ToString("yyyyMMdd") + @"') AND (ITH.TTYPE='R') AND (ITH.THWRKC='"+ centroCosto + @"'))
                GROUP BY ITH.THWRKC, ITH.TPROD, IIM.IDESC";
                // AND (ITH.TWHS='VVA') 
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            foreach (DataRow row in dataTable.Rows)
            {
                productos.Add(row["TPROD"].ToString(),row["IDESC"].ToString());
            }
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();

            return productos;
        }

        private DataTable obtenerLaProduccionActual2turnoDespues0am(){
            Dictionary<string, Dictionary<string,List<int>>> producionMaquinaPorHora = new Dictionary<string,Dictionary<string,List<int>>>();
            var dataTable = new DataTable();

            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                SELECT ITH.THWRKC, ITH.TPROD, IIM.IDESC, ITH.TQTY, ITH.THTIME
                FROM X7073a51.GBYLX835F.IIM IIM, X7073a51.GBYLX835F.ITH ITH
                WHERE ITH.TPROD = IIM.IPROD AND ((ITH.TTYPE='R') AND (ITH.TTDTE>='"+ DateTime.Now.ToString("yyyyMMdd") + @"') AND (ITH.TWHS='VVA ') AND (ITH.THTIME >= 0 And ITH.THTIME < 60000))
                ORDER BY ITH.THWRKC";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            return dataTable;
        }
        public Dictionary<string, Dictionary<string,List<int>>> obtenerLaProduccionActual2turno(bool band){
            Dictionary<string, Dictionary<string,List<int>>> producionMaquinaPorHora = new Dictionary<string,Dictionary<string,List<int>>>(); 
            Dictionary<string,List<int>> listaProSuma;
            List<int> listaSuma;
            var dataTable = new DataTable();
            var dataTable2 = new DataTable();

            List<string> maquinas = new List<string>();
            //* si band es true estamos a las 6pm a 11:59 am
            if(band){
                maquinas = gespline.MaquinasGesplineActivos2turnoAntes0am();
                dataTable = this.obtenerLaProduccionActual2turnoAntes0am(band);
            //* si band es true estamos a las 11:59 pm a 6 am
            }else{
                maquinas = gespline.MaquinasGesplineActivos2turnoDespues0am();
                dataTable = this.obtenerLaProduccionActual2turnoAntes0am(band);
                dataTable2 = this.obtenerLaProduccionActual2turnoDespues0am();
            }

            foreach (var item in maquinas)
            {
                listaProSuma = new Dictionary<string,List<int>>();
                producionMaquinaPorHora.Add(item,listaProSuma);
            }


            string maquina;
            string producto;
            int hora;
            foreach (DataRow row in dataTable.Rows)
            {
                maquina = row["THWRKC"].ToString();
                if(!producionMaquinaPorHora.ContainsKey(maquina)){
                    listaProSuma = new Dictionary<string,List<int>>();
                    producionMaquinaPorHora.Add(maquina,listaProSuma);
                }
                    listaProSuma = producionMaquinaPorHora[maquina];
                    producto = row["TPROD"].ToString();
                    if(listaProSuma.ContainsKey(producto)){
                        listaSuma = listaProSuma[producto];
                    }else{
                        listaSuma = new List<int>() {0,0,0,0,0,0,0,0,0,0,0,0,0};
                        listaProSuma.Add(producto,listaSuma);
                    }

                    hora = int.Parse(row["THTIME"].ToString());
                    if (hora >= 180000 && hora < 190000){
                        listaSuma[0] = listaSuma[0] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 200000){
                        listaSuma[1] = listaSuma[1] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 210000){
                        listaSuma[2] = listaSuma[2] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 220000){
                        listaSuma[3] = listaSuma[3] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 230000){
                        listaSuma[4] = listaSuma[4] + int.Parse(row["TQTY"].ToString());
                    }else if(hora < 240000){
                        listaSuma[5] = listaSuma[5] + int.Parse(row["TQTY"].ToString());
                    }
                    listaSuma[12] = listaSuma[12] + int.Parse(row["TQTY"].ToString());
                
            }
            if(!band){
                foreach (DataRow row in dataTable2.Rows)
                {
                    maquina = row["THWRKC"].ToString();
                    if(producionMaquinaPorHora.ContainsKey(maquina)){
                        listaProSuma = producionMaquinaPorHora[maquina];
                        producto = row["TPROD"].ToString();
                        if(listaProSuma.ContainsKey(producto)){
                            listaSuma = listaProSuma[producto];
                        }else{
                            listaSuma = new List<int>() {0,0,0,0,0,0,0,0,0,0,0,0,0};
                            listaProSuma.Add(producto,listaSuma);
                        }

                        hora = int.Parse(row["THTIME"].ToString());
                        if(hora > 0 && hora < 10000){
                            listaSuma[6] = listaSuma[6] + int.Parse(row["TQTY"].ToString());
                        }else if(hora < 20000){
                            listaSuma[7] = listaSuma[7] + int.Parse(row["TQTY"].ToString());
                        }else if(hora < 30000){
                            listaSuma[8] = listaSuma[8] + int.Parse(row["TQTY"].ToString());
                        }else if(hora < 40000){
                            listaSuma[9] = listaSuma[9] + int.Parse(row["TQTY"].ToString());
                        }else if(hora < 50000){
                            listaSuma[10] = listaSuma[10] + int.Parse(row["TQTY"].ToString());
                        }else if(hora <= 60000){
                            listaSuma[11] = listaSuma[11] + int.Parse(row["TQTY"].ToString());
                        }
                        listaSuma[12] = listaSuma[12] + int.Parse(row["TQTY"].ToString());
                    }else{
                        continue;
                    }
                }
            }
            return producionMaquinaPorHora;
        }
        
    }
}