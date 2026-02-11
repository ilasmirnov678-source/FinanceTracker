# Тесты analyzer.py: запуск через subprocess, проверка JSON и кодов выхода.

import json
import sqlite3
import subprocess
import sys
from pathlib import Path

import pytest

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
SCRIPT = REPO_ROOT / "PythonApp" / "analyzer.py"
SCHEMA_SQL = REPO_ROOT / "Database" / "schema.sql"


def run_analyzer(db_path: str, date_from: str, date_to: str):
    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--db", db_path, "--from", date_from, "--to", date_to],
        capture_output=True,
        text=True,
        cwd=str(REPO_ROOT),
    )
    return result


def create_temp_db(tmp_path, rows):
    db = tmp_path / "test.db"
    conn = sqlite3.connect(str(db))
    conn.executescript(SCHEMA_SQL.read_text(encoding="utf-8"))
    for r in rows:
        conn.execute(
            "INSERT INTO Transactions (Date, Amount, Category, Description) VALUES (?, ?, ?, ?)",
            (r["date"], r["amount"], r["category"], r.get("description", "")),
        )
    conn.commit()
    conn.close()
    return str(db)


def test_stdout_is_valid_json_with_required_keys(tmp_path):
    db = create_temp_db(
        tmp_path,
        [
            {"date": "2025-02-01", "amount": 100.0, "category": "A"},
            {"date": "2025-02-15", "amount": 50.0, "category": "A"},
            {"date": "2025-02-20", "amount": 200.0, "category": "B"},
        ],
    )
    result = run_analyzer(db, "2025-02-01", "2025-03-01")
    assert result.returncode == 0
    data = json.loads(result.stdout)
    assert "by_category" in data
    assert "by_month" in data
    assert "total" in data


def test_by_category_sums(tmp_path):
    db = create_temp_db(
        tmp_path,
        [
            {"date": "2025-02-01", "amount": 100.0, "category": "Еда"},
            {"date": "2025-02-02", "amount": 50.0, "category": "Еда"},
            {"date": "2025-02-03", "amount": 300.0, "category": "Транспорт"},
        ],
    )
    result = run_analyzer(db, "2025-02-01", "2025-03-01")
    assert result.returncode == 0
    data = json.loads(result.stdout)
    by_cat = {x["name"]: x["sum"] for x in data["by_category"]}
    assert by_cat["Еда"] == 150.0
    assert by_cat["Транспорт"] == 300.0
    assert data["total"] == 450.0


def test_by_month_sums(tmp_path):
    db = create_temp_db(
        tmp_path,
        [
            {"date": "2025-01-10", "amount": 100.0, "category": "X"},
            {"date": "2025-02-05", "amount": 200.0, "category": "X"},
            {"date": "2025-02-15", "amount": 50.0, "category": "Y"},
        ],
    )
    result = run_analyzer(db, "2025-01-01", "2025-03-01")
    assert result.returncode == 0
    data = json.loads(result.stdout)
    by_month = {x["month"]: x["sum"] for x in data["by_month"]}
    assert by_month["2025-01"] == 100.0
    assert by_month["2025-02"] == 250.0
    assert data["total"] == 350.0


def test_empty_period(tmp_path):
    db = create_temp_db(tmp_path, [{"date": "2025-02-01", "amount": 100.0, "category": "A"}])
    result = run_analyzer(db, "2026-01-01", "2026-02-01")
    assert result.returncode == 0
    data = json.loads(result.stdout)
    assert data["by_category"] == []
    assert data["by_month"] == []
    assert data["total"] == 0.0


def test_missing_db_exits_nonzero_and_stderr(tmp_path):
    result = run_analyzer(str(tmp_path / "nonexistent.db"), "2025-02-01", "2025-03-01")
    assert result.returncode != 0
    assert "Файл БД не найден" in result.stderr or "nonexistent" in result.stderr


def test_date_filter_exclusive_to(tmp_path):
    db = create_temp_db(
        tmp_path,
        [
            {"date": "2025-02-28", "amount": 100.0, "category": "A"},
            {"date": "2025-03-01", "amount": 200.0, "category": "A"},
        ],
    )
    result = run_analyzer(db, "2025-02-01", "2025-03-01")
    assert result.returncode == 0
    data = json.loads(result.stdout)
    assert data["total"] == 100.0
