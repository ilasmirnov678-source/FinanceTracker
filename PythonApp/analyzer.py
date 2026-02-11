#!/usr/bin/env python3
# Точка входа: разбор CLI, чтение SQLite, фильтр по датам.

import argparse
import sqlite3
import sys
from pathlib import Path

import pandas as pd


def parse_args():
    parser = argparse.ArgumentParser(description="Аналитика по транзакциям из SQLite.")
    parser.add_argument("--db", required=True, help="Путь к файлу finance.db")
    parser.add_argument("--from", dest="date_from", required=True, help="Начало периода (yyyy-MM-dd)")
    parser.add_argument("--to", dest="date_to", required=True, help="Конец периода (yyyy-MM-dd)")
    return parser.parse_args()


def load_transactions(conn: sqlite3.Connection, date_from: str, date_to: str) -> pd.DataFrame:
    # Date в БД — TEXT ISO8601; фильтр включительно от date_from, исключительно до date_to
    query = """
        SELECT Id, Date, Amount, Category, Description
        FROM Transactions
        WHERE Date >= ? AND Date < ?
        ORDER BY Date
    """
    return pd.read_sql(query, conn, params=(date_from, date_to))


def main():
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

    # Этап 1: только загрузка; вывод JSON — далее
    sys.exit(0)


if __name__ == "__main__":
    main()
