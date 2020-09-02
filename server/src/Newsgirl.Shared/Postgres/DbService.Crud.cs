namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;

    // ReSharper disable once UnusedTypeParameter
    public partial class DbService<TPocos>
    {
        public Task<int> BulkInsert<T>(IEnumerable<T> pocos, CancellationToken cancellationToken = default)
            where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();
            var columns = metadata.Columns;

            var sqlBuilder = new StringBuilder(128);

            // STATEMENT HEADER
            sqlBuilder.Append("INSERT INTO \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" (");

            bool headerFirstRun = true;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                if (!column.IsPrimaryKey)
                {
                    if (headerFirstRun)
                    {
                        sqlBuilder.Append("\"");
                        headerFirstRun = false;
                    }
                    else
                    {
                        sqlBuilder.Append(", \"");
                    }

                    sqlBuilder.Append(column.ColumnName);
                    sqlBuilder.Append('"');
                }
            }

            sqlBuilder.Append(") VALUES ");

            var allParameters = new List<NpgsqlParameter>();

            // PARAMETERS
            int paramIndex = 0;

            bool recordsFirstRun = true;

            foreach (var record in pocos)
            {
                if (!recordsFirstRun)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append("\n(");
                recordsFirstRun = false;

                var parameters = metadata.GenerateParameters(record);

                allParameters.AddRange(parameters);

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                    {
                        sqlBuilder.Append(", ");
                    }

                    int currentIndex = paramIndex++;
                    var parameter = parameters[i];
                    parameter.ParameterName = "p" + currentIndex;

                    sqlBuilder.Append("@p");
                    sqlBuilder.Append(currentIndex);
                }

                sqlBuilder.Append(")");
            }

            sqlBuilder.Append(";");

            string sql = sqlBuilder.ToString();

            return this.connection.ExecuteNonQuery(sql, allParameters, cancellationToken);
        }

        public Task<int> Delete<T>(T model, CancellationToken cancellationToken = default)
            where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = metadata.GetPrimaryKey(model);

            return this.Delete<T>(pk, cancellationToken);
        }

        public Task<int> Delete<T>(int[] ids, CancellationToken cancellationToken = default)
            where T : IPoco<T>
        {
            if (ids.Length == 0)
            {
                return Task.FromResult(0);
            }

            var metadata = DbCodeGenerator.GetMetadata<T>();

            string tableSchema = metadata.TableSchema;
            string tableName = metadata.TableName;
            string primaryKeyName = metadata.PrimaryKeyColumnName;

            var parameters = new[]
            {
                this.CreateParameter("pk", ids),
            };

            string sql = $"DELETE FROM \"{tableSchema}\".\"{tableName}\" WHERE \"{primaryKeyName}\" = any(@pk);";

            return this.connection.ExecuteNonQuery(sql, parameters, cancellationToken);
        }

        public Task<int> Delete<T>(int id, CancellationToken cancellationToken = default)
            where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            string tableSchema = metadata.TableSchema;
            string tableName = metadata.TableName;
            string primaryKeyName = metadata.PrimaryKeyColumnName;

            var parameters = new[]
            {
                this.CreateParameter("pk", id),
            };

            string sql = $"DELETE FROM \"{tableSchema}\".\"{tableName}\" WHERE \"{primaryKeyName}\" = @pk;";

            return this.connection.ExecuteNonQuery(sql, parameters, cancellationToken);
        }

        public async Task<int> Insert<T>(T model, CancellationToken cancellationToken = default)
            where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = await this.InsertWithoutMutating(model, cancellationToken);

            metadata.SetPrimaryKey(model, pk);

            return pk;
        }

        public Task<int> InsertWithoutMutating<T>(T model, CancellationToken cancellationToken = default)
            where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            var (columnNames, parameters) = metadata.GetAllColumns(model);

            var sqlBuilder = new StringBuilder(128);

            // STATEMENT HEADER
            sqlBuilder.Append("INSERT INTO \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" (");

            for (int i = 0; i < columnNames.Length; i++)
            {
                if (i != 0)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append('"');
                sqlBuilder.Append(columnNames[i]);
                sqlBuilder.Append('"');
            }

            sqlBuilder.Append(")\n VALUES (");

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append("@p");
                sqlBuilder.Append(i);

                parameters[i].ParameterName = "p" + i;
            }

            // STATEMENT FOOTER
            sqlBuilder.Append(") RETURNING \"");
            sqlBuilder.Append(metadata.PrimaryKeyColumnName);
            sqlBuilder.Append("\";");

            string sql = sqlBuilder.ToString();

            return this.connection.ExecuteScalar<int>(sql, parameters, cancellationToken);
        }

        public async Task<int> Save<T>(T model, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            if (metadata.IsNew(model))
            {
                return await this.Insert(model, cancellationToken);
            }

            await this.Update(model, cancellationToken);

            return metadata.GetPrimaryKey(model);
        }

        public Task<int> Update<T>(T model, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = metadata.GetPrimaryKey(model);

            if (pk == default)
            {
                throw new ApplicationException("Cannot update a model with primary key of 0.");
            }

            var (columnNames, parameters) = metadata.GetAllColumns(model);

            if (columnNames.Length == 0)
            {
                return Task.FromResult(0); // nothing to update?
            }

            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append("UPDATE \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" SET ");

            for (int i = 0; i < columnNames.Length; i++)
            {
                string columnName = columnNames[i];
                var parameter = parameters[i];

                string paramName = "@p" + i;

                sqlBuilder.Append("\n\"");
                sqlBuilder.Append(columnName);
                sqlBuilder.Append("\" = ");
                sqlBuilder.Append(paramName);

                if (i != columnNames.Length - 1)
                {
                    sqlBuilder.Append(',');
                }

                parameter.ParameterName = paramName;
            }

            sqlBuilder.Append("\nWHERE \"");
            sqlBuilder.Append(metadata.PrimaryKeyColumnName);
            sqlBuilder.Append("\" = @pk;");

            string sql = sqlBuilder.ToString();

            var allParameters = parameters.Concat(new[]
            {
                this.CreateParameter("pk", pk),
            });

            return this.connection.ExecuteNonQuery(sql, allParameters, cancellationToken);
        }

        public async Task<int> UpdateChangesOnly<T>(T model, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>, new()
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = metadata.GetPrimaryKey(model);

            if (pk == default)
            {
                throw new ApplicationException("Cannot update a model with primary key of 0.");
            }

            string selectSql =
                $"select * from \"{metadata.TableSchema}\".\"{metadata.TableName}\" where \"{metadata.PrimaryKeyColumnName}\" = @pk FOR UPDATE;";

            var selectParameters = new[]
            {
                this.CreateParameter("pk", pk),
            };

            var currentInstance = await this.connection.QueryOne<T>(selectSql, selectParameters, cancellationToken);

            if (currentInstance == null)
            {
                throw new ApplicationException("Cannot update record: record does not exists."); // no record to update?
            }

            var (names, parameters) = metadata.GetColumnChanges(currentInstance, model);

            if (names.Count == 0)
            {
                return 0; // nothing to update?
            }

            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append("UPDATE \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" SET ");

            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                var parameter = parameters[i];

                string paramName = "@p" + i;

                sqlBuilder.Append('\n');
                sqlBuilder.Append(name);
                sqlBuilder.Append(" = ");
                sqlBuilder.Append(paramName);

                if (i != names.Count - 1)
                {
                    sqlBuilder.Append(',');
                }

                parameter.ParameterName = paramName;
            }

            sqlBuilder.Append("\nWHERE ");
            sqlBuilder.Append(metadata.PrimaryKeyColumnName);
            sqlBuilder.Append(" = @pk;");

            parameters.Add(this.CreateParameter("pk", pk));

            string sql = sqlBuilder.ToString();

            return await this.connection.ExecuteNonQuery(sql, parameters, cancellationToken);
        }

        public Task<T> FindByID<T>(int id, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>, new()
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            string tableSchema = metadata.TableSchema;
            string tableName = metadata.TableName;
            string primaryKeyName = metadata.PrimaryKeyColumnName;

            var parameters = new[]
            {
                this.CreateParameter("pk", id),
            };

            string sql = $"SELECT * FROM \"{tableSchema}\".\"{tableName}\" WHERE \"{primaryKeyName}\" = @pk;";

            return this.connection.QueryOne<T>(sql, parameters, cancellationToken);
        }

        public async Task Copy<T>(IEnumerable<T> pocos)
            where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();
            var columns = metadata.Columns;

            var copyHeaderBuilder = new StringBuilder(128);

            // STATEMENT HEADER
            copyHeaderBuilder.Append("COPY \"");
            copyHeaderBuilder.Append(metadata.TableSchema);
            copyHeaderBuilder.Append("\".\"");
            copyHeaderBuilder.Append(metadata.TableName);
            copyHeaderBuilder.Append("\" (");

            bool headerFirstRun = true;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                if (!column.IsPrimaryKey)
                {
                    if (headerFirstRun)
                    {
                        copyHeaderBuilder.Append("\"");
                        headerFirstRun = false;
                    }
                    else
                    {
                        copyHeaderBuilder.Append(", \"");
                    }

                    copyHeaderBuilder.Append(column.ColumnName);
                    copyHeaderBuilder.Append('"');
                }
            }

            copyHeaderBuilder.Append(") FROM STDIN (FORMAT BINARY)");

            await using (var importer = this.connection.BeginBinaryImport(copyHeaderBuilder.ToString()))
            {
                foreach (var poco in pocos)
                {
                    await importer.StartRowAsync();

                    metadata.WriteToImporter(importer, poco);
                }

                await importer.CompleteAsync();
            }
        }
    }
}
