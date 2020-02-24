namespace Newsgirl.Shared.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LinqToDB;
    using LinqToDB.Mapping;

    using NpgsqlTypes;

    using PgNet;

    /// <summary>
    /// <para>Table name: 'feed_items'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema="public", Name = "feed_items")]
    public class FeedItemPoco : IPoco<FeedItemPoco>
    {
        /// <summary>
        /// <para>Column name: 'feed_id'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>Foreign key column [public.feed_items.feed_id -> public.feeds.feed_id].</para>
        /// <para>Foreign key constraint name: 'feed_items_feed_id_fkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "feed_id", DataType = DataType.Int32)]
        public int FeedID { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_added_time'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "feed_item_added_time", DataType = DataType.DateTime2)]
        public DateTime FeedItemAddedTime { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_description'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "feed_item_description", DataType = DataType.Text)]
        public string FeedItemDescription { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_id'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>Primary key of table: 'feed_items'.</para>
        /// <para>Primary key constraint name: 'feed_items_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey, Identity]
        [Column(Name = "feed_item_id", DataType = DataType.Int32)]
        public int FeedItemID { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_title'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "feed_item_title", DataType = DataType.Text)]
        public string FeedItemTitle { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_url'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "feed_item_url", DataType = DataType.Text)]
        public string FeedItemUrl { get; set; }

        public static TableMetadataModel<FeedItemPoco> Metadata => DbMetadata.FeedItemPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'feeds'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema="public", Name = "feeds")]
    public class FeedPoco : IPoco<FeedPoco>
    {
        /// <summary>
        /// <para>Column name: 'feed_id'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>Primary key of table: 'feeds'.</para>
        /// <para>Primary key constraint name: 'feeds_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey, Identity]
        [Column(Name = "feed_id", DataType = DataType.Int32)]
        public int FeedID { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_name'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "feed_name", DataType = DataType.Text)]
        public string FeedName { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_updated'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "feed_updated", DataType = DataType.DateTime2)]
        public DateTime FeedUpdated { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_url'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "feed_url", DataType = DataType.Text)]
        public string FeedUrl { get; set; }

        public static TableMetadataModel<FeedPoco> Metadata => DbMetadata.FeedPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'system_settings'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema="public", Name = "system_settings")]
    public class SystemSettingPoco : IPoco<SystemSettingPoco>
    {
        /// <summary>
        /// <para>Column name: 'setting_id'.</para>
        /// <para>Table name: 'system_settings'.</para>
        /// <para>Primary key of table: 'system_settings'.</para>
        /// <para>Primary key constraint name: 'system_settings_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey, Identity]
        [Column(Name = "setting_id", DataType = DataType.Int32)]
        public int SettingID { get; set; }

        /// <summary>
        /// <para>Column name: 'setting_name'.</para>
        /// <para>Table name: 'system_settings'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "setting_name", DataType = DataType.Text)]
        public string SettingName { get; set; }

        /// <summary>
        /// <para>Column name: 'setting_value'.</para>
        /// <para>Table name: 'system_settings'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "setting_value", DataType = DataType.Text)]
        public string SettingValue { get; set; }

        public static TableMetadataModel<SystemSettingPoco> Metadata => DbMetadata.SystemSettingPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'user_sessions'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema="public", Name = "user_sessions")]
    public class UserSessionPoco : IPoco<UserSessionPoco>
    {
        /// <summary>
        /// <para>Column name: 'login_date'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "login_date", DataType = DataType.DateTime2)]
        public DateTime LoginDate { get; set; }

        /// <summary>
        /// <para>Column name: 'session_id'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>Primary key of table: 'user_sessions'.</para>
        /// <para>Primary key constraint name: 'user_sessions_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey, Identity]
        [Column(Name = "session_id", DataType = DataType.Int32)]
        public int SessionID { get; set; }

        /// <summary>
        /// <para>Column name: 'user_id'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>Foreign key column [public.user_sessions.user_id -> public.users.user_id].</para>
        /// <para>Foreign key constraint name: 'user_sessions_user_id_fkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "user_id", DataType = DataType.Int32)]
        public int UserID { get; set; }

        public static TableMetadataModel<UserSessionPoco> Metadata => DbMetadata.UserSessionPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'users'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema="public", Name = "users")]
    public class UserPoco : IPoco<UserPoco>
    {
        /// <summary>
        /// <para>Column name: 'password'.</para>
        /// <para>Table name: 'users'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "password", DataType = DataType.Text)]
        public string Password { get; set; }

        /// <summary>
        /// <para>Column name: 'registration_date'.</para>
        /// <para>Table name: 'users'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "registration_date", DataType = DataType.DateTime2)]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// <para>Column name: 'user_id'.</para>
        /// <para>Table name: 'users'.</para>
        /// <para>Primary key of table: 'users'.</para>
        /// <para>Primary key constraint name: 'users_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey, Identity]
        [Column(Name = "user_id", DataType = DataType.Int32)]
        public int UserID { get; set; }

        /// <summary>
        /// <para>Column name: 'username'.</para>
        /// <para>Table name: 'users'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "username", DataType = DataType.Text)]
        public string Username { get; set; }

        public static TableMetadataModel<UserPoco> Metadata => DbMetadata.UserPocoMetadata;
    }

    public class DbPocos : IDbPocos<DbPocos>
    {
        /// <summary>
        /// <para>Database table 'feed_items'.</para>
        /// </summary>
        public IQueryable<FeedItemPoco> FeedItems => this.LinqProvider.GetTable<FeedItemPoco>();

        /// <summary>
        /// <para>Database table 'feeds'.</para>
        /// </summary>
        public IQueryable<FeedPoco> Feeds => this.LinqProvider.GetTable<FeedPoco>();

        /// <summary>
        /// <para>Database table 'system_settings'.</para>
        /// </summary>
        public IQueryable<SystemSettingPoco> SystemSettings => this.LinqProvider.GetTable<SystemSettingPoco>();

        /// <summary>
        /// <para>Database table 'user_sessions'.</para>
        /// </summary>
        public IQueryable<UserSessionPoco> UserSessions => this.LinqProvider.GetTable<UserSessionPoco>();

        /// <summary>
        /// <para>Database table 'users'.</para>
        /// </summary>
        public IQueryable<UserPoco> Users => this.LinqProvider.GetTable<UserPoco>();


        public ILinqProvider LinqProvider { private get; set; }
    }

    public class DbMetadata : IDbMetadata
    {
        internal static TableMetadataModel<FeedItemPoco> FeedItemPocoMetadata;

        internal static TableMetadataModel<FeedPoco> FeedPocoMetadata;

        internal static TableMetadataModel<SystemSettingPoco> SystemSettingPocoMetadata;

        internal static TableMetadataModel<UserSessionPoco> UserSessionPocoMetadata;

        internal static TableMetadataModel<UserPoco> UserPocoMetadata;

        internal static readonly List<FunctionMetadataModel> Functions = new List<FunctionMetadataModel>(); 
            
        private static readonly object InitLock = new object();

        private static bool Initialized;

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once CyclomaticComplexity
        private static void InitializeInternal()
        {
            FeedItemPocoMetadata = new TableMetadataModel<FeedItemPoco>
            {
                ClassName = "FeedItem",
                PluralClassName = "FeedItems",
                TableName = "feed_items",
                TableSchema = "public",
                PrimaryKeyColumnName = "feed_item_id",
                PrimaryKeyPropertyName = "FeedItemID",
                GetPrimaryKey = (instance) => instance.FeedItemID,
                SetPrimaryKey = (instance, val) => instance.FeedItemID = val,
                IsNew = (instance) => instance.FeedItemID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("True"),
                        ForeignKeyConstraintName = "feed_items_feed_id_fkey" == string.Empty ? null : "feed_items_feed_id_fkey",
                        ForeignKeyReferenceColumnName = "feed_id" == string.Empty ? null : "feed_id",
                        ForeignKeyReferenceSchemaName = "public" == string.Empty ? null : "public",
                        ForeignKeyReferenceTableName = "feeds" == string.Empty ? null : "feeds",
                        PropertyName = "FeedID",
                        TableName = "feed_items",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_added_time",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemAddedTime",
                        TableName = "feed_items",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_description",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemDescription",
                        TableName = "feed_items",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "feed_items_pkey" == string.Empty ? null : "feed_items_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemID",
                        TableName = "feed_items",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_title",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemTitle",
                        TableName = "feed_items",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_url",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemUrl",
                        TableName = "feed_items",
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
                        }
                    },
                }
            };

            FeedItemPocoMetadata.Clone = DbCodeGenerator.GetClone<FeedItemPoco>();
            FeedItemPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(FeedItemPocoMetadata);
            FeedItemPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(FeedItemPocoMetadata);
            FeedItemPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(FeedItemPocoMetadata);
            FeedItemPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(FeedItemPocoMetadata);

            FeedPocoMetadata = new TableMetadataModel<FeedPoco>
            {
                ClassName = "Feed",
                PluralClassName = "Feeds",
                TableName = "feeds",
                TableSchema = "public",
                PrimaryKeyColumnName = "feed_id",
                PrimaryKeyPropertyName = "FeedID",
                GetPrimaryKey = (instance) => instance.FeedID,
                SetPrimaryKey = (instance, val) => instance.FeedID = val,
                IsNew = (instance) => instance.FeedID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "feeds_pkey" == string.Empty ? null : "feeds_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedID",
                        TableName = "feeds",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_name",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedName",
                        TableName = "feeds",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_updated",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedUpdated",
                        TableName = "feeds",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_url",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedUrl",
                        TableName = "feeds",
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
                        }
                    },
                }
            };

            FeedPocoMetadata.Clone = DbCodeGenerator.GetClone<FeedPoco>();
            FeedPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(FeedPocoMetadata);
            FeedPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(FeedPocoMetadata);
            FeedPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(FeedPocoMetadata);
            FeedPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(FeedPocoMetadata);

            SystemSettingPocoMetadata = new TableMetadataModel<SystemSettingPoco>
            {
                ClassName = "SystemSetting",
                PluralClassName = "SystemSettings",
                TableName = "system_settings",
                TableSchema = "public",
                PrimaryKeyColumnName = "setting_id",
                PrimaryKeyPropertyName = "SettingID",
                GetPrimaryKey = (instance) => instance.SettingID,
                SetPrimaryKey = (instance, val) => instance.SettingID = val,
                IsNew = (instance) => instance.SettingID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "setting_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "system_settings_pkey" == string.Empty ? null : "system_settings_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "SettingID",
                        TableName = "system_settings",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "setting_name",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "SettingName",
                        TableName = "system_settings",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "setting_value",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "SettingValue",
                        TableName = "system_settings",
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
                        }
                    },
                }
            };

            SystemSettingPocoMetadata.Clone = DbCodeGenerator.GetClone<SystemSettingPoco>();
            SystemSettingPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(SystemSettingPocoMetadata);
            SystemSettingPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(SystemSettingPocoMetadata);
            SystemSettingPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(SystemSettingPocoMetadata);
            SystemSettingPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(SystemSettingPocoMetadata);

            UserSessionPocoMetadata = new TableMetadataModel<UserSessionPoco>
            {
                ClassName = "UserSession",
                PluralClassName = "UserSessions",
                TableName = "user_sessions",
                TableSchema = "public",
                PrimaryKeyColumnName = "session_id",
                PrimaryKeyPropertyName = "SessionID",
                GetPrimaryKey = (instance) => instance.SessionID,
                SetPrimaryKey = (instance, val) => instance.SessionID = val,
                IsNew = (instance) => instance.SessionID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "login_date",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "LoginDate",
                        TableName = "user_sessions",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "session_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "user_sessions_pkey" == string.Empty ? null : "user_sessions_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "SessionID",
                        TableName = "user_sessions",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "user_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("True"),
                        ForeignKeyConstraintName = "user_sessions_user_id_fkey" == string.Empty ? null : "user_sessions_user_id_fkey",
                        ForeignKeyReferenceColumnName = "user_id" == string.Empty ? null : "user_id",
                        ForeignKeyReferenceSchemaName = "public" == string.Empty ? null : "public",
                        ForeignKeyReferenceTableName = "users" == string.Empty ? null : "users",
                        PropertyName = "UserID",
                        TableName = "user_sessions",
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
                        }
                    },
                }
            };

            UserSessionPocoMetadata.Clone = DbCodeGenerator.GetClone<UserSessionPoco>();
            UserSessionPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(UserSessionPocoMetadata);
            UserSessionPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(UserSessionPocoMetadata);
            UserSessionPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(UserSessionPocoMetadata);
            UserSessionPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(UserSessionPocoMetadata);

            UserPocoMetadata = new TableMetadataModel<UserPoco>
            {
                ClassName = "User",
                PluralClassName = "Users",
                TableName = "users",
                TableSchema = "public",
                PrimaryKeyColumnName = "user_id",
                PrimaryKeyPropertyName = "UserID",
                GetPrimaryKey = (instance) => instance.UserID,
                SetPrimaryKey = (instance, val) => instance.UserID = val,
                IsNew = (instance) => instance.UserID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "password",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Password",
                        TableName = "users",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "registration_date",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "RegistrationDate",
                        TableName = "users",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "user_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "users_pkey" == string.Empty ? null : "users_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "UserID",
                        TableName = "users",
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
                        }
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "username",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Username",
                        TableName = "users",
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
                        }
                    },
                }
            };

            UserPocoMetadata.Clone = DbCodeGenerator.GetClone<UserPoco>();
            UserPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(UserPocoMetadata);
            UserPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(UserPocoMetadata);
            UserPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(UserPocoMetadata);
            UserPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(UserPocoMetadata);


        }

        public static void Initialize()
        {
            if(Initialized)
            {
                return;
            }

            lock(InitLock)
            {
                if(Initialized)
                {
                    return;
                }

                InitializeInternal();

                Initialized = true;
            }
        }

        static DbMetadata()
        {
            Initialize();
        }
    }
}
