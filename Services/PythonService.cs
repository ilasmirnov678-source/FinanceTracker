using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using FinanceTracker.Models.Analytics;

namespace FinanceTracker.Services;

// Запуск Python-анализатора, чтение stdout и парсинг JSON в AnalyticsResult.
public class PythonService
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
        _scriptPath = Path.Combine(_baseDirectory, "PythonApp", "analyzer.py");
    }

    // Запускает analyzer.py с заданным путём к БД и периодом дат, возвращает результат аналитики.
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

        process.Start();

        using var registration = cancellationToken.Register(() =>
        {
            try { process.Kill(); } catch { /* игнор при уже завершённом процессе */ }
        });

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Анализатор завершился с кодом {process.ExitCode}.{(string.IsNullOrEmpty(stderr) ? "" : " " + stderr.Trim())}");

        return DeserializeResult(stdout);
    }

    private static string ResolvePythonPath(string baseDir)
    {
        string venvPython = Path.Combine(baseDir, "PythonApp", "venv", "Scripts", "python.exe");
        if (File.Exists(venvPython))
            return venvPython;
        return "python";
    }

    // Внешний вызов без инжекта baseDir использует поле _baseDirectory.
    private string ResolvePythonPath() => ResolvePythonPath(_baseDirectory);

    private string BuildArguments(string dbPath, DateTime from, DateTime to)
    {
        string fromStr = from.ToString("yyyy-MM-dd");
        string toStr = to.ToString("yyyy-MM-dd");
        return $"\"{_scriptPath}\" --db \"{dbPath}\" --from {fromStr} --to {toStr}";
    }

    private static AnalyticsResult DeserializeResult(string json)
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
