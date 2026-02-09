# Finance Tracker

WPF-приложение для учёта личных финансов с хранением данных в SQLite и аналитикой через Python (Pandas, Matplotlib).

## Тестирование

```bash
dotnet test
```

Или только тестовый проект:
```bash
dotnet test FinanceTracker.Tests
```

## Требования

- .NET 8 — основное приложение (WPF)
- Python 3.x — скрипт аналитики
- pip-пакеты (для PythonApp): pandas, matplotlib (устанавливаются в виртуальное окружение проекта)

