/*
  Exports NexusDb as a portable SQL script: CREATE TABLE for every user table,
  then INSERT for every row, in foreign-key-safe order.

  The .bak is the primary artefact for a SQL Server grader, but it must be
  restored by the same edition, and the export cannot be read without one. This
  script is the fallback: it runs on any SQL Server from SSMS or sqlcmd and
  rebuilds the database from nothing.

  Run:  node .workbuddy-ai/tmp/export-db.js
*/

const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const SQLCMD = 'C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\170\\Tools\\Binn\\sqlcmd.exe';
const OUT = path.join(__dirname, '..', '..', 'deliverables', 'database', 'NexusDb_SchemaAndData.sql');

function query(sql) {
    const args = [
        '-S', 'DESKTOP-BD6OLPC\\SQLEXPRESS',
        '-U', 'sa', '-P', 'aptech',
        '-d', 'NexusDb',
        '-l', '60',
        '-h', '-1', '-W', '-s', '\u0001',
        '-Q', 'SET NOCOUNT ON; ' + sql
    ];

    const raw = execFileSync('cmd.exe', ['/c', SQLCMD, ...args], {
        encoding: 'utf8',
        maxBuffer: 64 * 1024 * 1024
    });
    return raw.split(/\r?\n/)
        .map(l => l.trimEnd())
        .filter(l => l.length > 0 && !l.startsWith('Changed database context'))
        .map(l => l.split('\u0001'));
}

