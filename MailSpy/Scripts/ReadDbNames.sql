SELECT db.name AS Value
FROM master.dbo.sysdatabases db
WHERE db.name NOT IN ('master', 'tempdb', 'model', 'msdb', 'DBA');