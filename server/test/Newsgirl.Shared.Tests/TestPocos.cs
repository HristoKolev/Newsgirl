// ReSharper disable InconsistentNaming
// ReSharper disable HeuristicUnreachableCode

namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LinqToDB;
    using LinqToDB.Mapping;
    using NpgsqlTypes;
    using Postgres;

    /// <summary>
    /// <para>Table name: 'test1'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "test1")]
    public class Test1Poco : IPoco<Test1Poco>
    {
        /// <summary>
        /// <para>Column name: 'test_bigint1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long?'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_bigint1", DataType = DataType.Int64)]
        public long? TestBigint1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_bigint2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_bigint2", DataType = DataType.Int64)]
        public long TestBigint2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_boolean1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'boolean'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Boolean'.</para>
        /// <para>CLR type: 'bool'.</para>
        /// <para>linq2db data type: 'DataType.Boolean'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_boolean1", DataType = DataType.Boolean)]
        public bool TestBoolean1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_boolean2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'boolean'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Boolean'.</para>
        /// <para>CLR type: 'bool?'.</para>
        /// <para>linq2db data type: 'DataType.Boolean'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_boolean2", DataType = DataType.Boolean)]
        public bool? TestBoolean2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_char1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'character'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Char'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NChar'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_char1", DataType = DataType.NChar)]
        public string TestChar1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_char2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'character'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Char'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NChar'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_char2", DataType = DataType.NChar)]
        public string TestChar2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_date1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'date'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Date'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.Date'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_date1", DataType = DataType.Date)]
        public DateTime TestDate1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_date2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'date'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Date'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.Date'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_date2", DataType = DataType.Date)]
        public DateTime? TestDate2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_decimal1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'numeric'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Numeric'.</para>
        /// <para>CLR type: 'decimal?'.</para>
        /// <para>linq2db data type: 'DataType.Decimal'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_decimal1", DataType = DataType.Decimal)]
        public decimal? TestDecimal1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_decimal2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'numeric'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Numeric'.</para>
        /// <para>CLR type: 'decimal'.</para>
        /// <para>linq2db data type: 'DataType.Decimal'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_decimal2", DataType = DataType.Decimal)]
        public decimal TestDecimal2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_double1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'double precision'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Double'.</para>
        /// <para>CLR type: 'double?'.</para>
        /// <para>linq2db data type: 'DataType.Double'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_double1", DataType = DataType.Double)]
        public double? TestDouble1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_double2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'double precision'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Double'.</para>
        /// <para>CLR type: 'double'.</para>
        /// <para>linq2db data type: 'DataType.Double'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_double2", DataType = DataType.Double)]
        public double TestDouble2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_id'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>Primary key of table: 'test1'.</para>
        /// <para>Primary key constraint name: 'test1_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "test_id", DataType = DataType.Int32)]
        public int TestID { get; set; }

        /// <summary>
        /// <para>Column name: 'test_integer1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int?'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_integer1", DataType = DataType.Int32)]
        public int? TestInteger1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_integer2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_integer2", DataType = DataType.Int32)]
        public int TestInteger2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_name1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'character varying'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_name1", DataType = DataType.NVarChar)]
        public string TestName1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_name2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'character varying'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_name2", DataType = DataType.NVarChar)]
        public string TestName2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_real1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'real'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Real'.</para>
        /// <para>CLR type: 'float?'.</para>
        /// <para>linq2db data type: 'DataType.Single'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_real1", DataType = DataType.Single)]
        public float? TestReal1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_real2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'real'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Real'.</para>
        /// <para>CLR type: 'float'.</para>
        /// <para>linq2db data type: 'DataType.Single'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_real2", DataType = DataType.Single)]
        public float TestReal2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_text1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_text1", DataType = DataType.Text)]
        public string TestText1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_text2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_text2", DataType = DataType.Text)]
        public string TestText2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_timestamp1'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_timestamp1", DataType = DataType.DateTime2)]
        public DateTime TestTimestamp1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_timestamp2'.</para>
        /// <para>Table name: 'test1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_timestamp2", DataType = DataType.DateTime2)]
        public DateTime? TestTimestamp2 { get; set; }

        public static TableMetadataModel<Test1Poco> Metadata => DbMetadata.Test1PocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'test2'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "test2")]
    public class Test2Poco : IPoco<Test2Poco>
    {
        /// <summary>
        /// <para>Column name: 'test_date'.</para>
        /// <para>Table name: 'test2'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_date", DataType = DataType.DateTime2)]
        public DateTime TestDate { get; set; }

        /// <summary>
        /// <para>Column name: 'test_id'.</para>
        /// <para>Table name: 'test2'.</para>
        /// <para>Primary key of table: 'test2'.</para>
        /// <para>Primary key constraint name: 'test2_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "test_id", DataType = DataType.Int32)]
        public int TestID { get; set; }

        /// <summary>
        /// <para>Column name: 'test_name'.</para>
        /// <para>Table name: 'test2'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "test_name", DataType = DataType.Text)]
        public string TestName { get; set; }

        public static TableMetadataModel<Test2Poco> Metadata => DbMetadata.Test2PocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'v_generate_series'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "v_generate_series")]
    public class VGenerateSeriesPoco : IReadOnlyPoco<VGenerateSeriesPoco>
    {
        /// <summary>
        /// <para>Column name: 'num'.</para>
        /// <para>Table name: 'v_generate_series'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int?'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "num", DataType = DataType.Int32)]
        public int? Num { get; set; }

        public static TableMetadataModel<VGenerateSeriesPoco> Metadata => DbMetadata.VGenerateSeriesPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'view1'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "view1")]
    public class View1Poco : IReadOnlyPoco<View1Poco>
    {
        /// <summary>
        /// <para>Column name: 'test1_test_id'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int?'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test1_test_id", DataType = DataType.Int32)]
        public int? Test1TestID { get; set; }

        /// <summary>
        /// <para>Column name: 'test2_test_id'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int?'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test2_test_id", DataType = DataType.Int32)]
        public int? Test2TestID { get; set; }

        /// <summary>
        /// <para>Column name: 'test_bigint1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long?'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_bigint1", DataType = DataType.Int64)]
        public long? TestBigint1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_bigint2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long?'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_bigint2", DataType = DataType.Int64)]
        public long? TestBigint2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_boolean1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'boolean'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Boolean'.</para>
        /// <para>CLR type: 'bool?'.</para>
        /// <para>linq2db data type: 'DataType.Boolean'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_boolean1", DataType = DataType.Boolean)]
        public bool? TestBoolean1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_boolean2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'boolean'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Boolean'.</para>
        /// <para>CLR type: 'bool?'.</para>
        /// <para>linq2db data type: 'DataType.Boolean'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_boolean2", DataType = DataType.Boolean)]
        public bool? TestBoolean2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_char1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'character'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Char'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NChar'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_char1", DataType = DataType.NChar)]
        public string TestChar1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_char2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'character'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Char'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NChar'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_char2", DataType = DataType.NChar)]
        public string TestChar2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_date'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_date", DataType = DataType.DateTime2)]
        public DateTime? TestDate { get; set; }

        /// <summary>
        /// <para>Column name: 'test_date1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'date'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Date'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.Date'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_date1", DataType = DataType.Date)]
        public DateTime? TestDate1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_date2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'date'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Date'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.Date'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_date2", DataType = DataType.Date)]
        public DateTime? TestDate2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_decimal1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'numeric'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Numeric'.</para>
        /// <para>CLR type: 'decimal?'.</para>
        /// <para>linq2db data type: 'DataType.Decimal'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_decimal1", DataType = DataType.Decimal)]
        public decimal? TestDecimal1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_decimal2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'numeric'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Numeric'.</para>
        /// <para>CLR type: 'decimal?'.</para>
        /// <para>linq2db data type: 'DataType.Decimal'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_decimal2", DataType = DataType.Decimal)]
        public decimal? TestDecimal2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_double1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'double precision'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Double'.</para>
        /// <para>CLR type: 'double?'.</para>
        /// <para>linq2db data type: 'DataType.Double'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_double1", DataType = DataType.Double)]
        public double? TestDouble1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_double2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'double precision'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Double'.</para>
        /// <para>CLR type: 'double?'.</para>
        /// <para>linq2db data type: 'DataType.Double'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_double2", DataType = DataType.Double)]
        public double? TestDouble2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_integer1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int?'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_integer1", DataType = DataType.Int32)]
        public int? TestInteger1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_integer2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int?'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_integer2", DataType = DataType.Int32)]
        public int? TestInteger2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_name'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_name", DataType = DataType.Text)]
        public string TestName { get; set; }

        /// <summary>
        /// <para>Column name: 'test_name1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'character varying'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_name1", DataType = DataType.NVarChar)]
        public string TestName1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_name2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'character varying'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_name2", DataType = DataType.NVarChar)]
        public string TestName2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_real1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'real'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Real'.</para>
        /// <para>CLR type: 'float?'.</para>
        /// <para>linq2db data type: 'DataType.Single'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_real1", DataType = DataType.Single)]
        public float? TestReal1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_real2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'real'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Real'.</para>
        /// <para>CLR type: 'float?'.</para>
        /// <para>linq2db data type: 'DataType.Single'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_real2", DataType = DataType.Single)]
        public float? TestReal2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_text1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_text1", DataType = DataType.Text)]
        public string TestText1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_text2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_text2", DataType = DataType.Text)]
        public string TestText2 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_timestamp1'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_timestamp1", DataType = DataType.DateTime2)]
        public DateTime? TestTimestamp1 { get; set; }

        /// <summary>
        /// <para>Column name: 'test_timestamp2'.</para>
        /// <para>Table name: 'view1'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "test_timestamp2", DataType = DataType.DateTime2)]
        public DateTime? TestTimestamp2 { get; set; }

        public static TableMetadataModel<View1Poco> Metadata => DbMetadata.View1PocoMetadata;
    }

    public class TestDbPocos : IDbPocos<TestDbPocos>
    {
        /// <summary>
        /// <para>Database table 'test1'.</para>
        /// </summary>
        public IQueryable<Test1Poco> Test1 => this.LinqProvider.GetTable<Test1Poco>();

        /// <summary>
        /// <para>Database table 'test2'.</para>
        /// </summary>
        public IQueryable<Test2Poco> Test2 => this.LinqProvider.GetTable<Test2Poco>();

        /// <summary>
        /// <para>Database table 'v_generate_series'.</para>
        /// </summary>
        public IQueryable<VGenerateSeriesPoco> VGenerateSeries => this.LinqProvider.GetTable<VGenerateSeriesPoco>();

        /// <summary>
        /// <para>Database table 'view1'.</para>
        /// </summary>
        public IQueryable<View1Poco> View1 => this.LinqProvider.GetTable<View1Poco>();

        /// <summary>
        /// <para>Database function 'increment_by_one'.</para>
        /// </summary>
        [Sql.FunctionAttribute(ServerSideOnly = true, Name = "increment_by_one")]
        public static int? IncrementByOne(int? num)
        {
            throw new NotImplementedException();
        }

        public ILinqProvider LinqProvider { private get; set; }
    }

    public class DbMetadata
    {
        internal static readonly TableMetadataModel<Test1Poco> Test1PocoMetadata;

        internal static readonly TableMetadataModel<Test2Poco> Test2PocoMetadata;

        internal static readonly TableMetadataModel<VGenerateSeriesPoco> VGenerateSeriesPocoMetadata;

        internal static readonly TableMetadataModel<View1Poco> View1PocoMetadata;

        internal static readonly List<FunctionMetadataModel> Functions = new List<FunctionMetadataModel>();

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once CyclomaticComplexity
        static DbMetadata()
        {
            Test1PocoMetadata = new TableMetadataModel<Test1Poco>
            {
                ClassName = "Test1",
                PluralClassName = "Test1",
                TableName = "test1",
                TableSchema = "public",
                PrimaryKeyColumnName = "test_id",
                PrimaryKeyPropertyName = "TestID",
                GetPrimaryKey = instance => instance.TestID,
                SetPrimaryKey = (instance, val) => instance.TestID = val,
                IsNew = instance => instance.TestID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_bigint1",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBigint1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "long?",
                            ClrType = typeof(long?),
                            ClrNonNullableTypeName = "long",
                            ClrNonNullableType = typeof(long),
                            ClrNullableTypeName = "long?",
                            ClrNullableType = typeof(long?),
                            DbDataType = "bigint",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int64",
                            Linq2DbDataType = DataType.Int64,
                            NpgsqlDbTypeName = "NpgsqlDbType.Bigint",
                            NpgsqlDbType = NpgsqlDbType.Bigint,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_bigint2",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBigint2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "long",
                            ClrType = typeof(long),
                            ClrNonNullableTypeName = "long",
                            ClrNonNullableType = typeof(long),
                            ClrNullableTypeName = "long?",
                            ClrNullableType = typeof(long?),
                            DbDataType = "bigint",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Int64",
                            Linq2DbDataType = DataType.Int64,
                            NpgsqlDbTypeName = "NpgsqlDbType.Bigint",
                            NpgsqlDbType = NpgsqlDbType.Bigint,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_boolean1",
                        DbDataType = "boolean",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBoolean1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "bool",
                            ClrType = typeof(bool),
                            ClrNonNullableTypeName = "bool",
                            ClrNonNullableType = typeof(bool),
                            ClrNullableTypeName = "bool?",
                            ClrNullableType = typeof(bool?),
                            DbDataType = "boolean",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Boolean",
                            Linq2DbDataType = DataType.Boolean,
                            NpgsqlDbTypeName = "NpgsqlDbType.Boolean",
                            NpgsqlDbType = NpgsqlDbType.Boolean,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_boolean2",
                        DbDataType = "boolean",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBoolean2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "bool?",
                            ClrType = typeof(bool?),
                            ClrNonNullableTypeName = "bool",
                            ClrNonNullableType = typeof(bool),
                            ClrNullableTypeName = "bool?",
                            ClrNullableType = typeof(bool?),
                            DbDataType = "boolean",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Boolean",
                            Linq2DbDataType = DataType.Boolean,
                            NpgsqlDbTypeName = "NpgsqlDbType.Boolean",
                            NpgsqlDbType = NpgsqlDbType.Boolean,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_char1",
                        DbDataType = "character",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestChar1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NChar",
                            Linq2DbDataType = DataType.NChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Char",
                            NpgsqlDbType = NpgsqlDbType.Char,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_char2",
                        DbDataType = "character",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestChar2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NChar",
                            Linq2DbDataType = DataType.NChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Char",
                            NpgsqlDbType = NpgsqlDbType.Char,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_date1",
                        DbDataType = "date",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDate1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime",
                            ClrType = typeof(DateTime),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "date",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Date",
                            Linq2DbDataType = DataType.Date,
                            NpgsqlDbTypeName = "NpgsqlDbType.Date",
                            NpgsqlDbType = NpgsqlDbType.Date,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_date2",
                        DbDataType = "date",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDate2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "date",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Date",
                            Linq2DbDataType = DataType.Date,
                            NpgsqlDbTypeName = "NpgsqlDbType.Date",
                            NpgsqlDbType = NpgsqlDbType.Date,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_decimal1",
                        DbDataType = "numeric",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDecimal1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "decimal?",
                            ClrType = typeof(decimal?),
                            ClrNonNullableTypeName = "decimal",
                            ClrNonNullableType = typeof(decimal),
                            ClrNullableTypeName = "decimal?",
                            ClrNullableType = typeof(decimal?),
                            DbDataType = "numeric",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Decimal",
                            Linq2DbDataType = DataType.Decimal,
                            NpgsqlDbTypeName = "NpgsqlDbType.Numeric",
                            NpgsqlDbType = NpgsqlDbType.Numeric,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_decimal2",
                        DbDataType = "numeric",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDecimal2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "decimal",
                            ClrType = typeof(decimal),
                            ClrNonNullableTypeName = "decimal",
                            ClrNonNullableType = typeof(decimal),
                            ClrNullableTypeName = "decimal?",
                            ClrNullableType = typeof(decimal?),
                            DbDataType = "numeric",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Decimal",
                            Linq2DbDataType = DataType.Decimal,
                            NpgsqlDbTypeName = "NpgsqlDbType.Numeric",
                            NpgsqlDbType = NpgsqlDbType.Numeric,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_double1",
                        DbDataType = "double precision",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDouble1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "double?",
                            ClrType = typeof(double?),
                            ClrNonNullableTypeName = "double",
                            ClrNonNullableType = typeof(double),
                            ClrNullableTypeName = "double?",
                            ClrNullableType = typeof(double?),
                            DbDataType = "double precision",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Double",
                            Linq2DbDataType = DataType.Double,
                            NpgsqlDbTypeName = "NpgsqlDbType.Double",
                            NpgsqlDbType = NpgsqlDbType.Double,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_double2",
                        DbDataType = "double precision",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDouble2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "double",
                            ClrType = typeof(double),
                            ClrNonNullableTypeName = "double",
                            ClrNonNullableType = typeof(double),
                            ClrNullableTypeName = "double?",
                            ClrNullableType = typeof(double?),
                            DbDataType = "double precision",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Double",
                            Linq2DbDataType = DataType.Double,
                            NpgsqlDbTypeName = "NpgsqlDbType.Double",
                            NpgsqlDbType = NpgsqlDbType.Double,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "test1_pkey" == string.Empty ? null : "test1_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestID",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int",
                            ClrType = typeof(int),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_integer1",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestInteger1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_integer2",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestInteger2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int",
                            ClrType = typeof(int),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_name1",
                        DbDataType = "character varying",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestName1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character varying",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NVarChar",
                            Linq2DbDataType = DataType.NVarChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Varchar",
                            NpgsqlDbType = NpgsqlDbType.Varchar,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_name2",
                        DbDataType = "character varying",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestName2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character varying",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NVarChar",
                            Linq2DbDataType = DataType.NVarChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Varchar",
                            NpgsqlDbType = NpgsqlDbType.Varchar,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_real1",
                        DbDataType = "real",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestReal1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "float?",
                            ClrType = typeof(float?),
                            ClrNonNullableTypeName = "float",
                            ClrNonNullableType = typeof(float),
                            ClrNullableTypeName = "float?",
                            ClrNullableType = typeof(float?),
                            DbDataType = "real",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Single",
                            Linq2DbDataType = DataType.Single,
                            NpgsqlDbTypeName = "NpgsqlDbType.Real",
                            NpgsqlDbType = NpgsqlDbType.Real,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_real2",
                        DbDataType = "real",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestReal2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "float",
                            ClrType = typeof(float),
                            ClrNonNullableTypeName = "float",
                            ClrNonNullableType = typeof(float),
                            ClrNullableTypeName = "float?",
                            ClrNullableType = typeof(float?),
                            DbDataType = "real",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Single",
                            Linq2DbDataType = DataType.Single,
                            NpgsqlDbTypeName = "NpgsqlDbType.Real",
                            NpgsqlDbType = NpgsqlDbType.Real,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_text1",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestText1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "text",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Text",
                            Linq2DbDataType = DataType.Text,
                            NpgsqlDbTypeName = "NpgsqlDbType.Text",
                            NpgsqlDbType = NpgsqlDbType.Text,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_text2",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestText2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "text",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Text",
                            Linq2DbDataType = DataType.Text,
                            NpgsqlDbTypeName = "NpgsqlDbType.Text",
                            NpgsqlDbType = NpgsqlDbType.Text,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_timestamp1",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestTimestamp1",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime",
                            ClrType = typeof(DateTime),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "timestamp without time zone",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.DateTime2",
                            Linq2DbDataType = DataType.DateTime2,
                            NpgsqlDbTypeName = "NpgsqlDbType.Timestamp",
                            NpgsqlDbType = NpgsqlDbType.Timestamp,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_timestamp2",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestTimestamp2",
                        TableName = "test1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "timestamp without time zone",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.DateTime2",
                            Linq2DbDataType = DataType.DateTime2,
                            NpgsqlDbTypeName = "NpgsqlDbType.Timestamp",
                            NpgsqlDbType = NpgsqlDbType.Timestamp,
                        },
                    },
                },
            };

            Test1PocoMetadata.Clone = DbCodeGenerator.GetClone<Test1Poco>();
            Test1PocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(Test1PocoMetadata);
            Test1PocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(Test1PocoMetadata);
            Test1PocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(Test1PocoMetadata);
            Test1PocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(Test1PocoMetadata);

            Test2PocoMetadata = new TableMetadataModel<Test2Poco>
            {
                ClassName = "Test2",
                PluralClassName = "Test2",
                TableName = "test2",
                TableSchema = "public",
                PrimaryKeyColumnName = "test_id",
                PrimaryKeyPropertyName = "TestID",
                GetPrimaryKey = instance => instance.TestID,
                SetPrimaryKey = (instance, val) => instance.TestID = val,
                IsNew = instance => instance.TestID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_date",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDate",
                        TableName = "test2",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime",
                            ClrType = typeof(DateTime),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "timestamp without time zone",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.DateTime2",
                            Linq2DbDataType = DataType.DateTime2,
                            NpgsqlDbTypeName = "NpgsqlDbType.Timestamp",
                            NpgsqlDbType = NpgsqlDbType.Timestamp,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "test2_pkey" == string.Empty ? null : "test2_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestID",
                        TableName = "test2",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int",
                            ClrType = typeof(int),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("False"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_name",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestName",
                        TableName = "test2",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "text",
                            IsNullable = bool.Parse("False"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Text",
                            Linq2DbDataType = DataType.Text,
                            NpgsqlDbTypeName = "NpgsqlDbType.Text",
                            NpgsqlDbType = NpgsqlDbType.Text,
                        },
                    },
                },
            };

            Test2PocoMetadata.Clone = DbCodeGenerator.GetClone<Test2Poco>();
            Test2PocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(Test2PocoMetadata);
            Test2PocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(Test2PocoMetadata);
            Test2PocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(Test2PocoMetadata);
            Test2PocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(Test2PocoMetadata);

            VGenerateSeriesPocoMetadata = new TableMetadataModel<VGenerateSeriesPoco>
            {
                ClassName = "VGenerateSeries",
                PluralClassName = "VGenerateSeries",
                TableName = "v_generate_series",
                TableSchema = "public",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "num",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Num",
                        TableName = "v_generate_series",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                },
            };

            VGenerateSeriesPocoMetadata.Clone = DbCodeGenerator.GetClone<VGenerateSeriesPoco>();

            View1PocoMetadata = new TableMetadataModel<View1Poco>
            {
                ClassName = "View1",
                PluralClassName = "View1",
                TableName = "view1",
                TableSchema = "public",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test1_test_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Test1TestID",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test2_test_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Test2TestID",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_bigint1",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBigint1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "long?",
                            ClrType = typeof(long?),
                            ClrNonNullableTypeName = "long",
                            ClrNonNullableType = typeof(long),
                            ClrNullableTypeName = "long?",
                            ClrNullableType = typeof(long?),
                            DbDataType = "bigint",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int64",
                            Linq2DbDataType = DataType.Int64,
                            NpgsqlDbTypeName = "NpgsqlDbType.Bigint",
                            NpgsqlDbType = NpgsqlDbType.Bigint,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_bigint2",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBigint2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "long?",
                            ClrType = typeof(long?),
                            ClrNonNullableTypeName = "long",
                            ClrNonNullableType = typeof(long),
                            ClrNullableTypeName = "long?",
                            ClrNullableType = typeof(long?),
                            DbDataType = "bigint",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int64",
                            Linq2DbDataType = DataType.Int64,
                            NpgsqlDbTypeName = "NpgsqlDbType.Bigint",
                            NpgsqlDbType = NpgsqlDbType.Bigint,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_boolean1",
                        DbDataType = "boolean",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBoolean1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "bool?",
                            ClrType = typeof(bool?),
                            ClrNonNullableTypeName = "bool",
                            ClrNonNullableType = typeof(bool),
                            ClrNullableTypeName = "bool?",
                            ClrNullableType = typeof(bool?),
                            DbDataType = "boolean",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Boolean",
                            Linq2DbDataType = DataType.Boolean,
                            NpgsqlDbTypeName = "NpgsqlDbType.Boolean",
                            NpgsqlDbType = NpgsqlDbType.Boolean,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_boolean2",
                        DbDataType = "boolean",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestBoolean2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "bool?",
                            ClrType = typeof(bool?),
                            ClrNonNullableTypeName = "bool",
                            ClrNonNullableType = typeof(bool),
                            ClrNullableTypeName = "bool?",
                            ClrNullableType = typeof(bool?),
                            DbDataType = "boolean",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Boolean",
                            Linq2DbDataType = DataType.Boolean,
                            NpgsqlDbTypeName = "NpgsqlDbType.Boolean",
                            NpgsqlDbType = NpgsqlDbType.Boolean,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_char1",
                        DbDataType = "character",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestChar1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NChar",
                            Linq2DbDataType = DataType.NChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Char",
                            NpgsqlDbType = NpgsqlDbType.Char,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_char2",
                        DbDataType = "character",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestChar2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NChar",
                            Linq2DbDataType = DataType.NChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Char",
                            NpgsqlDbType = NpgsqlDbType.Char,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_date",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDate",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "timestamp without time zone",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.DateTime2",
                            Linq2DbDataType = DataType.DateTime2,
                            NpgsqlDbTypeName = "NpgsqlDbType.Timestamp",
                            NpgsqlDbType = NpgsqlDbType.Timestamp,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_date1",
                        DbDataType = "date",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDate1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "date",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Date",
                            Linq2DbDataType = DataType.Date,
                            NpgsqlDbTypeName = "NpgsqlDbType.Date",
                            NpgsqlDbType = NpgsqlDbType.Date,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_date2",
                        DbDataType = "date",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDate2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "date",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Date",
                            Linq2DbDataType = DataType.Date,
                            NpgsqlDbTypeName = "NpgsqlDbType.Date",
                            NpgsqlDbType = NpgsqlDbType.Date,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_decimal1",
                        DbDataType = "numeric",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDecimal1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "decimal?",
                            ClrType = typeof(decimal?),
                            ClrNonNullableTypeName = "decimal",
                            ClrNonNullableType = typeof(decimal),
                            ClrNullableTypeName = "decimal?",
                            ClrNullableType = typeof(decimal?),
                            DbDataType = "numeric",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Decimal",
                            Linq2DbDataType = DataType.Decimal,
                            NpgsqlDbTypeName = "NpgsqlDbType.Numeric",
                            NpgsqlDbType = NpgsqlDbType.Numeric,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_decimal2",
                        DbDataType = "numeric",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDecimal2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "decimal?",
                            ClrType = typeof(decimal?),
                            ClrNonNullableTypeName = "decimal",
                            ClrNonNullableType = typeof(decimal),
                            ClrNullableTypeName = "decimal?",
                            ClrNullableType = typeof(decimal?),
                            DbDataType = "numeric",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Decimal",
                            Linq2DbDataType = DataType.Decimal,
                            NpgsqlDbTypeName = "NpgsqlDbType.Numeric",
                            NpgsqlDbType = NpgsqlDbType.Numeric,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_double1",
                        DbDataType = "double precision",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDouble1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "double?",
                            ClrType = typeof(double?),
                            ClrNonNullableTypeName = "double",
                            ClrNonNullableType = typeof(double),
                            ClrNullableTypeName = "double?",
                            ClrNullableType = typeof(double?),
                            DbDataType = "double precision",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Double",
                            Linq2DbDataType = DataType.Double,
                            NpgsqlDbTypeName = "NpgsqlDbType.Double",
                            NpgsqlDbType = NpgsqlDbType.Double,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_double2",
                        DbDataType = "double precision",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestDouble2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "double?",
                            ClrType = typeof(double?),
                            ClrNonNullableTypeName = "double",
                            ClrNonNullableType = typeof(double),
                            ClrNullableTypeName = "double?",
                            ClrNullableType = typeof(double?),
                            DbDataType = "double precision",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Double",
                            Linq2DbDataType = DataType.Double,
                            NpgsqlDbTypeName = "NpgsqlDbType.Double",
                            NpgsqlDbType = NpgsqlDbType.Double,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_integer1",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestInteger1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_integer2",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestInteger2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_name",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestName",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "text",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Text",
                            Linq2DbDataType = DataType.Text,
                            NpgsqlDbTypeName = "NpgsqlDbType.Text",
                            NpgsqlDbType = NpgsqlDbType.Text,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_name1",
                        DbDataType = "character varying",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestName1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character varying",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NVarChar",
                            Linq2DbDataType = DataType.NVarChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Varchar",
                            NpgsqlDbType = NpgsqlDbType.Varchar,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_name2",
                        DbDataType = "character varying",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestName2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "character varying",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.NVarChar",
                            Linq2DbDataType = DataType.NVarChar,
                            NpgsqlDbTypeName = "NpgsqlDbType.Varchar",
                            NpgsqlDbType = NpgsqlDbType.Varchar,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_real1",
                        DbDataType = "real",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestReal1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "float?",
                            ClrType = typeof(float?),
                            ClrNonNullableTypeName = "float",
                            ClrNonNullableType = typeof(float),
                            ClrNullableTypeName = "float?",
                            ClrNullableType = typeof(float?),
                            DbDataType = "real",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Single",
                            Linq2DbDataType = DataType.Single,
                            NpgsqlDbTypeName = "NpgsqlDbType.Real",
                            NpgsqlDbType = NpgsqlDbType.Real,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_real2",
                        DbDataType = "real",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestReal2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "float?",
                            ClrType = typeof(float?),
                            ClrNonNullableTypeName = "float",
                            ClrNonNullableType = typeof(float),
                            ClrNullableTypeName = "float?",
                            ClrNullableType = typeof(float?),
                            DbDataType = "real",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Single",
                            Linq2DbDataType = DataType.Single,
                            NpgsqlDbTypeName = "NpgsqlDbType.Real",
                            NpgsqlDbType = NpgsqlDbType.Real,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_text1",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestText1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "text",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Text",
                            Linq2DbDataType = DataType.Text,
                            NpgsqlDbTypeName = "NpgsqlDbType.Text",
                            NpgsqlDbType = NpgsqlDbType.Text,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_text2",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestText2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "string",
                            ClrType = typeof(string),
                            ClrNonNullableTypeName = "string",
                            ClrNonNullableType = typeof(string),
                            ClrNullableTypeName = "string",
                            ClrNullableType = typeof(string),
                            DbDataType = "text",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("False"),
                            IsClrNullableType = bool.Parse("False"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Text",
                            Linq2DbDataType = DataType.Text,
                            NpgsqlDbTypeName = "NpgsqlDbType.Text",
                            NpgsqlDbType = NpgsqlDbType.Text,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_timestamp1",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestTimestamp1",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "timestamp without time zone",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.DateTime2",
                            Linq2DbDataType = DataType.DateTime2,
                            NpgsqlDbTypeName = "NpgsqlDbType.Timestamp",
                            NpgsqlDbType = NpgsqlDbType.Timestamp,
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "test_timestamp2",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "TestTimestamp2",
                        TableName = "view1",
                        TableSchema = "public",
                        PropertyType = new SimpleType
                        {
                            ClrTypeName = "DateTime?",
                            ClrType = typeof(DateTime?),
                            ClrNonNullableTypeName = "DateTime",
                            ClrNonNullableType = typeof(DateTime),
                            ClrNullableTypeName = "DateTime?",
                            ClrNullableType = typeof(DateTime?),
                            DbDataType = "timestamp without time zone",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.DateTime2",
                            Linq2DbDataType = DataType.DateTime2,
                            NpgsqlDbTypeName = "NpgsqlDbType.Timestamp",
                            NpgsqlDbType = NpgsqlDbType.Timestamp,
                        },
                    },
                },
            };

            View1PocoMetadata.Clone = DbCodeGenerator.GetClone<View1Poco>();

            Functions.Add(new FunctionMetadataModel
            {
                SchemaName = "public" == string.Empty ? null : "public",
                FunctionName = "increment_by_one" == string.Empty ? null : "increment_by_one",
                MethodName = "IncrementByOne" == string.Empty ? null : "IncrementByOne",
                FunctionReturnTypeName = "int4" == string.Empty ? null : "int4",
                FunctionComment = "" == string.Empty ? null : "",
                FunctionArgumentsAsString = "num integer" switch
                {
                    "" => null,
                    _ => "num integer",
                },
                FunctionReturnType = new SimpleType
                {
                    ClrTypeName = "int?",
                    ClrType = typeof(int?),
                    ClrNonNullableTypeName = "int",
                    ClrNonNullableType = typeof(int),
                    ClrNullableTypeName = "int?",
                    ClrNullableType = typeof(int?),
                    DbDataType = "int4",
                    IsNullable = bool.Parse("True"),
                    IsClrValueType = bool.Parse("True"),
                    IsClrNullableType = bool.Parse("True"),
                    IsClrReferenceType = bool.Parse("True"),
                    Linq2DbDataTypeName = "DataType.Int32",
                    Linq2DbDataType = DataType.Int32,
                    NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                    NpgsqlDbType = NpgsqlDbType.Integer,
                },
                FunctionArguments = new Dictionary<string, SimpleType>
                {
                    {
                        "num", new SimpleType
                        {
                            ClrTypeName = "int?",
                            ClrType = typeof(int?),
                            ClrNonNullableTypeName = "int",
                            ClrNonNullableType = typeof(int),
                            ClrNullableTypeName = "int?",
                            ClrNullableType = typeof(int?),
                            DbDataType = "integer",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Int32",
                            Linq2DbDataType = DataType.Int32,
                            NpgsqlDbTypeName = "NpgsqlDbType.Integer",
                            NpgsqlDbType = NpgsqlDbType.Integer,
                        }
                    },
                },
            });
        }
    }
}
