 SELECT
    n.nspname as SchemaName,
    f.proname as FunctionName,
    (case pg_get_function_identity_arguments(f.oid) when '' then null else pg_get_function_identity_arguments(f.oid) end) as FunctionArgumentsAsString,
    (select t.typname::text from pg_type t where t.oid = f.prorettype) as FunctionReturnTypeName,

    pg_get_functiondef(f.oid) as FunctionDefinition,

    (SELECT d.description from pg_description d where d.classoid = 'pg_proc'::regclass and f.OID = d.objoid) as FunctionComment
    
    FROM pg_catalog.pg_proc f
    INNER JOIN pg_catalog.pg_namespace n ON (f.pronamespace = n.oid)
    where f.prokind = 'f'
    and n.nspname != 'information_schema'
    AND n.nspname !~~ 'pg_%';