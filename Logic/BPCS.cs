using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ConsultasSQL.Model;
using System.Data.Odbc;
using System.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConsultasSQL.Logic
{
    public class BPCS
    {
        // --------- Dependencias ---------
        private readonly IBpcsConnectionFactory _factory;
        private readonly DbIngDoc conexionIngDoc = new DbIngDoc(); // (SQL Server)
        private readonly Gespline gespline = new Gespline();

        public BPCS(IBpcsConnectionFactory factory)
        {
            _factory = factory;
        }

        // --------------------------------------------------------------------
        //  OBJETIVO POR HORA SEGÚN PRODUCTO (usa SQL Server) - PARAMETRIZADO
        // --------------------------------------------------------------------
        public Dictionary<string, Dictionary<string, int>> ObjetivoPorHoraSegunProducto(int tiempo)
        {
            Dictionary<string, Dictionary<string, int>> produccion;
            if (tiempo == 1)
            {
                produccion = MaquinaProductosProduccionActual1turno();
            }
            else if (tiempo == 2)
            {
                produccion = MaquinaProductosProduccionActual2turnoAntes0am();
            }
            else if (tiempo == 3)
            {
                produccion = MaquinaProductosProduccionActual2turnoDespues0am();
            }
            else
            {
                return null!;
            }

            foreach (var kvpMaq in produccion.ToList())
            {
                var maquina = kvpMaq.Key;
                var produccionMaquina = kvpMaq.Value;

                foreach (var prodKey in produccionMaquina.Keys.ToList())
                {
                    // SQL Server parametrizado
                    using var sqlConn = conexionIngDoc.OpeAbrirConex();
                    using var cmd = new SqlCommand(@"
                        SELECT TOP (1) dbo.ObPrConver.OcObjEfic AS [ObjEstandar]
                        FROM [DOC_IngI].[dbo].[ObPrConver]
                        INNER JOIN [BD_SeguimientoPlanta].[BPCS].[IIM] 
                            ON [DOC_IngI].[dbo].[ObPrConver].OcCprod = [BD_SeguimientoPlanta].[BPCS].[IIM].IPROD
                        WHERE dbo.ObPrConver.OcCentro = @maquina
                          AND dbo.ObPrConver.OcCprod  = @producto
                        ORDER BY OcFecha DESC", sqlConn);

                    cmd.Parameters.Add(new SqlParameter("@maquina", System.Data.SqlDbType.VarChar, 20) { Value = maquina });
                    cmd.Parameters.Add(new SqlParameter("@producto", System.Data.SqlDbType.VarChar, 50) { Value = prodKey });

                    var obj = cmd.ExecuteScalar();
                    conexionIngDoc.OpeCerrarConex();

                    if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var objetivo))
                        produccion[maquina][prodKey] = objetivo;
                    else
                        produccion[maquina][prodKey] = -1;
                }
            }

            return produccion;
        }

        // --------------------------------------------------------------------
        //  1er TURNO: 06:00:00–17:59:59
        // --------------------------------------------------------------------
        public Dictionary<string, Dictionary<string, int>> MaquinaProductosProduccionActual1turno()
        {
            var maquinasActivas = gespline.MaquinasGesplineActivos1turno() ?? new List<string>();
            var maquinasSet = new HashSet<string>(maquinasActivas.Select(m => (m ?? string.Empty).Trim()),
                                                  StringComparer.OrdinalIgnoreCase);

            var produccion = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

            int fechaInt = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
            string whs = "VVA";       // comparar con RTRIM(TWHS)
            int timeStart = 60000;    // 06:00:00
            int timeEnd = 180000;     // 18:00:00 (exclusivo)

            string sql = @"
                SELECT 
                    RTRIM(ITH.THWRKC) AS THWRKC,
                    RTRIM(ITH.TPROD)  AS TPROD,
                    SUM(ITH.TQTY)     AS PRODUCCION
                FROM GBYLX835F/ITH ITH
                WHERE ITH.TTYPE = 'R'
                AND ITH.TTDTE = ?
                AND RTRIM(ITH.TWHS) = ?
                AND ITH.THTIME >= ? AND ITH.THTIME < ?
                GROUP BY RTRIM(ITH.THWRKC), RTRIM(ITH.TPROD)
                ORDER BY RTRIM(ITH.THWRKC)
            ";

            using var conn = _factory.CreateOpen();
            using var cmd = new OdbcCommand(sql, conn);
            cmd.CommandTimeout = 120;

            cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaInt;
            cmd.Parameters.Add("TWHS", OdbcType.VarChar, 10).Value = whs;
            cmd.Parameters.Add("TINI", OdbcType.Int).Value = timeStart;
            cmd.Parameters.Add("TFIN", OdbcType.Int).Value = timeEnd;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var wrkc = (reader["THWRKC"] as string)?.Trim() ?? string.Empty;
                var prod = (reader["TPROD"] as string)?.Trim() ?? string.Empty;
                int qty = 0;

                if (reader["PRODUCCION"] != DBNull.Value)
                {
                    var dec = Convert.ToDecimal(reader["PRODUCCION"], CultureInfo.InvariantCulture);
                    qty = Convert.ToInt32(Math.Round(dec, MidpointRounding.AwayFromZero));
                }

                if (!maquinasSet.Contains(wrkc)) continue;

                if (!produccion.TryGetValue(wrkc, out var dict))
                    produccion[wrkc] = dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                dict[prod] = qty;
            }

            foreach (var m in maquinasSet)
                if (!produccion.ContainsKey(m))
                    produccion[m] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            return produccion;
        }

        // --------------------------------------------------------------------
        //  2do TURNO (después de 0 am): combinas 00:00–05:59 del día actual + 18:00–23:59 del día actual
        //  (Mantenemos la misma lógica que tenías: dos consultas y sumas)
        // --------------------------------------------------------------------
        public Dictionary<string, Dictionary<string, int>> MaquinaProductosProduccionActual2turnoDespues0am()
        {
            var produccion = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var maquinas = gespline.MaquinasGesplineActivos2turnoDespues0am() ?? new List<string>();
            foreach (var m in maquinas) produccion[(m ?? string.Empty).Trim()] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int fechaHoy = int.Parse(DateTime.Today.ToString("yyyyMMdd"), CultureInfo.InvariantCulture);
            const string whs = "VVA";
            const int timeStart = 0;
            const int timeEnd = 60000;

            string sql = @"
                SELECT 
                    RTRIM(ITH.THWRKC) AS THWRKC, 
                    RTRIM(ITH.TPROD) AS TPROD, 
                    SUM(ITH.TQTY) PRODUCCION
                FROM GBYLX835F/ITH ITH
                WHERE ITH.TTYPE='R'
                    AND ITH.TTDTE = ?
                    AND RTRIM(ITH.TWHS) = ?
                    AND ITH.THTIME >= ? AND ITH.THTIME < ?
                GROUP BY RTRIM(ITH.THWRKC), RTRIM(ITH.TPROD)
                ORDER BY RTRIM(ITH.THWRKC)";

            using var conn = _factory.CreateOpen();
            using var cmd = new OdbcCommand(sql, conn);
            cmd.CommandTimeout = 120;

            cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaHoy;
            cmd.Parameters.Add("TWHS", OdbcType.VarChar, 10).Value = whs;
            cmd.Parameters.Add("TINI", OdbcType.Int).Value = timeStart;
            cmd.Parameters.Add("TFIN", OdbcType.Int).Value = timeEnd;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var wrkc = (r["THWRKC"] as string)?.Trim() ?? string.Empty;
                var prod = (r["TPROD"] as string)?.Trim() ?? string.Empty;
                
                if(!produccion.TryGetValue(wrkc, out var dict))
                    continue;
                
                int qty = 0;
                if(r["PRODUCCION"] != DBNull.Value)
                {
                    var dec = Convert.ToDecimal(r["PRODUCCION"], CultureInfo.InvariantCulture);
                    qty = Convert.ToInt32(Math.Round(dec, MidpointRounding.AwayFromZero));
                }                
                
                if (qty == 0) continue;

                dict[prod] = dict.ContainsKey(prod) ? dict[prod] + qty : qty;
                }           
        return produccion;
    }

        // --------------------------------------------------------------------
        //  2do TURNO (antes de 0 am): 18:00–23:59 pero usando la FECHA +1 (como en tu lógica original)
        // --------------------------------------------------------------------
        public Dictionary<string, Dictionary<string, int>> MaquinaProductosProduccionActual2turnoAntes0am()
        {
            var produccion = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var maquinas = gespline.MaquinasGesplineActivos2turnoAntes0am() ?? new List<string>();
            foreach (var m in maquinas) produccion[(m ?? string.Empty).Trim()] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int fechaHoy = int.Parse(DateTime.Today.ToString("yyyyMMdd"), CultureInfo.InvariantCulture);
            //int fechaAyer = int.Parse(DateTime.Today.AddDays(-1).ToString("yyyyMMdd"), CultureInfo.InvariantCulture);
            const string whs = "VVA";
            const int timeStart = 180000;
            const int timeEnd = 240000;

            string sql = @"
                SELECT 
                    RTRIM(ITH.THWRKC) AS THWRKC, 
                    RTRIM(ITH.TPROD) AS TPROD, 
                    SUM(ITH.TQTY) AS PRODUCCION
                FROM GBYLX835F/ITH ITH
                WHERE ITH.TTYPE='R'
                    AND ITH.TTDTE = ?
                    AND RTRIM(ITH.TWHS) = ?
                    AND ITH.THTIME >= ? AND ITH.THTIME < ?
                GROUP BY RTRIM(ITH.THWRKC), RTRIM(ITH.TPROD)
                ORDER BY RTRIM(ITH.THWRKC)";

            using var conn = _factory.CreateOpen();
            using var cmd = new OdbcCommand(sql, conn);
            cmd.CommandTimeout = 120;

            cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaHoy;
            cmd.Parameters.Add("TWHS", OdbcType.VarChar, 10).Value = whs;
            cmd.Parameters.Add("TINI", OdbcType.Int).Value = timeStart;
            cmd.Parameters.Add("TFIN", OdbcType.Int).Value = timeEnd;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var wrkc = (r["THWRKC"] as string)?.Trim() ?? string.Empty;
                var prod = (r["TPROD"] as string)?.Trim() ?? string.Empty;

                if(!produccion.TryGetValue(wrkc, out var dict))
                    continue;
                
                int qty = 0;
                if(r["PRODUCCION"] != DBNull.Value)
                {
                    var dec = Convert.ToDecimal(r["PRODUCCION"], CultureInfo.InvariantCulture);
                    qty = Convert.ToInt32(Math.Round(dec, MidpointRounding.AwayFromZero));
                }

                if (qty == 0) continue;

                dict[prod] = dict.ContainsKey(prod) ? dict[prod] + qty : qty;
            }
            return produccion;
        }

        // --------------------------------------------------------------------
        //  CONVERSIÓN A ESTÁNDAR (usa SQL Server para factor IIM.IMFLPF) - PARAMETRIZADO
        // --------------------------------------------------------------------
    public Dictionary<string, Dictionary<string, int>> conversionTotalAEstandarPormaquinaYproducto(
        Dictionary<string, Dictionary<string, int>> produccion)
    {
        // Longitud típica de IIM.IPROD en AS400; ajusta si es distinta (ej. 15)
        const int LenIPROD = 15;

        foreach (var maq in produccion.Keys.ToList())
        {
            var productos = produccion[maq];

            foreach (var prod in productos.Keys.ToList())
            {
                var actual = productos[prod];

                using var conn = _factory.CreateOpen();  // ODBC a AS400
                using var cmd = new OdbcCommand(@"
                    SELECT IMFLPF
                    FROM GBYLX835F/IIM
                    WHERE RTRIM(IPROD) = ?", conn);

                // Opción A: VARCHar sin pad, ya que usamos RTRIM en la columna
                cmd.Parameters.Add("IPROD", OdbcType.VarChar, LenIPROD).Value = (prod ?? string.Empty).Trim();

                var obj = cmd.ExecuteScalar();

                // Si no encontró fila, ponemos -1
                if (obj == null || obj == DBNull.Value)
                {
                    productos[prod] = -1;
                    continue;
                }

                // Normalizamos el factor y parseamos como DECIMAL
                decimal factor;
                {
                    string raw = obj.ToString()?.Trim() ?? "";

                    raw = raw.Replace(',', '.');                 // coma → punto
                    if (raw.StartsWith(".")) raw = "0" + raw;    // .500 → 0.500

                    if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out factor))
                    {
                        productos[prod] = -1;
                        continue;
                    }
                }

                if (factor > 0m)
                    productos[prod] = (int)Math.Round(actual * factor, MidpointRounding.AwayFromZero);
                else
                    productos[prod] = -1;
            }
        }

        return produccion;
    }

        // --------------------------------------------------------------------
        //  CONVERSIÓN A ESTÁNDAR versión List<int> por horas (SQL Server) - PARAMETRIZADO
        // --------------------------------------------------------------------

    public Dictionary<string, Dictionary<string, List<int>>> conversionTotalAEstandarPormaquinaYproducto(
        Dictionary<string, Dictionary<string, List<int>>> produccion)
    {
        const int LenIPROD = 15;

        var total = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Total"] = Enumerable.Repeat(0, 13).ToList()
        };

        foreach (var maq in produccion.Keys.ToList())
        {
            var productos = produccion[maq];

            foreach (var prod in productos.Keys.ToList())
            {
                var lista = productos[prod];

                using var conn = _factory.CreateOpen(); // ODBC a AS400
                using var cmd = new OdbcCommand(@"
                    SELECT IMFLPF
                    FROM GBYLX835F/IIM
                    WHERE RTRIM(IPROD) = ?", conn);

                cmd.Parameters.Add("IPROD", OdbcType.VarChar, LenIPROD).Value = (prod ?? string.Empty).Trim();

                var obj = cmd.ExecuteScalar();

                if (obj == null || obj == DBNull.Value)
                {
                    for (int k = 0; k < lista.Count; k++) lista[k] = -1;
                    productos[prod] = lista;
                    continue;
                }

                decimal factor;
                {
                    string raw = obj.ToString()?.Trim() ?? "";
                    raw = raw.Replace(',', '.');
                    if (raw.StartsWith(".")) raw = "0" + raw;

                    if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out factor))
                    {
                        for (int k = 0; k < lista.Count; k++) lista[k] = -1;
                        productos[prod] = lista;
                        continue;
                    }
                }

                if (factor > 0m)
                {
                    for (int k = 0; k < lista.Count; k++)
                    {
                        var v = (int)Math.Round(lista[k] * factor, MidpointRounding.AwayFromZero);
                        lista[k] = v;
                        total["Total"][k] += v;
                    }
                }
                else
                {
                    for (int k = 0; k < lista.Count; k++) lista[k] = -1;
                }

                productos[prod] = lista;
            }
        }

        if (produccion.ContainsKey("Total"))
            produccion["Total"] = total;
        else
            produccion.Add("Total", total);

        return produccion;
    }

        // --------------------------------------------------------------------
        //  PRODUCCIÓN POR HORA (divide por horas trabajadas de Gespline)
        // --------------------------------------------------------------------
        public Dictionary<string, int> ProduccionActualPorMaquinaPorHora(
            Dictionary<string, Dictionary<string, int>> produccion,
            int Periodotiempo)
        {
            Dictionary<string, float> tiempo;
            if (Periodotiempo == 1)
                tiempo = gespline.tiempoTrabajadoActual1turno();
            else if (Periodotiempo == 2)
                tiempo = gespline.tiempoTrabajadoActual2turno(true);
            else if (Periodotiempo == 3)
                tiempo = gespline.tiempoTrabajadoActual2turno(false);
            else
                return null!;

            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var maq in produccion.Keys)
            {
                int suma = produccion[maq].Values.Sum();
                if (tiempo.TryGetValue(maq, out var horas) && horas > 0)
                    result[maq] = (int)Math.Round(suma / horas);
            }
            return result;
        }

        // --------------------------------------------------------------------
        //  PRODUCCIÓN HORA a HORA: 1er turno (06:00–18:00)
            // --------------------------------------------------------------------
    public Dictionary<string, Dictionary<string, List<int>>> obtenerLaProduccionActual1turno()
    {
        // 1) Construcción del diccionario base con todas las máquinas activas
        var maquinas = gespline.MaquinasGesplineActivos1turno() ?? new List<string>();
        var prodMaqHora = new Dictionary<string, Dictionary<string, List<int>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in maquinas)
            prodMaqHora[(m ?? string.Empty).Trim()] = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        // 2) Parámetros
        int fechaInt = int.Parse(DateTime.Today.ToString("yyyyMMdd"), CultureInfo.InvariantCulture);
        const int timeStart = 60000;   // 06:00:00  → inclusivo
        const int timeEnd   = 180000;  // 18:00:00  → exclusivo
        const string whs    = "VVA";   // almacén

        // 3) Consulta ODBC a AS400, todo parametrizado
        string sql = @"
            SELECT 
                RTRIM(ITH.THWRKC) AS THWRKC,
                RTRIM(ITH.TPROD)  AS TPROD,
                ITH.TQTY          AS TQTY,
                ITH.THTIME        AS THTIME
            FROM GBYLX835F/ITH ITH
            WHERE ITH.TTYPE = 'R'
            AND ITH.TTDTE = ?
            AND ITH.THTIME >= ? AND ITH.THTIME < ?
            AND RTRIM(ITH.TWHS) = ?
            ORDER BY ITH.THWRKC, ITH.THTIME";

        using var conn = _factory.CreateOpen();             // ODBC a AS400
        using var cmd  = new OdbcCommand(sql, conn);
        cmd.CommandTimeout = 120;

        cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaInt;
        cmd.Parameters.Add("TINI",  OdbcType.Int).Value = timeStart;
        cmd.Parameters.Add("TFIN",  OdbcType.Int).Value = timeEnd;      // fin exclusivo
        cmd.Parameters.Add("TWHS",  OdbcType.VarChar, 10).Value = whs;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var maquina = (reader["THWRKC"] as string)?.Trim() ?? string.Empty;
            var prod    = (reader["TPROD"]  as string)?.Trim() ?? string.Empty;

            // TQTY en ITH puede ser DEC(15,5) en algunos AS400: convierto con decimal y luego a int
            int qty = 0;
            if (reader["TQTY"] != DBNull.Value)
            {
                var decQty = Convert.ToDecimal(reader["TQTY"], CultureInfo.InvariantCulture);
                qty = Convert.ToInt32(Math.Round(decQty, MidpointRounding.AwayFromZero));
            }

            int hora = 0;
            if (reader["THTIME"] != DBNull.Value)
                hora = Convert.ToInt32(reader["THTIME"], CultureInfo.InvariantCulture);

            if (!prodMaqHora.TryGetValue(maquina, out var dicProd))
            {
                dicProd = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                prodMaqHora[maquina] = dicProd;
            }

            if (!dicProd.TryGetValue(prod, out var lista))
            {
                lista = Enumerable.Repeat(0, 13).ToList(); // [12 horas + total]
                dicProd[prod] = lista;
            }

            // 4) Buckets de 1 hora desde 06:00 hasta 18:00 (18:00 es fin EXCLUSIVO)
            // 06:00-06:59 → idx 0; 07:00-07:59 → idx 1; ...; 17:00-17:59 → idx 11
            int idx = -1;
            if (hora >= 60000 && hora < 70000) idx = 0;
            else if (hora < 80000)  idx = 1;
            else if (hora < 90000)  idx = 2;
            else if (hora < 100000) idx = 3;
            else if (hora < 110000) idx = 4;
            else if (hora < 120000) idx = 5;
            else if (hora < 130000) idx = 6;
            else if (hora < 140000) idx = 7;
            else if (hora < 150000) idx = 8;
            else if (hora < 160000) idx = 9;
            else if (hora < 170000) idx = 10;
            else if (hora < 180000) idx = 11; // fin exclusivo

            if (idx >= 0)
            {
                lista[idx]  += qty;
                lista[12]   += qty;  // total
            }
        }

        // 5) Asegurar que todas las máquinas queden presentes (aunque vacías)
        foreach (var m in maquinas.Select(x => (x ?? string.Empty).Trim()))
            if (!prodMaqHora.ContainsKey(m))
                prodMaqHora[m] = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        return prodMaqHora;
    }

        // Helpers ODBC para 2do turno (devuelven DataTable como en tu código original)
        private DataTable obtenerLaProduccionActual2turnoAntes0am(bool band)
        {
            var table = new DataTable();
            using var conn = _factory.CreateOpen();

            if (band)
            {
                // 18:00–23:59 con fecha +1 (tu lógica original)
                int fechaManiana = int.Parse(DateTime.Today.AddDays(+1).ToString("yyyyMMdd"));
                string sql = @"
                    SELECT RTRIM(ITH.THWRKC) THWRKC, RTRIM(ITH.TPROD) TPROD, ITH.TQTY, ITH.THTIME
                    FROM GBYLX835F/ITH ITH
                    WHERE ITH.TTYPE='R'
                        AND ITH.TTDTE >= ?
                        AND RTRIM(ITH.TWHS)='VVA'
                        AND ITH.THTIME >= 180000 AND ITH.THTIME < 240000
                    ORDER BY ITH.THWRKC";
                using var cmd = new OdbcCommand(sql, conn);
                cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaManiana;
                using var r = cmd.ExecuteReader();
                table.Load(r);
            }
            else
            {
                int fechaHoy = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
                string sql = @"
                    SELECT RTRIM(ITH.THWRKC) THWRKC, RTRIM(ITH.TPROD) TPROD, ITH.TQTY, ITH.THTIME
                    FROM GBYLX835F/ITH ITH
                    WHERE ITH.TTYPE='R'
                        AND ITH.TTDTE >= ?
                        AND RTRIM(ITH.TWHS)='VVA'
                        AND ITH.THTIME >= 180000 AND ITH.THTIME < 240000
                    ORDER BY ITH.THWRKC";
                using var cmd = new OdbcCommand(sql, conn);
                cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaHoy;
                using var r = cmd.ExecuteReader();
                table.Load(r);
            }
            return table;
        }

        private DataTable obtenerLaProduccionActual2turnoDespues0am()
        {
            var table = new DataTable();
            using var conn = _factory.CreateOpen();

            int fechaHoy = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
            string sql = @"
                SELECT RTRIM(ITH.THWRKC) THWRKC, RTRIM(ITH.TPROD) TPROD, ITH.TQTY, ITH.THTIME
                FROM GBYLX835F/ITH ITH
                WHERE ITH.TTYPE='R'
                    AND ITH.TTDTE >= ?
                    AND RTRIM(ITH.TWHS)='VVA'
                    AND ITH.THTIME >= 0 AND ITH.THTIME < 60000
                ORDER BY ITH.THWRKC";
            using var cmd = new OdbcCommand(sql, conn);
            cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaHoy;
            using var r = cmd.ExecuteReader();
            table.Load(r);

            return table;
        }

        // --------------------------------------------------------------------
        //  Composición de producción hora a hora para 2do turno (igual a tu lógica)
        // --------------------------------------------------------------------
        public Dictionary<string, Dictionary<string, List<int>>> obtenerLaProduccionActual2turno(bool band)
        {
            var prodMaqHora = new Dictionary<string, Dictionary<string, List<int>>>(StringComparer.OrdinalIgnoreCase);

            var maquinas = band
                ? gespline.MaquinasGesplineActivos2turnoAntes0am()
                : gespline.MaquinasGesplineActivos2turnoDespues0am();

            foreach (var m in (maquinas ?? new List<string>()))
                prodMaqHora[(m ?? string.Empty).Trim()] = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            var tablaA = this.obtenerLaProduccionActual2turnoAntes0am(band);
            var tablaB = band ? null : this.obtenerLaProduccionActual2turnoDespues0am();

            void acumular(DataTable dt, bool tramoNoche)
            {
                foreach (DataRow row in dt.Rows)
                {
                    var maquina = row["THWRKC"].ToString() ?? string.Empty;
                    var prod = row["TPROD"].ToString() ?? string.Empty;
                    var qty = int.Parse(row["TQTY"].ToString() ?? "0", CultureInfo.InvariantCulture);
                    var hora = int.Parse(row["THTIME"].ToString() ?? "0", CultureInfo.InvariantCulture);

                    if (!prodMaqHora.TryGetValue(maquina, out var dicProd))
                    {
                        dicProd = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                        prodMaqHora[maquina] = dicProd;
                    }

                    if (!dicProd.TryGetValue(prod, out var lista))
                    {
                        lista = Enumerable.Repeat(0, 13).ToList();
                        dicProd[prod] = lista;
                    }

                    if (tramoNoche)
                    {
                        // 18:00–23:59 => slots 0–5
                        if (hora >= 180000 && hora < 190000) lista[0] += qty;
                        else if (hora < 200000) lista[1] += qty;
                        else if (hora < 210000) lista[2] += qty;
                        else if (hora < 220000) lista[3] += qty;
                        else if (hora < 230000) lista[4] += qty;
                        else if (hora < 240000) lista[5] += qty;
                    }
                    else
                    {
                        // 00:00–05:59 => slots 6–11
                        if (hora > 0 && hora < 10000) lista[6] += qty;
                        else if (hora < 20000) lista[7] += qty;
                        else if (hora < 30000) lista[8] += qty;
                        else if (hora < 40000) lista[9] += qty;
                        else if (hora < 50000) lista[10] += qty;
                        else if (hora <= 60000) lista[11] += qty;
                    }

                    lista[12] += qty; // total
                }
            }

            // tramo noche (18–24)
            acumular(tablaA, true);

            // tramo madrugada (0–6)
            if (tablaB != null)
                acumular(tablaB, false);

            return prodMaqHora;
        }

        // --------------------------------------------------------------------
        //  Productos actuales (todo el día) - ODBC, parametrizado
        // --------------------------------------------------------------------
    public Dictionary<string, string> obtenerLosProductosActuales()
    {
        var productos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int fechaInt = int.Parse(DateTime.Today.ToString("yyyyMMdd"), CultureInfo.InvariantCulture);
        const string whs = "VVA";

        string sql = @"
            SELECT 
                RTRIM(ITH.TPROD) AS TPROD,
                RTRIM(IIM.IDESC) AS IDESC,
                SUM(ITH.TQTY)    AS SUMQ
            FROM GBYLX835F/ITH ITH
            INNER JOIN GBYLX835F/IIM IIM
                ON RTRIM(ITH.TPROD) = RTRIM(IIM.IPROD)
            WHERE ITH.TTYPE = 'R'
            AND ITH.TTDTE = ?
            AND RTRIM(ITH.TWHS) = ?
            GROUP BY RTRIM(ITH.TPROD), RTRIM(IIM.IDESC)
            HAVING SUM(ITH.TQTY) > 0
            ORDER BY TPROD";

        using var conn = _factory.CreateOpen();
        using var cmd  = new OdbcCommand(sql, conn);
        cmd.CommandTimeout = 120;

        cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaInt;
        cmd.Parameters.Add("TWHS",  OdbcType.VarChar, 10).Value = whs;

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var prod = (r["TPROD"] as string)?.Trim() ?? string.Empty;
            var desc = (r["IDESC"] as string)?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(prod) && !productos.ContainsKey(prod))
                productos.Add(prod, desc);
        }

        return productos;
    }

        // --------------------------------------------------------------------
        //  Productos actuales por línea (centro de costo) - ODBC, parametrizado
        // --------------------------------------------------------------------
        public Dictionary<string, string> obtenerLosProductosActualesDeLinea(string centroCosto)
        {
            var productos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            int fechaInt = int.Parse(DateTime.Today.ToString("yyyyMMdd"), CultureInfo.InvariantCulture);

            string sql = @"
                SELECT 
                    RTRIM(ITH.TPROD) AS TPROD,
                    RTRIM(IIM.IDESC) AS IDESC,
                    SUM(ITH.TQTY)    AS SUMQ
                FROM GBYLX835F/ITH ITH
                INNER JOIN GBYLX835F/IIM IIM
                    ON RTRIM(ITH.TPROD) = RTRIM(IIM.IPROD)
                WHERE ITH.TTYPE = 'R'
                AND ITH.TTDTE = ?
                AND RTRIM(ITH.THWRKC) = ?
                GROUP BY RTRIM(ITH.TPROD), RTRIM(IIM.IDESC)
                HAVING SUM(ITH.TQTY) > 0
                ORDER BY TPROD";

            using var conn = _factory.CreateOpen();
            using var cmd  = new OdbcCommand(sql, conn);
            cmd.CommandTimeout = 120;

            cmd.Parameters.Add("TTDTE", OdbcType.Int).Value = fechaInt;
            cmd.Parameters.Add("WRKC",  OdbcType.VarChar, 20).Value = (centroCosto ?? string.Empty).Trim();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var prod = (r["TPROD"] as string)?.Trim() ?? string.Empty;
                var desc = (r["IDESC"] as string)?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(prod) && !productos.ContainsKey(prod))
                    productos.Add(prod, desc);
            }

            return productos;
        }
    }
}