-- Таблица транзакций (дата в ISO8601 TEXT, в C# — DateTime)
CREATE TABLE IF NOT EXISTS Transactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date TEXT NOT NULL,
    Amount REAL NOT NULL,
    Category TEXT NOT NULL,
    Description TEXT
);
