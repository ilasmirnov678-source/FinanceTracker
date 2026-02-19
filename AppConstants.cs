namespace FinanceTracker;

// Константы приложения: пути к БД и Python, аргументы CLI, таймауты.
public static class AppConstants
{
    // Пути и имена для БД.
    public static readonly string DefaultDbRelativePath = "Database/finance.db";
    public static readonly string DatabaseFolder = "Database";
    public static readonly string SchemaFileName = "schema.sql";

    // Пути и имена для Python-анализатора.
    public static readonly string PythonAppFolder = "PythonApp";
    public static readonly string AnalyzerScriptName = "analyzer.py";
    public static readonly string VenvFolder = "venv";
    public static readonly string VenvScriptsFolder = "Scripts";
    public static readonly string PythonExeName = "python.exe";
    public static readonly string PythonFallback = "python";

    // Аргументы командной строки analyzer.py.
    public static readonly string CliArgDb = "--db";
    public static readonly string CliArgFrom = "--from";
    public static readonly string CliArgTo = "--to";

    // Таймаут запуска анализатора, секунды.
    public static readonly int ReportTimeoutSeconds = 15;
}
