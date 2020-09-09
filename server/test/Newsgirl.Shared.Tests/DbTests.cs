namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Postgres;
    using Xunit;

    public class Test1Test : TestPocosDatabaseTest
    {
        [Theory]
        [ClassData(typeof(GeneratedData<Test1Poco>))]
        public async Task Crud(Test1Poco poco)
        {
            int id = await this.Db.Insert(poco);

            var readFromDb = await this.Db.FindByID<Test1Poco>(id);

            Assert.Equal(poco.TestBigint1, readFromDb.TestBigint1);
            Assert.Equal(poco.TestBigint2, readFromDb.TestBigint2);
            Assert.Equal(poco.TestBoolean1, readFromDb.TestBoolean1);
            Assert.Equal(poco.TestBoolean2, readFromDb.TestBoolean2);
            Assert.Equal(poco.TestChar1, readFromDb.TestChar1);
            Assert.Equal(poco.TestChar2, readFromDb.TestChar2);
            Assert.Equal(poco.TestDate1, readFromDb.TestDate1);
            Assert.Equal(poco.TestDate2, readFromDb.TestDate2);
            Assert.Equal(poco.TestDecimal1, readFromDb.TestDecimal1);
            Assert.Equal(poco.TestDecimal2, readFromDb.TestDecimal2);
            Assert.Equal(poco.TestDouble1, readFromDb.TestDouble1);
            Assert.Equal(poco.TestDouble2, readFromDb.TestDouble2);
            Assert.Equal(poco.TestID, readFromDb.TestID);
            Assert.Equal(poco.TestInteger1, readFromDb.TestInteger1);
            Assert.Equal(poco.TestInteger2, readFromDb.TestInteger2);
            Assert.Equal(poco.TestName1, readFromDb.TestName1);
            Assert.Equal(poco.TestName2, readFromDb.TestName2);
            Assert.Equal(poco.TestReal1, readFromDb.TestReal1);
            Assert.Equal(poco.TestReal2, readFromDb.TestReal2);
            Assert.Equal(poco.TestText1, readFromDb.TestText1);
            Assert.Equal(poco.TestText2, readFromDb.TestText2);
            Assert.Equal(poco.TestTimestamp1, readFromDb.TestTimestamp1);
            Assert.Equal(poco.TestTimestamp2, readFromDb.TestTimestamp2);

            int updatedId = await this.Db.Update(poco);

            Assert.Equal(id, updatedId);

            await this.Db.Delete(poco);
        }

        [Theory]
        [ClassData(typeof(GeneratedBulkData<Test1Poco>))]
        public async Task BulkInsert(List<Test1Poco> poco)
        {
            await this.Db.BulkInsert(poco);
        }

        [Theory]
        [ClassData(typeof(GeneratedBulkData<Test1Poco>))]
        public void GetColumnChanges(List<Test1Poco> pocos)
        {
            var getColumnChanges = DbMetadata.Test1PocoMetadata.GetColumnChanges;

            var columns = DbMetadata.Test1PocoMetadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();
            var getters = DbCodeGenerator.GenerateGetters<Test1Poco>();

            var allColumnNames = new HashSet<string>(columns.Select(x => x.ColumnName));

            foreach (var (instance1, instance2) in pocos.Zip(Enumerable.Reverse(pocos), (x, y) => (x, y)))
            {
                var (columnNames, parameters) = getColumnChanges(instance1, instance2);

                Assert.Equal(parameters.Count, columnNames.Count);
                Assert.True(columnNames.Count <= columns.Length);

                foreach (string columnName in columnNames)
                {
                    Assert.Contains(columnName, allColumnNames);
                }

                foreach (var column in columns)
                {
                    var getter = getters[column.ColumnName];

                    var value1 = getter(instance1);
                    var value2 = getter(instance2);

                    if (DbCodeGenerator.StupidEquals(value1, value2))
                    {
                        Assert.DoesNotContain(column.ColumnName, columnNames);
                    }
                    else
                    {
                        Assert.Contains(column.ColumnName, columnNames);
                        int index = columnNames.IndexOf(column.ColumnName);
                        var parameter = parameters[index];

                        Assert.Equal(column.PropertyType.NpgsqlDbType, parameter.NpgsqlDbType);

                        Assert.Equal(value2 ?? DBNull.Value, parameter.Value);
                    }
                }
            }
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test1Poco>))]
        // ReSharper disable once CyclomaticComplexity
        public void GenerateParameters(Test1Poco poco)
        {
            var getParameters = DbMetadata.Test1PocoMetadata.GenerateParameters;

            var parameters = getParameters(poco);

            var columns = DbMetadata.Test1PocoMetadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();
            var getters = DbCodeGenerator.GenerateGetters<Test1Poco>();

            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var getter = getters[column.ColumnName];

                var parameter = parameters[i];

                Assert.Equal(getter(poco) ?? DBNull.Value, parameter.Value);
            }
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test1Poco>))]
        // ReSharper disable once CyclomaticComplexity
        public void GetAllColumns(Test1Poco poco)
        {
            var getAllColumns = DbMetadata.Test1PocoMetadata.GetAllColumns;

            var (columnNames, parameters) = getAllColumns(poco);

            var columns = DbMetadata.Test1PocoMetadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();
            var getters = DbCodeGenerator.GenerateGetters<Test1Poco>();

            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var getter = getters[column.ColumnName];
                var parameter = parameters[i];

                Assert.Equal(columnNames[i], column.ColumnName);

                Assert.Equal(getter(poco) ?? DBNull.Value, parameter.Value);
            }
        }

        [Theory]
        [ClassData(typeof(GeneratedBulkData<Test1Poco>))]
        public Task Copy(List<Test1Poco> pocos)
        {
            return this.Db.Copy(pocos);
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test1Poco>))]
        public void Getters(Test1Poco poco)
        {
            var getters = DbCodeGenerator.GenerateGetters<Test1Poco>();

            Assert.Equal(poco.TestBigint1, getters["test_bigint1"](poco));
            Assert.Equal(poco.TestBigint2, getters["test_bigint2"](poco));
            Assert.Equal(poco.TestBoolean1, getters["test_boolean1"](poco));
            Assert.Equal(poco.TestBoolean2, getters["test_boolean2"](poco));
            Assert.Equal(poco.TestChar1, getters["test_char1"](poco));
            Assert.Equal(poco.TestChar2, getters["test_char2"](poco));
            Assert.Equal(poco.TestDate1, getters["test_date1"](poco));
            Assert.Equal(poco.TestDate2, getters["test_date2"](poco));
            Assert.Equal(poco.TestDecimal1, getters["test_decimal1"](poco));
            Assert.Equal(poco.TestDecimal2, getters["test_decimal2"](poco));
            Assert.Equal(poco.TestDouble1, getters["test_double1"](poco));
            Assert.Equal(poco.TestDouble2, getters["test_double2"](poco));
            Assert.Equal(poco.TestID, getters["test_id"](poco));
            Assert.Equal(poco.TestInteger1, getters["test_integer1"](poco));
            Assert.Equal(poco.TestInteger2, getters["test_integer2"](poco));
            Assert.Equal(poco.TestName1, getters["test_name1"](poco));
            Assert.Equal(poco.TestName2, getters["test_name2"](poco));
            Assert.Equal(poco.TestReal1, getters["test_real1"](poco));
            Assert.Equal(poco.TestReal2, getters["test_real2"](poco));
            Assert.Equal(poco.TestText1, getters["test_text1"](poco));
            Assert.Equal(poco.TestText2, getters["test_text2"](poco));
            Assert.Equal(poco.TestTimestamp1, getters["test_timestamp1"](poco));
            Assert.Equal(poco.TestTimestamp2, getters["test_timestamp2"](poco));
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test1Poco>))]
        public void Setters(Test1Poco poco)
        {
            var setters = DbCodeGenerator.GenerateSetters<Test1Poco>();

            var newObj = new Test1Poco();

            setters["test_bigint1"](newObj, poco.TestBigint1);
            Assert.Equal(poco.TestBigint1, newObj.TestBigint1);

            setters["test_bigint2"](newObj, poco.TestBigint2);
            Assert.Equal(poco.TestBigint2, newObj.TestBigint2);

            setters["test_boolean1"](newObj, poco.TestBoolean1);
            Assert.Equal(poco.TestBoolean1, newObj.TestBoolean1);

            setters["test_boolean2"](newObj, poco.TestBoolean2);
            Assert.Equal(poco.TestBoolean2, newObj.TestBoolean2);

            setters["test_char1"](newObj, poco.TestChar1);
            Assert.Equal(poco.TestChar1, newObj.TestChar1);

            setters["test_char2"](newObj, poco.TestChar2);
            Assert.Equal(poco.TestChar2, newObj.TestChar2);

            setters["test_date1"](newObj, poco.TestDate1);
            Assert.Equal(poco.TestDate1, newObj.TestDate1);

            setters["test_date2"](newObj, poco.TestDate2);
            Assert.Equal(poco.TestDate2, newObj.TestDate2);

            setters["test_decimal1"](newObj, poco.TestDecimal1);
            Assert.Equal(poco.TestDecimal1, newObj.TestDecimal1);

            setters["test_decimal2"](newObj, poco.TestDecimal2);
            Assert.Equal(poco.TestDecimal2, newObj.TestDecimal2);

            setters["test_double1"](newObj, poco.TestDouble1);
            Assert.Equal(poco.TestDouble1, newObj.TestDouble1);

            setters["test_double2"](newObj, poco.TestDouble2);
            Assert.Equal(poco.TestDouble2, newObj.TestDouble2);

            setters["test_id"](newObj, poco.TestID);
            Assert.Equal(poco.TestID, newObj.TestID);

            setters["test_integer1"](newObj, poco.TestInteger1);
            Assert.Equal(poco.TestInteger1, newObj.TestInteger1);

            setters["test_integer2"](newObj, poco.TestInteger2);
            Assert.Equal(poco.TestInteger2, newObj.TestInteger2);

            setters["test_name1"](newObj, poco.TestName1);
            Assert.Equal(poco.TestName1, newObj.TestName1);

            setters["test_name2"](newObj, poco.TestName2);
            Assert.Equal(poco.TestName2, newObj.TestName2);

            setters["test_real1"](newObj, poco.TestReal1);
            Assert.Equal(poco.TestReal1, newObj.TestReal1);

            setters["test_real2"](newObj, poco.TestReal2);
            Assert.Equal(poco.TestReal2, newObj.TestReal2);

            setters["test_text1"](newObj, poco.TestText1);
            Assert.Equal(poco.TestText1, newObj.TestText1);

            setters["test_text2"](newObj, poco.TestText2);
            Assert.Equal(poco.TestText2, newObj.TestText2);

            setters["test_timestamp1"](newObj, poco.TestTimestamp1);
            Assert.Equal(poco.TestTimestamp1, newObj.TestTimestamp1);

            setters["test_timestamp2"](newObj, poco.TestTimestamp2);
            Assert.Equal(poco.TestTimestamp2, newObj.TestTimestamp2);
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test1Poco>))]
        public void Clone(Test1Poco poco)
        {
            var clone = DbMetadata.Test1PocoMetadata.Clone;

            var newObj = clone(poco);

            Assert.NotEqual(poco, newObj);

            Assert.Equal(poco.TestBigint1, newObj.TestBigint1);
            Assert.Equal(poco.TestBigint2, newObj.TestBigint2);
            Assert.Equal(poco.TestBoolean1, newObj.TestBoolean1);
            Assert.Equal(poco.TestBoolean2, newObj.TestBoolean2);
            Assert.Equal(poco.TestChar1, newObj.TestChar1);
            Assert.Equal(poco.TestChar2, newObj.TestChar2);
            Assert.Equal(poco.TestDate1, newObj.TestDate1);
            Assert.Equal(poco.TestDate2, newObj.TestDate2);
            Assert.Equal(poco.TestDecimal1, newObj.TestDecimal1);
            Assert.Equal(poco.TestDecimal2, newObj.TestDecimal2);
            Assert.Equal(poco.TestDouble1, newObj.TestDouble1);
            Assert.Equal(poco.TestDouble2, newObj.TestDouble2);
            Assert.Equal(poco.TestID, newObj.TestID);
            Assert.Equal(poco.TestInteger1, newObj.TestInteger1);
            Assert.Equal(poco.TestInteger2, newObj.TestInteger2);
            Assert.Equal(poco.TestName1, newObj.TestName1);
            Assert.Equal(poco.TestName2, newObj.TestName2);
            Assert.Equal(poco.TestReal1, newObj.TestReal1);
            Assert.Equal(poco.TestReal2, newObj.TestReal2);
            Assert.Equal(poco.TestText1, newObj.TestText1);
            Assert.Equal(poco.TestText2, newObj.TestText2);
            Assert.Equal(poco.TestTimestamp1, newObj.TestTimestamp1);
            Assert.Equal(poco.TestTimestamp2, newObj.TestTimestamp2);
        }
    }

    public class Test2Test : TestPocosDatabaseTest
    {
        [Theory]
        [ClassData(typeof(GeneratedData<Test2Poco>))]
        public async Task Crud(Test2Poco poco)
        {
            int id = await this.Db.Insert(poco);

            var readFromDb = await this.Db.FindByID<Test2Poco>(id);

            Assert.Equal(poco.TestDate, readFromDb.TestDate);
            Assert.Equal(poco.TestID, readFromDb.TestID);
            Assert.Equal(poco.TestName, readFromDb.TestName);

            int updatedId = await this.Db.Update(poco);

            Assert.Equal(id, updatedId);

            await this.Db.Delete(poco);
        }

        [Theory]
        [ClassData(typeof(GeneratedBulkData<Test2Poco>))]
        public async Task BulkInsert(List<Test2Poco> poco)
        {
            await this.Db.BulkInsert(poco);
        }

        [Theory]
        [ClassData(typeof(GeneratedBulkData<Test2Poco>))]
        public void GetColumnChanges(List<Test2Poco> pocos)
        {
            var getColumnChanges = DbMetadata.Test2PocoMetadata.GetColumnChanges;

            var columns = DbMetadata.Test2PocoMetadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();
            var getters = DbCodeGenerator.GenerateGetters<Test2Poco>();

            var allColumnNames = new HashSet<string>(columns.Select(x => x.ColumnName));

            foreach (var (instance1, instance2) in pocos.Zip(Enumerable.Reverse(pocos), (x, y) => (x, y)))
            {
                var (columnNames, parameters) = getColumnChanges(instance1, instance2);

                Assert.Equal(parameters.Count, columnNames.Count);
                Assert.True(columnNames.Count <= columns.Length);

                foreach (string columnName in columnNames)
                {
                    Assert.Contains(columnName, allColumnNames);
                }

                foreach (var column in columns)
                {
                    var getter = getters[column.ColumnName];

                    var value1 = getter(instance1);
                    var value2 = getter(instance2);

                    if (DbCodeGenerator.StupidEquals(value1, value2))
                    {
                        Assert.DoesNotContain(column.ColumnName, columnNames);
                    }
                    else
                    {
                        Assert.Contains(column.ColumnName, columnNames);
                        int index = columnNames.IndexOf(column.ColumnName);
                        var parameter = parameters[index];

                        Assert.Equal(column.PropertyType.NpgsqlDbType, parameter.NpgsqlDbType);

                        Assert.Equal(value2 ?? DBNull.Value, parameter.Value);
                    }
                }
            }
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test2Poco>))]
        // ReSharper disable once CyclomaticComplexity
        public void GenerateParameters(Test2Poco poco)
        {
            var getParameters = DbMetadata.Test2PocoMetadata.GenerateParameters;

            var parameters = getParameters(poco);

            var columns = DbMetadata.Test2PocoMetadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();
            var getters = DbCodeGenerator.GenerateGetters<Test2Poco>();

            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var getter = getters[column.ColumnName];

                var parameter = parameters[i];

                Assert.Equal(getter(poco) ?? DBNull.Value, parameter.Value);
            }
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test2Poco>))]
        // ReSharper disable once CyclomaticComplexity
        public void GetAllColumns(Test2Poco poco)
        {
            var getAllColumns = DbMetadata.Test2PocoMetadata.GetAllColumns;

            var (columnNames, parameters) = getAllColumns(poco);

            var columns = DbMetadata.Test2PocoMetadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();
            var getters = DbCodeGenerator.GenerateGetters<Test2Poco>();

            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var getter = getters[column.ColumnName];
                var parameter = parameters[i];

                Assert.Equal(columnNames[i], column.ColumnName);

                Assert.Equal(getter(poco) ?? DBNull.Value, parameter.Value);
            }
        }

        [Theory]
        [ClassData(typeof(GeneratedBulkData<Test2Poco>))]
        public Task Copy(List<Test2Poco> pocos)
        {
            return this.Db.Copy(pocos);
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test2Poco>))]
        public void Getters(Test2Poco poco)
        {
            var getters = DbCodeGenerator.GenerateGetters<Test2Poco>();

            Assert.Equal(poco.TestDate, getters["test_date"](poco));
            Assert.Equal(poco.TestID, getters["test_id"](poco));
            Assert.Equal(poco.TestName, getters["test_name"](poco));
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test2Poco>))]
        public void Setters(Test2Poco poco)
        {
            var setters = DbCodeGenerator.GenerateSetters<Test2Poco>();

            var newObj = new Test2Poco();

            setters["test_date"](newObj, poco.TestDate);
            Assert.Equal(poco.TestDate, newObj.TestDate);

            setters["test_id"](newObj, poco.TestID);
            Assert.Equal(poco.TestID, newObj.TestID);

            setters["test_name"](newObj, poco.TestName);
            Assert.Equal(poco.TestName, newObj.TestName);
        }

        [Theory]
        [ClassData(typeof(GeneratedData<Test2Poco>))]
        public void Clone(Test2Poco poco)
        {
            var clone = DbMetadata.Test2PocoMetadata.Clone;

            var newObj = clone(poco);

            Assert.NotEqual(poco, newObj);

            Assert.Equal(poco.TestDate, newObj.TestDate);
            Assert.Equal(poco.TestID, newObj.TestID);
            Assert.Equal(poco.TestName, newObj.TestName);
        }
    }

    public class VGenerateSeriesTest : TestPocosDatabaseTest
    {
        [Theory]
        [ClassData(typeof(GeneratedData<VGenerateSeriesPoco>))]
        public void Getters(VGenerateSeriesPoco poco)
        {
            var getters = DbCodeGenerator.GenerateGetters<VGenerateSeriesPoco>();

            Assert.Equal(poco.Num, getters["num"](poco));
        }

        [Theory]
        [ClassData(typeof(GeneratedData<VGenerateSeriesPoco>))]
        public void Setters(VGenerateSeriesPoco poco)
        {
            var setters = DbCodeGenerator.GenerateSetters<VGenerateSeriesPoco>();

            var newObj = new VGenerateSeriesPoco();

            setters["num"](newObj, poco.Num);
            Assert.Equal(poco.Num, newObj.Num);
        }

        [Theory]
        [ClassData(typeof(GeneratedData<VGenerateSeriesPoco>))]
        public void Clone(VGenerateSeriesPoco poco)
        {
            var clone = DbMetadata.VGenerateSeriesPocoMetadata.Clone;

            var newObj = clone(poco);

            Assert.NotEqual(poco, newObj);

            Assert.Equal(poco.Num, newObj.Num);
        }
    }

    public class View1Test : TestPocosDatabaseTest
    {
        [Theory]
        [ClassData(typeof(GeneratedData<View1Poco>))]
        public void Getters(View1Poco poco)
        {
            var getters = DbCodeGenerator.GenerateGetters<View1Poco>();

            Assert.Equal(poco.Test1TestID, getters["test1_test_id"](poco));
            Assert.Equal(poco.Test2TestID, getters["test2_test_id"](poco));
            Assert.Equal(poco.TestBigint1, getters["test_bigint1"](poco));
            Assert.Equal(poco.TestBigint2, getters["test_bigint2"](poco));
            Assert.Equal(poco.TestBoolean1, getters["test_boolean1"](poco));
            Assert.Equal(poco.TestBoolean2, getters["test_boolean2"](poco));
            Assert.Equal(poco.TestChar1, getters["test_char1"](poco));
            Assert.Equal(poco.TestChar2, getters["test_char2"](poco));
            Assert.Equal(poco.TestDate, getters["test_date"](poco));
            Assert.Equal(poco.TestDate1, getters["test_date1"](poco));
            Assert.Equal(poco.TestDate2, getters["test_date2"](poco));
            Assert.Equal(poco.TestDecimal1, getters["test_decimal1"](poco));
            Assert.Equal(poco.TestDecimal2, getters["test_decimal2"](poco));
            Assert.Equal(poco.TestDouble1, getters["test_double1"](poco));
            Assert.Equal(poco.TestDouble2, getters["test_double2"](poco));
            Assert.Equal(poco.TestInteger1, getters["test_integer1"](poco));
            Assert.Equal(poco.TestInteger2, getters["test_integer2"](poco));
            Assert.Equal(poco.TestName, getters["test_name"](poco));
            Assert.Equal(poco.TestName1, getters["test_name1"](poco));
            Assert.Equal(poco.TestName2, getters["test_name2"](poco));
            Assert.Equal(poco.TestReal1, getters["test_real1"](poco));
            Assert.Equal(poco.TestReal2, getters["test_real2"](poco));
            Assert.Equal(poco.TestText1, getters["test_text1"](poco));
            Assert.Equal(poco.TestText2, getters["test_text2"](poco));
            Assert.Equal(poco.TestTimestamp1, getters["test_timestamp1"](poco));
            Assert.Equal(poco.TestTimestamp2, getters["test_timestamp2"](poco));
        }

        [Theory]
        [ClassData(typeof(GeneratedData<View1Poco>))]
        public void Setters(View1Poco poco)
        {
            var setters = DbCodeGenerator.GenerateSetters<View1Poco>();

            var newObj = new View1Poco();

            setters["test1_test_id"](newObj, poco.Test1TestID);
            Assert.Equal(poco.Test1TestID, newObj.Test1TestID);

            setters["test2_test_id"](newObj, poco.Test2TestID);
            Assert.Equal(poco.Test2TestID, newObj.Test2TestID);

            setters["test_bigint1"](newObj, poco.TestBigint1);
            Assert.Equal(poco.TestBigint1, newObj.TestBigint1);

            setters["test_bigint2"](newObj, poco.TestBigint2);
            Assert.Equal(poco.TestBigint2, newObj.TestBigint2);

            setters["test_boolean1"](newObj, poco.TestBoolean1);
            Assert.Equal(poco.TestBoolean1, newObj.TestBoolean1);

            setters["test_boolean2"](newObj, poco.TestBoolean2);
            Assert.Equal(poco.TestBoolean2, newObj.TestBoolean2);

            setters["test_char1"](newObj, poco.TestChar1);
            Assert.Equal(poco.TestChar1, newObj.TestChar1);

            setters["test_char2"](newObj, poco.TestChar2);
            Assert.Equal(poco.TestChar2, newObj.TestChar2);

            setters["test_date"](newObj, poco.TestDate);
            Assert.Equal(poco.TestDate, newObj.TestDate);

            setters["test_date1"](newObj, poco.TestDate1);
            Assert.Equal(poco.TestDate1, newObj.TestDate1);

            setters["test_date2"](newObj, poco.TestDate2);
            Assert.Equal(poco.TestDate2, newObj.TestDate2);

            setters["test_decimal1"](newObj, poco.TestDecimal1);
            Assert.Equal(poco.TestDecimal1, newObj.TestDecimal1);

            setters["test_decimal2"](newObj, poco.TestDecimal2);
            Assert.Equal(poco.TestDecimal2, newObj.TestDecimal2);

            setters["test_double1"](newObj, poco.TestDouble1);
            Assert.Equal(poco.TestDouble1, newObj.TestDouble1);

            setters["test_double2"](newObj, poco.TestDouble2);
            Assert.Equal(poco.TestDouble2, newObj.TestDouble2);

            setters["test_integer1"](newObj, poco.TestInteger1);
            Assert.Equal(poco.TestInteger1, newObj.TestInteger1);

            setters["test_integer2"](newObj, poco.TestInteger2);
            Assert.Equal(poco.TestInteger2, newObj.TestInteger2);

            setters["test_name"](newObj, poco.TestName);
            Assert.Equal(poco.TestName, newObj.TestName);

            setters["test_name1"](newObj, poco.TestName1);
            Assert.Equal(poco.TestName1, newObj.TestName1);

            setters["test_name2"](newObj, poco.TestName2);
            Assert.Equal(poco.TestName2, newObj.TestName2);

            setters["test_real1"](newObj, poco.TestReal1);
            Assert.Equal(poco.TestReal1, newObj.TestReal1);

            setters["test_real2"](newObj, poco.TestReal2);
            Assert.Equal(poco.TestReal2, newObj.TestReal2);

            setters["test_text1"](newObj, poco.TestText1);
            Assert.Equal(poco.TestText1, newObj.TestText1);

            setters["test_text2"](newObj, poco.TestText2);
            Assert.Equal(poco.TestText2, newObj.TestText2);

            setters["test_timestamp1"](newObj, poco.TestTimestamp1);
            Assert.Equal(poco.TestTimestamp1, newObj.TestTimestamp1);

            setters["test_timestamp2"](newObj, poco.TestTimestamp2);
            Assert.Equal(poco.TestTimestamp2, newObj.TestTimestamp2);
        }

        [Theory]
        [ClassData(typeof(GeneratedData<View1Poco>))]
        public void Clone(View1Poco poco)
        {
            var clone = DbMetadata.View1PocoMetadata.Clone;

            var newObj = clone(poco);

            Assert.NotEqual(poco, newObj);

            Assert.Equal(poco.Test1TestID, newObj.Test1TestID);
            Assert.Equal(poco.Test2TestID, newObj.Test2TestID);
            Assert.Equal(poco.TestBigint1, newObj.TestBigint1);
            Assert.Equal(poco.TestBigint2, newObj.TestBigint2);
            Assert.Equal(poco.TestBoolean1, newObj.TestBoolean1);
            Assert.Equal(poco.TestBoolean2, newObj.TestBoolean2);
            Assert.Equal(poco.TestChar1, newObj.TestChar1);
            Assert.Equal(poco.TestChar2, newObj.TestChar2);
            Assert.Equal(poco.TestDate, newObj.TestDate);
            Assert.Equal(poco.TestDate1, newObj.TestDate1);
            Assert.Equal(poco.TestDate2, newObj.TestDate2);
            Assert.Equal(poco.TestDecimal1, newObj.TestDecimal1);
            Assert.Equal(poco.TestDecimal2, newObj.TestDecimal2);
            Assert.Equal(poco.TestDouble1, newObj.TestDouble1);
            Assert.Equal(poco.TestDouble2, newObj.TestDouble2);
            Assert.Equal(poco.TestInteger1, newObj.TestInteger1);
            Assert.Equal(poco.TestInteger2, newObj.TestInteger2);
            Assert.Equal(poco.TestName, newObj.TestName);
            Assert.Equal(poco.TestName1, newObj.TestName1);
            Assert.Equal(poco.TestName2, newObj.TestName2);
            Assert.Equal(poco.TestReal1, newObj.TestReal1);
            Assert.Equal(poco.TestReal2, newObj.TestReal2);
            Assert.Equal(poco.TestText1, newObj.TestText1);
            Assert.Equal(poco.TestText2, newObj.TestText2);
            Assert.Equal(poco.TestTimestamp1, newObj.TestTimestamp1);
            Assert.Equal(poco.TestTimestamp2, newObj.TestTimestamp2);
        }
    }
}
