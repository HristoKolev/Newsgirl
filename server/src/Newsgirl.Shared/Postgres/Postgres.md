
* unify the metadata models between the generator and the shared library.

* document it.

* find out when to prepare commands.




















































# NpgsqlConnectionExtensions

The `NpgsqlConnectionExtensions` class provides extension methods for `NpgsqlConnection` objects that perform high level database operations that don't require information about the specific database schema.

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

* Helper methods:
  * `GetSetters<T>`: 
  * `GetGetters<T>`: 