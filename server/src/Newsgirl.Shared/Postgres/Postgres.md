
# NpgsqlConnectionExtensions

The `NpgsqlConnectionExtensions` class provides extension methods that perform high level database operations that don't require information about the specific database schema.

Extension methods can be divided in several categories:

* `NpgsqlCommand` method equivalents:
  * `ExecuteNonQuery`: Runs a parameterized query and returns the number of affected rows.  
  * `ExecuteScalar<T>`: Runs a parameterized query and returns the value of the first column of the first row.


* Methods that read rows into CLR classes:
  * `Query<T>`: Runs a parameterized query and returns a list of all rows read into objects of type `T`.
  * `QueryOne<T>`: Runs a parameterized query and returns a single row read into an object of type `T`.


* Methods that abstract away the `NpgsqlTransaction` API:
  * `ExecuteInTransaction`: Invokes the given delegate instance in a transaction. You have to commit the transaction manually.
  * `ExecuteInTransactionAndCommit`: Invokes the given delegate instance in a transaction and commits it automatically.


* `CreateParameter`: Creates `NpgsqlParameter` and `NpgsqlParameter<T>` objects with given arguments.


* `EnsureOpenState`: Opens the connection if it's not in an opened state.

* Helper methods.

# DbService

The `DbService<TPocos>` is used to perform database operations that require knowledge about the specific database schema.

While the `DbService<TPocos>` has a dependency on `NpgsqlConnection` it does not own that resource, disposing it will not dispose the underlying connection.

The `TPocos` type is the part that differs from database to database. It contains a property of type `IQueryable<TPoco>` for every table to be used in linq queries.

Each `TPoco` class has is generated using the columns of a single table and represents that table in CLR code.

## Operations:

* Most methods from `NpgsqlConnectionExtensions`.

* `FindByID<TPoco>(int id)`: Reads a single record by it's primary key value and returns it as an object of type `T`.

* `Insert<TPoco>(TPoco obj)`: Inserts a record.
* `Update<TPoco>(TPoco obj)`: Updates a record.
* `Save<TPoco>(TPoco obj)`: Inserts or updates a record depending on if the record's primary key value is grater than `0` or not.
* `Delete<TPoco>(TPoco obj)`: Deletes a record.
* `Delete<TPoco>(int id)`: Deletes the record that has the given primary key value.
* `Delete<TPoco>(int[] ids)`: Deletes several records from a given table by their primary key values.

* `BulkInsert<TPoco>(IEnumerable<TPoco> pocos)`: Inserts several records into a table in single query.
* `Copy<TPoco>(IEnumerable<TPoco> pocos)`: Inserts several records into a table using the postgresql binary COPY API.

# Future considerations
 
* find out when to prepare commands.
