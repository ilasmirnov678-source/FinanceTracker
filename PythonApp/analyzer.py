#!/usr/bin/env python3
# Аналитика по транзакциям из SQLite за период. CLI: --db (путь к БД), --from, --to (даты yyyy-MM-dd). Результат — JSON в stdout (by_category, by_month, total).

import argparse
import json
import sqlite3
import sys
from pathlib import Path

import pandas as pd


# Разбор CLI: --db, --from, --to (даты yyyy-MM-dd). Вызов из C# с теми же именами аргументов.
def parse_args():
    parser = argparse.ArgumentParser(description="Аналитика по транзакциям из SQLite.")
    parser.add_argument("--db", required=True, help="Путь к файлу finance.db")
    parser.add_argument("--from", dest="date_from", required=True, help="Начало периода (yyyy-MM-dd)")
    parser.add_argument("--to", dest="date_to", required=True, help="Конец периода (yyyy-MM-dd)")
    return parser.parse_args()


# Чтение транзакций за период (Date >= date_from, Date < date_to) из таблицы Transactions.
def load_transactions(conn: sqlite3.Connection, date_from: str, date_to: str) -> pd.DataFrame:
    # Date в БД — TEXT ISO8601; фильтр включительно от date_from, исключительно до date_to
    query = """
        SELECT Id, Date, Amount, Category, Description
        FROM Transactions
        WHERE Date >= ? AND Date < ?
        ORDER BY Date
    """
    return pd.read_sql(query, conn, params=(date_from, date_to))


# Агрегация: по категориям (name, sum), по месяцам (month yyyy-MM, sum), total. Контракт JSON для C#.
def aggregate(df: pd.DataFrame) -> dict:
    # По категориям: группа — сумма Amount; знак не меняем (в схеме Amount уже положительный)
    by_cat = df.groupby("Category", as_index=False)["Amount"].sum()
    by_category = [{"name": row["Category"], "sum": float(row["Amount"])} for _, row in by_cat.iterrows()]

    # По месяцам: из Date (TEXT yyyy-MM-dd) берём yyyy-MM, группируем, сумма, сортировка по месяцу
    df = df.copy()
    df["month"] = df["Date"].str[:7]
    by_mon = df.groupby("month", as_index=False)["Amount"].sum().sort_values("month")
    by_month = [{"month": row["month"], "sum": float(row["Amount"])} for _, row in by_mon.iterrows()]

    total = float(df["Amount"].sum()) if len(df) else 0.0
    return {"by_category": by_category, "by_month": by_month, "total": total}


def main():
    # Разбор аргументов, чтение БД, агрегация по категориям и месяцам, вывод JSON в stdout.
    args = parse_args()
    db_path = args.db
    date_from = getattr(args, "date_from")
    date_to = getattr(args, "date_to")

    if not Path(db_path).exists():
        print(f"Файл БД не найден: {db_path}", file=sys.stderr)
        sys.exit(1)

    try:
        with sqlite3.connect(f"file:{db_path}?mode=ro", uri=True) as conn:
            df = load_transactions(conn, date_from, date_to)
    except Exception as e:
        print(f"Ошибка чтения БД: {e}", file=sys.stderr)
        sys.exit(2)

    analytics = aggregate(df)
    print(json.dumps(analytics, ensure_ascii=False))
    sys.exit(0)


if __name__ == "__main__":
    main()
