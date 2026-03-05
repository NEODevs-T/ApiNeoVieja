using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ConsultasSQL.Model;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;

namespace ConsultasSQL.Logic{
    public class BPCSGB 
    {
        private DbBpcsGB conexionBPCSGB = new DbBpcsGB();
        private SqlCommand CommandBPCSGB = new SqlCommand();
        private SqlDataReader? DataReaderBPCSGB;

        public Dictionary<string,string> ObtenerProductosActualesSegunCentroDeCosto(string centroCosto){
            Dictionary<string,string> ProductosActuales = new Dictionary<string,string>();
            DataTable dataTable = new DataTable();
            CommandBPCSGB.Connection = conexionBPCSGB.OpeAbrirConex();
            CommandBPCSGB.CommandText = @"
                SELECT TOP (1000) [Vista_Orden_Fabricacion].SPROD AS [IdProducto],Vista_Producto.IDESC AS [Descripcion]
                FROM [DbBpcsGB].[dbo].[Vista_Orden_Fabricacion]
                INNER JOIN [dbo].Vista_Producto ON Vista_Producto.IPROD = [Vista_Orden_Fabricacion].SPROD
                WHERE [ESTADO] = 'ABIERTA' AND SWRKC = " + centroCosto + @"
            ";
            DataReaderBPCSGB = CommandBPCSGB.ExecuteReader();
            dataTable.Load(DataReaderBPCSGB);

            foreach (DataRow row in dataTable.Rows)
            {
                if(! ProductosActuales.ContainsKey(row["IdProducto"].ToString())){
                    ProductosActuales.Add(row["IdProducto"].ToString(),row["Descripcion"].ToString());
                }
            }

            return ProductosActuales;
        }
        public string ObtenerDescripcionDelProductoPorSuCodigo(string codigoProducto){
            string descripcion = "";
            DataTable dataTable = new DataTable();
            CommandBPCSGB.Connection = conexionBPCSGB.OpeAbrirConex();
            CommandBPCSGB.CommandText = @"
                SELECT TOP (1) IDESC
                FROM dbo.Vista_Producto
                WHERE (IPROD='" + codigoProducto + @"')
            ";
            DataReaderBPCSGB = CommandBPCSGB.ExecuteReader();
            dataTable.Load(DataReaderBPCSGB);
            
            foreach (DataRow row in dataTable.Rows)
            {
                descripcion = row["IDESC"].ToString();
            }

            return descripcion;
        }
    }
}