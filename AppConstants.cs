namespace FinanceTracker;

// Общие константы приложения: пути к БД и Python, аргументы CLI, таймауты.
public static class AppConstants
{
    // БД
    public static readonly string DefaultDbRelativePath = "Database/finance.db";
    public static readonly string DatabaseFolder = "Database";
    public static readonly string SchemaFileName = "schema.sql";

    // Python-анализатор
    public static readonly string PythonAppFolder = "PythonApp";
    public static readonly string AnalyzerScriptName = "analyzer.py";
    public static readonly string VenvFolder = "venv";
    public static readonly string VenvScriptsFolder = "Scripts";
    public static readonly string PythonExeName = "python.exe";
    public static readonly string PythonFallback = "python";

    // Аргументы CLI analyzer.py
    public static readonly string CliArgDb = "--db";
    public static readonly string CliArgFrom = "--from";
    public static readonly string CliArgTo = "--to";

    // Таймаут запуска анализатора (секунды)
    public static readonly int ReportTimeoutSeconds = 15;
}
