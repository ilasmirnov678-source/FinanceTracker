using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using FinanceTracker;
using FinanceTracker.Models.Analytics;

namespace FinanceTracker.Services;

// Запуск Python-анализатора, чтение stdout и парсинг JSON в AnalyticsResult.
public class PythonService : IPythonService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _baseDirectory;
    private readonly string _scriptPath;

    public PythonService()
    {
        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _scriptPath = Path.Combine(_baseDirectory, AppConstants.PythonAppFolder, AppConstants.AnalyzerScriptName);
    }

    // Для тестов: задать базовую директорию (например, без PythonApp — проверка FileNotFoundException).
    internal PythonService(string baseDirectory)
    {
        _baseDirectory = baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _scriptPath = Path.Combine(_baseDirectory, AppConstants.PythonAppFolder, AppConstants.AnalyzerScriptName);
    }

    // Запустить analyzer.py (путь к БД, период from–to), прочитать stdout/stderr, распарсить JSON в AnalyticsResult. Таймаут по константе. Выброс: FileNotFoundException, InvalidOperationException (таймаут, ненулевой exit code, ошибка парсинга), OperationCanceledException при отмене.
    public async Task<AnalyticsResult> GenerateReportAsync(string dbPath, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_scriptPath))
            throw new FileNotFoundException($"Скрипт аналитики не найден: {_scriptPath}", _scriptPath);

        string pythonPath = ResolvePythonPath();
        string arguments = BuildArguments(dbPath, from, to);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = arguments,
            WorkingDirectory = _baseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        process.StartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

        process.Start();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(AppConstants.ReportTimeoutSeconds));

        using var registration = cts.Token.Register(() =>
        {
            try { process.Kill(); } catch { /* игнор при уже завершённом процессе */ }
        });

        // При таймауте сначала дочитать stderr; отмену чтения потоков выполнять только при отмене пользователем.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            string errOut = await stdoutTask;
            string errErr = await stderrTask;
            if (cancellationToken.IsCancellationRequested)
                throw;
            throw new InvalidOperationException(
                $"Таймаут выполнения анализатора ({AppConstants.ReportTimeoutSeconds} с).{(string.IsNullOrEmpty(errErr) ? "" : " " + errErr.Trim())}");
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Анализатор завершился с кодом {process.ExitCode}.{(string.IsNullOrEmpty(stderr) ? "" : " " + stderr.Trim())}");

        return DeserializeResult(stdout);
    }

    // Определить путь к Python: сначала venv в PythonApp/Scripts/python.exe, иначе исполняемый python из PATH.
    internal static string ResolvePythonPath(string baseDir)
    {
        string venvPython = Path.Combine(baseDir, AppConstants.PythonAppFolder, AppConstants.VenvFolder, AppConstants.VenvScriptsFolder, AppConstants.PythonExeName);
        if (File.Exists(venvPython))
            return venvPython;
        return AppConstants.PythonFallback;
    }

    // Вызвать ResolvePythonPath для _baseDirectory (используется из публичного API).
    private string ResolvePythonPath() => ResolvePythonPath(_baseDirectory);

    // Собрать аргументы CLI: путь к скрипту, --db, --from, --to (даты yyyy-MM-dd).
    internal string BuildArguments(string dbPath, DateTime from, DateTime to)
    {
        string fromStr = from.ToString("yyyy-MM-dd");
        string toStr = to.ToString("yyyy-MM-dd");
        return $"\"{_scriptPath}\" {AppConstants.CliArgDb} \"{dbPath}\" {AppConstants.CliArgFrom} {fromStr} {AppConstants.CliArgTo} {toStr}";
    }

    // Распарсить JSON в AnalyticsResult (контракт: by_category, by_month, total). Выброс InvalidOperationException при пустом выводе или неверном JSON.
    internal static AnalyticsResult DeserializeResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Анализатор не вернул данные (пустой вывод).");

        try
        {
            var result = JsonSerializer.Deserialize<AnalyticsResult>(json, JsonOptions);
            if (result == null)
                throw new InvalidOperationException("Не удалось прочитать результат аналитики.");
            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Ошибка разбора JSON от анализатора: {ex.Message}", ex);
        }
    }
}
