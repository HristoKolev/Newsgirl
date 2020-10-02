namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;
    using Postgres;
    using Testing;
    using Xunit;

    public class ConnectionExtensionsTest : TestPocosDatabaseTest
    {
        [Fact]
        public async Task ExecuteNonQuery1()
        {
            int number = 123;

            var parameters = new[]
            {
                this.DbConnection.CreateParameter("n", number),
            };

            await this.DbConnection.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", parameters, CancellationToken.None);

            int result = await this.DbConnection.ExecuteScalar<int>(
                "select test_number from public.test2 where test_name = 'enq_test';"
            );

            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteNonQuery2()
        {
            int number = 123;

            await this.DbConnection.ExecuteNonQuery($@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), {number});
            ");

            int result = await this.DbConnection.ExecuteScalar<int>(
                "select test_number from public.test2 where test_name = 'enq_test';"
            );

            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteNonQuery3()
        {
            int number = 123;

            await this.DbConnection.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", this.DbConnection.CreateParameter("n", number));

            int result = await this.DbConnection.ExecuteScalar<int>(
                "select test_number from public.test2 where test_name = 'enq_test';"
            );

            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteScalar1()
        {
            int number = 123;
            var parameters = new[] {this.DbConnection.CreateParameter("n", number)};
            int result = await this.DbConnection.ExecuteScalar<int>("select :n;", parameters, CancellationToken.None);
            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteScalar2()
        {
            int number = 123;
            int result = await this.DbConnection.ExecuteScalar<int>($"select {number};");
            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteScalar3()
        {
            int number = 123;
            int result = await this.DbConnection.ExecuteScalar<int>("select :n;", this.DbConnection.CreateParameter("n", number));
            Assert.Equal(number, result);
        }

        [Fact]
        public async Task Query1()
        {
            int number = 3;

            var parameters = new[]
            {
                this.DbConnection.CreateParameter("n", number),
            };

            var result = await this.DbConnection.Query<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                parameters,
                CancellationToken.None
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task Query2()
        {
            int number = 3;

            var result = await this.DbConnection.Query<Test2Model>(
                $"select test_id, test_name, test_number from test2 where test_number = {number};"
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task Query3()
        {
            int number = 3;

            var result = await this.DbConnection.Query<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                this.DbConnection.CreateParameter("n", number)
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task QueryOne1()
        {
            int number = 3;

            var parameters = new[]
            {
                this.DbConnection.CreateParameter("n", number),
            };

            var result = await this.DbConnection.QueryOne<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                parameters,
                CancellationToken.None
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task QueryOne2()
        {
            int number = 3;

            var result = await this.DbConnection.QueryOne<Test2Model>(
                $"select test_id, test_name, test_number from test2 where test_number = {number};"
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task QueryOne3()
        {
            int number = 3;

            var result = await this.DbConnection.QueryOne<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                this.DbConnection.CreateParameter("n", number)
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task ExecuteNonQuery_throws_on_null_connection()
        {
            await Snapshot.MatchError(async () =>
            {
                await NpgsqlConnectionExtensions.ExecuteNonQuery(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteNonQuery_throws_on_null_sql()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.ExecuteNonQuery(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteNonQuery_throws_on_null_parameters()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.ExecuteNonQuery("select 1;", null, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_null_connection()
        {
            await Snapshot.MatchError(async () =>
            {
                await NpgsqlConnectionExtensions.ExecuteScalar<int>(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_null_sql()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.ExecuteScalar<int>(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_null_parameters()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.ExecuteScalar<int>("select 1;", null, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_empty_result_set()
        {
            Exception exception = null;

            try
            {
                await this.DbConnection.ExecuteScalar<int>("select 1 where false;");
            }
            catch (Exception err)
            {
                exception = err;
            }

            Snapshot.MatchError(exception);
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_no_columns()
        {
            Exception exception = null;

            try
            {
                await this.DbConnection.ExecuteScalar<int>("create table test22 (); select * from test22;");
            }
            catch (Exception err)
            {
                exception = err;
            }

            Snapshot.MatchError(exception);
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_more_that_one_column()
        {
            Exception exception = null;

            try
            {
                await this.DbConnection.ExecuteScalar<int>("select 1,1;");
            }
            catch (Exception err)
            {
                exception = err;
            }

            Snapshot.MatchError(exception);
        }

        [Fact]
        public async Task ExecuteScalar_throws_on_more_that_one_row()
        {
            Exception exception = null;

            try
            {
                await this.DbConnection.ExecuteScalar<int>("select * from (values (1), (2)) as x;");
            }
            catch (Exception err)
            {
                exception = err;
            }

            Snapshot.MatchError(exception);
        }

        [Fact]
        public async Task ExecuteScalar_throws_when_null_is_selected_for_value_type()
        {
            Exception exception = null;

            try
            {
                await this.DbConnection.ExecuteScalar<int>("select null;");
            }
            catch (Exception err)
            {
                exception = err;
            }

            Snapshot.MatchError(exception);
        }

        [Fact]
        public async Task ExecuteScalar_can_return_null()
        {
            string x = await this.DbConnection.ExecuteScalar<string>("select null;");

            Assert.Null(x);
        }

        [Fact]
        public async Task Query_throws_on_null_connection()
        {
            await Snapshot.MatchError(async () =>
            {
                await NpgsqlConnectionExtensions.Query<TestRow>(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task Query_throws_on_null_sql()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.Query<TestRow>(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task Query_throws_on_null_parameters()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.Query<TestRow>("select 1;", null, CancellationToken.None);
            });
        }

        [Fact]
        public async Task Query_handles_null_values()
        {
            var rows = await this.DbConnection.Query<TestRow>("select null as col1, null as col2;");
            var row = rows.First();

            Assert.Null(row.Col1);
            Assert.Null(row.Col2);
        }

        [Fact]
        public async Task QueryOne_throws_on_null_connection()
        {
            await Snapshot.MatchError(async () =>
            {
                await NpgsqlConnectionExtensions.QueryOne<TestRow>(null, "select 1;", Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task QueryOne_throws_on_null_sql()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.QueryOne<TestRow>(null, Array.Empty<NpgsqlParameter>(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task QueryOne_throws_on_null_parameters()
        {
            await Snapshot.MatchError(async () =>
            {
                await this.DbConnection.QueryOne<TestRow>("select 1;", null, CancellationToken.None);
            });
        }

        [Fact]
        public async Task QueryOne_returns_null_on_no_rows()
        {
            var row = await this.DbConnection.QueryOne<TestRow>("select 1 where false;");

            Assert.Null(row);
        }
        
        [Fact]
        public async Task QueryOne_throws_on_more_than_one_row()
        {
            
            Exception exception = null;

            try
            {
                await this.DbConnection.QueryOne<TestRow>("select * from (values (1, 2), (3, 4)) as x(col1, col2);");
            }
            catch (Exception err)
            {
                exception = err;
            }

            Snapshot.MatchError(exception);
        }
    }

    public class TransactionConnectionExtensionsTest
    {
        [Fact]
        public async Task ExecuteInTransactionAndCommit_works_with_non_default_CT()
        {
            var builder = new NpgsqlConnectionStringBuilder(TestHelper.TestConfig.DbTestConnectionString)
            {
                Enlist = false,
                Pooling = false,
            };
            
            await using (var db = new NpgsqlConnection(builder.ToString()))
            {
                //await db.ExecuteNonQuery("select pg_sleep(1);");

                await db.OpenAsync();
                
                var cts = new CancellationTokenSource();
                cts.CancelAfter(6000);
                
                await db.ExecuteInTransactionAndCommit(async () =>
                {
                    await db.ExecuteNonQuery("select pg_sleep(5);");
                }, cts.Token);
            }
        }
    }

    public class DbServiceTest : TestPocosDatabaseTest
    {
        [Fact]
        public async Task ExecuteNonQuery1()
        {
            int number = 123;

            var parameters = new[]
            {
                this.Db.CreateParameter("n", number),
            };

            await this.Db.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", parameters, CancellationToken.None);

            int result = await this.Db.ExecuteScalar<int>(
                "select test_number from public.test2 where test_name = 'enq_test';"
            );

            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteNonQuery2()
        {
            int number = 123;

            await this.Db.ExecuteNonQuery($@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), {number});
            ");

            int result = await this.Db.ExecuteScalar<int>(
                "select test_number from public.test2 where test_name = 'enq_test';"
            );

            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteNonQuery3()
        {
            int number = 123;

            await this.Db.ExecuteNonQuery(@"
                INSERT into public.test2 
                (test_name, test_date, test_number) values
                ('enq_test', now(), :n);
            ", this.Db.CreateParameter("n", number));

            int result = await this.Db.ExecuteScalar<int>(
                "select test_number from public.test2 where test_name = 'enq_test';"
            );

            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteScalar1()
        {
            int number = 123;
            var parameters = new[] {this.Db.CreateParameter("n", number)};
            int result = await this.Db.ExecuteScalar<int>("select :n;", parameters, CancellationToken.None);
            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteScalar2()
        {
            int number = 123;
            int result = await this.Db.ExecuteScalar<int>($"select {number};");
            Assert.Equal(number, result);
        }

        [Fact]
        public async Task ExecuteScalar3()
        {
            int number = 123;
            int result = await this.Db.ExecuteScalar<int>("select :n;", this.Db.CreateParameter("n", number));
            Assert.Equal(number, result);
        }

        [Fact]
        public async Task Query1()
        {
            int number = 3;

            var parameters = new[]
            {
                this.Db.CreateParameter("n", number),
            };

            var result = await this.Db.Query<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                parameters,
                CancellationToken.None
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task Query2()
        {
            int number = 3;

            var result = await this.Db.Query<Test2Model>(
                $"select test_id, test_name, test_number from test2 where test_number = {number};"
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task Query3()
        {
            int number = 3;

            var result = await this.Db.Query<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                this.Db.CreateParameter("n", number)
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task QueryOne1()
        {
            int number = 3;

            var parameters = new[]
            {
                this.Db.CreateParameter("n", number),
            };

            var result = await this.Db.QueryOne<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                parameters,
                CancellationToken.None
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task QueryOne2()
        {
            int number = 3;

            var result = await this.Db.QueryOne<Test2Model>(
                $"select test_id, test_name, test_number from test2 where test_number = {number};"
            );

            Snapshot.Match(result);
        }

        [Fact]
        public async Task QueryOne3()
        {
            int number = 3;

            var result = await this.Db.QueryOne<Test2Model>(
                "select test_id, test_name, test_number from test2 where test_number = :n;",
                this.Db.CreateParameter("n", number)
            );

            Snapshot.Match(result);
        }
    }

    public class Test2Model
    {
        public DateTime TestDate { get; set; }

        public int TestID { get; set; }

        public string TestName { get; set; }

        public int TestNumber { get; set; }
    }

    public class TestRow
    {
        public string Col1 { get; set; }

        public string Col2 { get; set; }
    }
}
