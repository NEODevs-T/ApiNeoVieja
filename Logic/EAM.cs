using System.Data;
using System.Data.Odbc;

namespace ConsultasSQL.Logic
{
    public class EAM
    {
        private readonly IBpcsConnectionFactory _bpcsConn;

        public EAM(IBpcsConnectionFactory factory)
        {
            _bpcsConn = factory;
        }

        public Dictionary<string, string> ObtenerEquiposEAMSegunCentroDeCosto(string centroCosto)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Usa System Naming → LIB/FILE
            string sql = @"
                SELECT 
                    RTRIM(EEQMST.EQID)   AS EQID,
                    RTRIM(EEQMST.EDESCR) AS EDESCR
                FROM VEAM900F/EEQMST EEQMST
                WHERE 
                      EEQMST.EQCO = 10
                  AND EEQMST.ECSTCR LIKE ?
                  AND (EEQMST.EQID LIKE '85%' OR EEQMST.EQID LIKE 'OF%')
            ";

            using var conn = _bpcsConn.CreateOpen();
            using var cmd  = new OdbcCommand(sql, conn);

            cmd.CommandTimeout = 120;

            // Parámetro posicional ODBC (?)
            cmd.Parameters.Add(new OdbcParameter
            {
                OdbcType = OdbcType.VarChar,
                Value = centroCosto + "%"      // ejemplo: "32%" → "32%"
            });

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string eqid   = (reader["EQID"] as string)?.Trim()   ?? "";
                string descr  = (reader["EDESCR"] as string)?.Trim() ?? "";

                if (!result.ContainsKey(eqid))
                    result.Add(eqid, descr);
            }

            return result;
        }
    }
}