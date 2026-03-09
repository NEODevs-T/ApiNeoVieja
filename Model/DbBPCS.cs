using System.Data.Odbc;

public interface IBpcsConnectionFactory
{
    OdbcConnection CreateOpen();
}

public class BpcsConnectionFactory : IBpcsConnectionFactory
{
    // Ajusta estos valores o muévelos a appsettings.json + secrets
    private const string Host = "APPN.GRANDBAY";
    private const string User = "VUSERCON01";
    private const string Password = "VUSERCON01";

    // Library list de tu DSN
    private const string DefaultLibraries = "QGPL";
    private const string LibraryList = "GBYLX835F,VENCAFVIL,VENPFLFIL";

    // Nombre EXACTO del driver que sí funcionó en tu diagnóstico
    private const string DriverName = "iSeries Access ODBC Driver";

    public OdbcConnection CreateOpen()
    {
        var cs =
            $"Driver={{{DriverName}}};" +
            $"System={Host};" +
            $"Uid={User};Pwd={Password};" +
            $"Naming=1;" +                          // System naming (LIB/FILE)
            $"DefaultLibraries={DefaultLibraries};" +
            $"LibraryList={LibraryList};" +
            $"CommitMode=0;" +
            $"Prompt=0;";

        var cn = new OdbcConnection(cs);
        cn.Open();
        return cn;
    }
}