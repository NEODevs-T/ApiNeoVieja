using System;
using System.Data;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Text;

public static class BpcsConnectivityDiagnostics
{
    // >>>>>>>> AJUSTA SOLO ESTOS VALORES <<<<<<<<
    private const string Host = "APPN.GRANDBAY";
    private const string User = "VUSERCON01";        // <-- usuario real
    private const string Pwd  = "VUSERCON01";        // <-- clave real
    private const string LibraryList = "GBYLX835F,VENCAFVIL,VENPFLFIL";
    private const string DefaultCollection = "QGPL";
    private const string DsnName = "BPCS_VE";        // si quieres probar el DSN que mostraste
    // >>>>>>>> FIN AJUSTES <<<<<<<<

    public static string RunAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Diagnóstico de conectividad IBM i (x{(Environment.Is64BitProcess ? 64 : 32)}). Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // 1) OLE DB - IBMDA400 (variantes incrementales)
        TestOleDbVariant(sb,
            "OLEDB/IBMDA400 - mínima",
            $"Provider=IBMDA400;Data Source={Host};User ID={User};Password={Pwd};"
        );

        TestOleDbVariant(sb,
            "OLEDB/IBMDA400 - con Naming=1 (System naming)",
            $"Provider=IBMDA400;Data Source={Host};User ID={User};Password={Pwd};Naming=1;"
        );

        TestOleDbVariant(sb,
            "OLEDB/IBMDA400 - Naming=1 + Default Collection",
            $"Provider=IBMDA400;Data Source={Host};User ID={User};Password={Pwd};Naming=1;Default Collection={DefaultCollection};"
        );

        TestOleDbVariant(sb,
            "OLEDB/IBMDA400 - Naming=1 + Default + Library List",
            $"Provider=IBMDA400;Data Source={Host};User ID={User};Password={Pwd};Naming=1;Default Collection={DefaultCollection};Library List={LibraryList};"
        );

        // Algunas instalaciones registran el ProgID como IBMDA400.DataSource.1
        TestOleDbVariant(sb,
            "OLEDB/IBMDA400.DataSource.1 - Naming=1 + Default + Library List",
            $"Provider=IBMDA400.DataSource.1;Data Source={Host};User ID={User};Password={Pwd};Naming=1;Default Collection={DefaultCollection};Library List={LibraryList};"
        );

        // 2) ODBC - DSN y DSN-less (por si decides quedarte en ODBC)
        TestOdbcVariant(sb,
            "ODBC/DSN (System naming heredado del DSN)",
            $"DSN={DsnName};Uid={User};Pwd={Pwd};"
        );

        // Nombre del driver según tu captura (64 bits): "iSeries Access ODBC Driver".
        // Si en tu máquina apareciera como "IBM i Access ODBC Driver", cambia el nombre entre llaves.
        TestOdbcVariant(sb,
            "ODBC/DSN-less (iSeries Access ODBC Driver) con Naming=1",
            $"Driver={{iSeries Access ODBC Driver}};System={Host};Uid={User};Pwd={Pwd};Naming=1;DefaultLibraries={DefaultCollection};LibraryList={LibraryList};CommitMode=0;Prompt=0;"
        );

        sb.AppendLine();
        sb.AppendLine("Fin del diagnóstico.");
        return sb.ToString();
    }

    private static void TestOleDbVariant(StringBuilder sb, string title, string connString)
    {
        sb.AppendLine(new string('-', 80));
        sb.AppendLine(title);

        try
        {
            using var cn = new OleDbConnection(connString);
            cn.Open();
            sb.AppendLine("  -> Conexión ABIERTA correctamente (OLE DB).");

            // Consulta de humo: en System naming, el catálogo se puede usar con slash:
            using var cmd = new OleDbCommand("SELECT CURRENT_DATE FROM SYSIBM/SYSDUMMY1 WITH UR", cn);
            var result = cmd.ExecuteScalar();
            sb.AppendLine($"  -> Consulta de humo OK. CURRENT_DATE = {result}");
        }
        catch (OleDbException ex)
        {
            sb.AppendLine($"  !! OleDbException: {ex.Message}");
            foreach (OleDbError err in ex.Errors)
                sb.AppendLine($"     - Source: {err.Source} | SQLState: {err.SQLState} | Native: {err.NativeError} | Message: {err.Message}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  !! Exception: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void TestOdbcVariant(StringBuilder sb, string title, string connString)
    {
        sb.AppendLine(new string('-', 80));
        sb.AppendLine(title);

        try
        {
            using var cn = new OdbcConnection(connString);
            cn.Open();
            sb.AppendLine("  -> Conexión ABIERTA correctamente (ODBC).");

            // Con System naming, usa slash:
            using var cmd = new OdbcCommand("SELECT CURRENT_DATE FROM SYSIBM/SYSDUMMY1 WITH UR", cn);
            var result = cmd.ExecuteScalar();
            sb.AppendLine($"  -> Consulta de humo OK. CURRENT_DATE = {result}");
        }
        catch (OdbcException ex)
        {
            sb.AppendLine($"  !! OdbcException: {ex.Message}");
            foreach (OdbcError err in ex.Errors)
                sb.AppendLine($"     - Source: {err.Source} | SQLState: {err.SQLState} | Native: {err.NativeError} | Message: {err.Message}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  !! Exception: {ex.GetType().Name}: {ex.Message}");
        }
    }
}