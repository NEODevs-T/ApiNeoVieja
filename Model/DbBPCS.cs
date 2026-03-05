using System.Data;
using System.Data.OleDb;

namespace ConsultasSQL.Model
{
    public class DBconexionBPCS
    {
        private OleDbConnection ConCodPro = new OleDbConnection("Provider=IBMDA400.DataSource.1;Data Source=APPN.GRANDBAY;Password=VUSERCON01;User ID=VUSERCON01");
        // Conexion para los centros y ordenes de producción
        public OleDbConnection CodAbrirConex()
        {
            if (ConCodPro.State == ConnectionState.Closed)
                ConCodPro.Open();
            return ConCodPro;
        }

        public OleDbConnection CodCerrarConex()
        {
            if (ConCodPro.State == ConnectionState.Open)
                ConCodPro.Close();
            return ConCodPro;
        }
    }
}