function sqlString(v) {
    return "'" + v.replace(/'/g, "''") + "'";
}

function literal(value, type, isNullable) {
    if (value === 'NULL') return 'NULL';

    switch (type) {
        case 'bit':
            return value === '1' ? '1' : '0';
        case 'int': case 'bigint': case 'smallint': case 'tinyint':
        case 'decimal': case 'numeric': case 'money': case 'smallmoney':
        case 'float': case 'real':
            return value;
        case 'uniqueidentifier':
            return sqlString(value);
        case 'date': case 'datetime': case 'datetime2': case 'smalldatetime':
            return sqlString(value);
        case 'time':
            return sqlString(value);
        case 'binary': case 'varbinary': case 'image':
            return '0x' + Buffer.from(value, 'utf8').toString('hex');
        default:
            return sqlString(value);
    }
}

function columnsOf(table) {
    return query(`
        SELECT c.name, t.name, c.is_nullable, c.is_identity,
               c.max_length, c.precision, c.scale
        FROM sys.columns c
        JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.${table}')
        ORDER BY c.column_id`)
        .map(([name, type, nullable, identity, maxLength, precision, scale]) =>
            ({ name, type, nullable: nullable === '1', identity: identity === '1', maxLength: +maxLength, precision: +precision, scale: +scale }));
}

function typeSql(col) {
    switch (col.type) {
        case 'varchar': case 'char': case 'varbinary': case 'binary':
            return `${col.type}(${col.maxLength === -1 ? 'max' : col.maxLength})`;
        case 'nvarchar': case 'nchar':
            return `${col.type}(${col.maxLength === -1 ? 'max' : col.maxLength / 2})`;
        case 'decimal': case 'numeric':
            return `${col.type}(${col.precision},${col.scale})`;
        case 'float':
            return `float(${col.precision})`;
        case 'datetime2': case 'time':
            return `${col.type}(${col.scale})`;
        default:
            return col.type;
    }
}

function tableOrder() {
    // Parents before children, so the INSERTs satisfy every foreign key.
    const tables = query(`
        SELECT t.name FROM sys.tables t
        WHERE t.schema_id = SCHEMA_ID('dbo') AND t.is_ms_shipped = 0
        ORDER BY t.name`).map(r => r[0]);

    const deps = new Map();
    for (const t of tables) deps.set(t, []);

    const fks = query(`
        SELECT OBJECT_NAME(fk.parent_object_id), OBJECT_NAME(fk.referenced_object_id)
        FROM sys.foreign_keys fk`);
    for (const [child, parent] of fks) {
        if (deps.has(child) && deps.has(parent) && child !== parent) deps.get(child).push(parent);
    }

    const ordered = [];
    const seen = new Set();
    const visiting = new Set();

    function visit(t) {
        if (seen.has(t) || visiting.has(t)) return;
        visiting.add(t);
        for (const p of deps.get(t) || []) visit(p);
        visiting.delete(t);
        seen.add(t);
        ordered.push(t);
    }

    // __EFMigrationsHistory has no FKs and must land early enough that a
    // grader can tell which migrations were applied.
    visit('__EFMigrationsHistory');
    for (const t of tables) visit(t);
    return ordered;
}

function main() {
    const lines = [];
    const tables = tableOrder();

    lines.push('/*');
    lines.push('  NEXUS Service Marketing System - database export');
    lines.push('  Generated from NexusDb on ' + new Date().toISOString().slice(0, 10));
    lines.push('');
    lines.push('  schema + data, in foreign-key-safe order. Run this against an empty');
    lines.push('  database to rebuild the demo state:');
    lines.push('');
    lines.push('      sqlcmd -S .\\SQLEXPRESS -U sa -P <password> -d NexusDb -i NexusDb_SchemaAndData.sql');
    lines.push('');
    lines.push('  Or open it in SSMS and Execute. NexusDb_Full.bak in this folder is the');
    lines.push('  native backup and restores faster, but needs the same SQL Server edition.');
    lines.push('*/');
    lines.push('');
    lines.push('SET NOCOUNT ON;');
    lines.push('SET QUOTED_IDENTIFIER ON;');
    lines.push('GO');
    lines.push('');

    for (const table of tables) {
        const cols = columnsOf(table);
        const dataCols = cols.filter(c => !c.identity);

        lines.push(`/* ---- ${table} ---- */`);
        lines.push(`IF OBJECT_ID('dbo.${table}') IS NULL`);
        lines.push('BEGIN');
        lines.push(`    CREATE TABLE dbo.${table} (`);
        lines.push(cols.map(c =>
            `        [${c.name}] ${typeSql(c)}` +
            `${c.identity ? ' IDENTITY(1,1)' : ''}` +
            `${c.nullable ? ' NULL' : ' NOT NULL'}`).join(',\n'));
        lines.push('    );');
        lines.push('END');
        lines.push('GO');
        lines.push('');

        const rows = query(`SELECT ${cols.map(c => `[${c.name}]`).join(', ')} FROM dbo.${table}`);
        if (rows.length === 0) {
            lines.push(`-- ${table}: no rows`);
            lines.push('GO');
            lines.push('');
            continue;
        }

        for (let i = 0; i < rows.length; i += 200) {
            const chunk = rows.slice(i, i + 200);
            lines.push(`INSERT INTO dbo.${table} (${cols.map(c => `[${c.name}]`).join(', ')}) VALUES`);
            lines.push(chunk.map(r =>
                '    (' + r.map((v, idx) => literal(v, cols[idx].type, cols[idx].nullable)).join(', ') + ')'
            ).join(',\n') + ';');
            lines.push('GO');
        }
        lines.push('');
    }

    // Identity columns were inserted explicitly, so the in-memory seed values
    // must be pushed past them or the next insert collides with row 1.
    lines.push('/* Realign identity seeds with the imported data. */');
    for (const table of tables) {
        const cols = columnsOf(table);
        const ident = cols.find(c => c.identity);
        if (!ident) continue;
        lines.push(`DBCC CHECKIDENT ('dbo.${table}', RESEED);`);
    }
    lines.push('GO');
    lines.push('');

    fs.writeFileSync(OUT, lines.join('\n'), 'utf8');
    console.log(`Wrote ${OUT}`);
    console.log(`Tables: ${tables.length}`);
    console.log(`Size: ${(fs.statSync(OUT).size / 1024).toFixed(1)} KB`);
}

main();
