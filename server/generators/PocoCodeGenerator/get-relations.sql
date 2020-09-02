
  ((SELECT
      t.tablename as TableName,
      n.nspname AS TableSchema,
      a.attname as ColumnName,
      pg_catalog.format_type(a.atttypid, NULL) AS DbDataType,
      pg_catalog.col_description(a.attrelid, a.attnum) AS ColumnComment,
      (a.attnotnull = FALSE) AS IsNullable,
      p.conname AS PrimaryKeyConstraintName,
      f.conname AS ForeignKeyConstraintName,
      fc.relname as ForeignKeyReferenceTableName,
      CASE WHEN pg_catalog.pg_get_constraintdef(f.oid) LIKE 'FOREIGN KEY %'
              THEN substring(pg_catalog.pg_get_constraintdef(f.oid),
                  position('(' in substring(pg_catalog.pg_get_constraintdef(f.oid), 14))+14, position(')'
                  in substring(pg_catalog.pg_get_constraintdef(f.oid), position('(' in
                  substring(pg_catalog.pg_get_constraintdef(f.oid), 14))+14))-1) END AS ForeignKeyReferenceColumnName,
      fn.nspname as ForeignKeyReferenceSchemaName,
      FALSE as IsViewColumn

      FROM pg_catalog.pg_class c
      JOIN pg_catalog.pg_tables t ON c.relname = t.tablename
      JOIN pg_catalog.pg_attribute a ON c.oid = a.attrelid AND a.attnum > 0
      JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
      LEFT JOIN pg_catalog.pg_constraint p ON p.contype = 'p'::char AND p.conrelid = c.oid AND (a.attnum = ANY (p.conkey))
      LEFT JOIN pg_catalog.pg_constraint f ON f.contype = 'f'::char AND f.conrelid = c.oid AND (a.attnum = ANY (f.conkey))
      LEFT JOIN pg_catalog.pg_class fc on fc.oid = f.confrelid
      LEFT JOIN pg_catalog.pg_namespace fn on fn.oid = fc.relnamespace

      WHERE a.atttypid <> 0::oid AND (n.nspname != 'information_schema' AND n.nspname NOT LIKE 'pg_%') AND c.relkind = 'r')
  UNION
  (SELECT
      t.viewname as TableName,
      n.nspname AS TableSchema,
      a.attname as ColumnName,
      pg_catalog.format_type(a.atttypid, NULL) AS DbDataType,
      pg_catalog.col_description(a.attrelid, a.attnum) AS ColumnComment,
      (a.attnotnull = FALSE) AS IsNullable,
      null AS PrimaryKeyConstraintName,
      null AS ForeignKeyConstraintName,
      null as ForeignKeyReferenceTableName,
      null as ForeignKeyReferenceColumnName,
      null as ForeignKeyReferenceSchemaName,
      true as IsViewColumn

      FROM pg_catalog.pg_class c
      JOIN pg_catalog.pg_views t ON c.relname = t.viewname
      JOIN pg_catalog.pg_attribute a ON c.oid = a.attrelid AND a.attnum > 0
      JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace

      WHERE a.atttypid <> 0::oid
        AND (n.nspname != 'information_schema' AND n.nspname NOT LIKE 'pg_%')
        and not (n.nspname = 'public' and t.viewname = 'db_columns')
  )) ORDER BY TableSchema, IsViewColumn, TableName, ColumnName;
