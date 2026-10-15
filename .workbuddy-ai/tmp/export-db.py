"""
Exports NexusDb as a portable SQL script: CREATE TABLE for every user table,
then INSERT for every row, in foreign-key-safe order.

The .bak is the primary artefact for a SQL Server grader, but it needs the same
edition to restore and cannot be read without one. This script is the fallback:
it runs on any SQL Server from SSMS or sqlcmd and rebuilds the database from
nothing.

Run:  python .workbuddy-ai/tmp/export-db.py
"""

import os
import subprocess
import sys
from datetime import date

BASE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(BASE, "deliverables", "database", "NexusDb_SchemaAndData.sql")
SEP = "\x01"

SQLCMD = r"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"

CONN = ["-S", r"DESKTOP-BD6OLPC\SQLEXPRESS", "-U", "sa", "-P", "aptech", "-d", "NexusDb", "-l", "90"]


def query(sql):
    p = subprocess.run(
        [SQLCMD, *CONN, "-h", "-1", "-W", "-s", SEP, "-Q", "SET NOCOUNT ON; " + sql],
        capture_output=True, text=True, encoding="utf-8", errors="replace")
    if p.returncode != 0:
        raise RuntimeError(f"sqlcmd failed ({p.returncode}): {p.stderr.strip()}")

    rows = []
    for line in p.stdout.splitlines():
        line = line.rstrip()
        if not line or line.startswith("Changed database context"):
            continue
        rows.append(line.split(SEP))
    return rows


def sql_str(v):
    return "'" + v.replace("'", "''") + "'"


NUMERIC = {"bit", "int", "bigint", "smallint", "tinyint", "decimal", "numeric",
           "money", "smallmoney", "float", "real"}
TEMPORAL = {"date", "datetime", "datetime2", "smalldatetime", "time"}


def literal(value, ctype):
    if value == "NULL":
        return "NULL"
    if ctype == "bit":
        return "1" if value == "1" else "0"
    if ctype in NUMERIC:
        return value
    if ctype in TEMPORAL or ctype == "uniqueidentifier":
        return sql_str(value)
    if ctype in {"binary", "varbinary", "image"}:
        return "0x" + value.encode("utf-8").hex()
    return sql_str(value)


def columns_of(table):
    rows = query(f"""
        SELECT c.name, t.name, c.is_nullable, c.is_identity,
               c.max_length, c.precision, c.scale
        FROM sys.columns c
        JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.{table}')
        ORDER BY c.column_id""")
    return [{
        "name": r[0], "type": r[1], "nullable": r[2] == "1", "identity": r[3] == "1",
        "max_length": int(r[4]), "precision": int(r[5]), "scale": int(r[6]),
    } for r in rows]


def type_sql(c):
    t = c["type"]
    if t in ("varchar", "char", "varbinary", "binary"):
        n = "max" if c["max_length"] == -1 else c["max_length"]
        return f"{t}({n})"
    if t in ("nvarchar", "nchar"):
        n = "max" if c["max_length"] == -1 else c["max_length"] // 2
        return f"{t}({n})"
    if t in ("decimal", "numeric"):
        return f"{t}({c['precision']},{c['scale']})"
    if t == "float":
        return f"float({c['precision']})"
    if t in ("datetime2", "time"):
        return f"{t}({c['scale']})"
    return t


def table_order():
    """Parents before children, so the INSERTs satisfy every foreign key."""
    tables = [r[0] for r in query("""
        SELECT t.name FROM sys.tables t
        WHERE t.schema_id = SCHEMA_ID('dbo') AND t.is_ms_shipped = 0
        ORDER BY t.name""")]

    deps = {t: [] for t in tables}
    for child, parent in query("""
            SELECT OBJECT_NAME(fk.parent_object_id), OBJECT_NAME(fk.referenced_object_id)
            FROM sys.foreign_keys fk"""):
        if child in deps and parent in deps and child != parent:
            deps[child].append(parent)

    ordered, seen, visiting = [], set(), set()

    def visit(t):
        if t in seen or t in visiting:
            return
        visiting.add(t)
        for p in deps.get(t, []):
            visit(p)
        visiting.discard(t)
        seen.add(t)
        ordered.append(t)

    if "__EFMigrationsHistory" in deps:
        visit("__EFMigrationsHistory")
    for t in tables:
        visit(t)
    return ordered


def main():
    lines = []
    tables = table_order()

    lines += [
        "/*",
        "  NEXUS Service Marketing System - database export",
        f"  Generated from NexusDb on {date.today().isoformat()}",
        "",
        "  Schema and data, in foreign-key-safe order. Run this against an empty",
        "  database to rebuild the demo state:",
        "",
        "      sqlcmd -S .\\SQLEXPRESS -U sa -P <password> -d NexusDb -i NexusDb_SchemaAndData.sql",
        "",
        "  Or open it in SSMS and Execute. NexusDb_Full.bak in this folder is the",
        "  native backup and restores faster, but needs the same SQL Server edition.",
        "*/",
        "",
        "SET NOCOUNT ON;",
        "SET QUOTED_IDENTIFIER ON;",
        "GO",
        "",
    ]

    for table in tables:
        cols = columns_of(table)
        lines.append(f"/* ---- {table} ---- */")
        lines.append(f"IF OBJECT_ID('dbo.{table}') IS NULL")
        lines.append("BEGIN")
        lines.append(f"    CREATE TABLE dbo.{table} (")
        lines.append(",\n".join(
            f"        [{c['name']}] {type_sql(c)}" +
            (" IDENTITY(1,1)" if c["identity"] else "") +
            (" NULL" if c["nullable"] else " NOT NULL")
            for c in cols))
        lines.append("    );")
        lines.append("END")
        lines.append("GO")
        lines.append("")

        collist = ", ".join(f"[{c['name']}]" for c in cols)
        rows = query(f"SELECT {collist} FROM dbo.{table}")

        if not rows:
            lines.append(f"-- {table}: no rows")
            lines.append("GO")
            lines.append("")
            continue

        for i in range(0, len(rows), 200):
            chunk = rows[i:i + 200]
            lines.append(f"INSERT INTO dbo.{table} ({collist}) VALUES")
            lines.append(",\n".join(
                "    (" + ", ".join(literal(v, cols[j]["type"]) for j, v in enumerate(r)) + ")"
                for r in chunk) + ";")
            lines.append("GO")
        lines.append("")

    lines.append("/* Realign identity seeds with the imported rows, so the next insert")
    lines.append("   does not collide with an id the export claimed. */")
    for table in tables:
        if any(c["identity"] for c in columns_of(table)):
            lines.append(f"DBCC CHECKIDENT ('dbo.{table}', RESEED);")
    lines.append("GO")
    lines.append("")

    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"Wrote {OUT}")
    print(f"Tables: {len(tables)}")
    print(f"Size: {os.path.getsize(OUT) / 1024:.1f} KB")


if __name__ == "__main__":
    sys.exit(main())
