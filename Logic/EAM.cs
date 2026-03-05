using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ConsultasSQL.Model;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;

namespace ConsultasSQL.Logic{

    public class EAM 
    {
        private DBconexionBPCS conexionBPCS = new DBconexionBPCS();
        private OleDbCommand CommandBPCS = new OleDbCommand();
        private OleDbDataReader? DataReaderBPCS;
        public Dictionary<string,string> ObtenerEquiposEAMSegunCentroDeCosto(string centroCosto){
            Dictionary<string,string> CodigosEAM = new Dictionary<string,string>();
            DataTable dataTable = new DataTable();
            CommandBPCS.Connection = conexionBPCS.CodAbrirConex();
            CommandBPCS.CommandText = @"
                SELECT EEQMST.EQID, EEQMST.EDESCR
                FROM X7073a51.VEAM900F.EEQMST EEQMST
                WHERE (EEQMST.EQCO=10) AND (EEQMST.ECSTCR Like '32%') AND (EEQMST.EQID Like '85%') OR (EEQMST.EQID Like 'OF%')
            ";
            DataReaderBPCS = CommandBPCS.ExecuteReader();
            dataTable.Load(DataReaderBPCS);
            CommandBPCS.Connection = conexionBPCS.CodCerrarConex();
            
            return CodigosEAM;
        }
    }
}