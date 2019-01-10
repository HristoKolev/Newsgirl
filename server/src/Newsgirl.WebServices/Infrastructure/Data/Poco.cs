namespace Newsgirl.WebServices.Infrastructure.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using LinqToDB;
    using LinqToDB.Mapping;

    using NpgsqlTypes;

    using PgNet;

    /// <summary>
    ///     <para>Table name: 'system_settings'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "system_settings")]
    public class SystemSettingPoco : IPoco<SystemSettingPoco>
    {
        /// <summary>
        ///     <para>Column name: 'setting_id'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>Primary key of table: 'system_settings'.</para>
        ///     <para>Primary key constraint name: 'system_settings_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "setting_id", DataType = DataType.Int32)]
        public int SettingID { get; set; }

        /// <summary>
        ///     <para>Column name: 'setting_name'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "setting_name", DataType = DataType.NVarChar)]
        public string SettingName { get; set; }

        /// <summary>
        ///     <para>Column name: 'setting_value'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "setting_value", DataType = DataType.NVarChar)]
        public string SettingValue { get; set; }

        public static TableMetadataModel<SystemSettingPoco> Metadata => DbMetadata.SystemSettingPocoMetadata;

        public SystemSettingBM ToBm()
        {
            return new SystemSettingBM
            {
                SettingID = this.SettingID,
                SettingName = this.SettingName,
                SettingValue = this.SettingValue
            };
        }
    }

    /// <summary>
    ///     <para>Table name: 'user_sessions'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "user_sessions")]
    public class UserSessionPoco : IPoco<UserSessionPoco>
    {
        /// <summary>
        ///     <para>Column name: 'login_date'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        ///     <para>CLR type: 'DateTime'.</para>
        ///     <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "login_date", DataType = DataType.DateTime2)]
        public DateTime LoginDate { get; set; }

        /// <summary>
        ///     <para>Column name: 'session_id'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>Primary key of table: 'user_sessions'.</para>
        ///     <para>Primary key constraint name: 'user_sessions_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "session_id", DataType = DataType.Int32)]
        public int SessionID { get; set; }

        /// <summary>
        ///     <para>Column name: 'user_id'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>Foreign key column [public.user_sessions.user_id -> public.users.user_id].</para>
        ///     <para>Foreign key constraint name: 'user_sessions_user_id_fkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "user_id", DataType = DataType.Int32)]
        public int UserID { get; set; }

        public static TableMetadataModel<UserSessionPoco> Metadata => DbMetadata.UserSessionPocoMetadata;

        public UserSessionBM ToBm()
        {
            return new UserSessionBM
            {
                LoginDate = this.LoginDate,
                SessionID = this.SessionID,
                UserID = this.UserID
            };
        }
    }

    /// <summary>
    ///     <para>Table name: 'users'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "users")]
    public class UserPoco : IPoco<UserPoco>
    {
        /// <summary>
        ///     <para>Column name: 'password'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "password", DataType = DataType.NVarChar)]
        public string Password { get; set; }

        /// <summary>
        ///     <para>Column name: 'registration_date'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        ///     <para>CLR type: 'DateTime'.</para>
        ///     <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "registration_date", DataType = DataType.DateTime2)]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        ///     <para>Column name: 'user_id'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>Primary key of table: 'users'.</para>
        ///     <para>Primary key constraint name: 'users_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "user_id", DataType = DataType.Int32)]
        public int UserID { get; set; }

        /// <summary>
        ///     <para>Column name: 'username'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        [NotNull]
        [Column(Name = "username", DataType = DataType.NVarChar)]
        public string Username { get; set; }

        public static TableMetadataModel<UserPoco> Metadata => DbMetadata.UserPocoMetadata;

        public UserBM ToBm()
        {
            return new UserBM
            {
                Password = this.Password,
                RegistrationDate = this.RegistrationDate,
                UserID = this.UserID,
                Username = this.Username
            };
        }
    }


    /// <summary>
    ///     <para>Table name: 'system_settings'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class SystemSettingCM : ICatalogModel<SystemSettingPoco>
    {
        /// <summary>
        ///     <para>Column name: 'setting_id'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>Primary key of table: 'system_settings'.</para>
        ///     <para>Primary key constraint name: 'system_settings_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int SettingID { get; set; }

        /// <summary>
        ///     <para>Column name: 'setting_name'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        ///     <para>Column name: 'setting_value'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string SettingValue { get; set; }
    }

    /// <summary>
    ///     <para>Table name: 'user_sessions'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class UserSessionCM : ICatalogModel<UserSessionPoco>
    {
        /// <summary>
        ///     <para>Column name: 'login_date'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        ///     <para>CLR type: 'DateTime'.</para>
        ///     <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        public DateTime LoginDate { get; set; }

        /// <summary>
        ///     <para>Column name: 'session_id'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>Primary key of table: 'user_sessions'.</para>
        ///     <para>Primary key constraint name: 'user_sessions_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int SessionID { get; set; }

        /// <summary>
        ///     <para>Column name: 'user_id'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>Foreign key column [public.user_sessions.user_id -> public.users.user_id].</para>
        ///     <para>Foreign key constraint name: 'user_sessions_user_id_fkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int UserID { get; set; }
    }

    /// <summary>
    ///     <para>Table name: 'users'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class UserCM : ICatalogModel<UserPoco>
    {
        /// <summary>
        ///     <para>Column name: 'password'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     <para>Column name: 'registration_date'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        ///     <para>CLR type: 'DateTime'.</para>
        ///     <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        ///     <para>Column name: 'user_id'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>Primary key of table: 'users'.</para>
        ///     <para>Primary key constraint name: 'users_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        ///     <para>Column name: 'username'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string Username { get; set; }
    }

    /// <summary>
    ///     <para>Table name: 'system_settings'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class SystemSettingFM : IFilterModel<SystemSettingPoco>
    {
        [FilterOperator(QueryOperatorType.Equal, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int? SettingID { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int? SettingID_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.LessThan, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int? SettingID_LessThan { get; set; }

        [FilterOperator(QueryOperatorType.LessThanOrEqual, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int? SettingID_LessThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThan, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int? SettingID_GreaterThan { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThanOrEqual, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int? SettingID_GreaterThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int[] SettingID_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "SettingID", NpgsqlDbType.Integer, "setting_id")]
        public int[] SettingID_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.StartsWith, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_StartsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotStartWith, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_DoesNotStartWith { get; set; }

        [FilterOperator(QueryOperatorType.EndsWith, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_EndsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotEndWith, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_DoesNotEndWith { get; set; }

        [FilterOperator(QueryOperatorType.Contains, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_Contains { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotContain, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string SettingName_DoesNotContain { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string[] SettingName_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "SettingName", NpgsqlDbType.Varchar, "setting_name")]
        public string[] SettingName_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.StartsWith, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_StartsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotStartWith, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_DoesNotStartWith { get; set; }

        [FilterOperator(QueryOperatorType.EndsWith, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_EndsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotEndWith, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_DoesNotEndWith { get; set; }

        [FilterOperator(QueryOperatorType.Contains, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_Contains { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotContain, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string SettingValue_DoesNotContain { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string[] SettingValue_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "SettingValue", NpgsqlDbType.Varchar, "setting_value")]
        public string[] SettingValue_IsNotIn { get; set; }
    }

    /// <summary>
    ///     <para>Table name: 'user_sessions'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class UserSessionFM : IFilterModel<UserSessionPoco>
    {
        [FilterOperator(QueryOperatorType.Equal, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime? LoginDate { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime? LoginDate_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.LessThan, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime? LoginDate_LessThan { get; set; }

        [FilterOperator(QueryOperatorType.LessThanOrEqual, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime? LoginDate_LessThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThan, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime? LoginDate_GreaterThan { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThanOrEqual, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime? LoginDate_GreaterThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime[] LoginDate_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "LoginDate", NpgsqlDbType.Timestamp, "login_date")]
        public DateTime[] LoginDate_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int? SessionID { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int? SessionID_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.LessThan, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int? SessionID_LessThan { get; set; }

        [FilterOperator(QueryOperatorType.LessThanOrEqual, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int? SessionID_LessThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThan, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int? SessionID_GreaterThan { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThanOrEqual, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int? SessionID_GreaterThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int[] SessionID_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "SessionID", NpgsqlDbType.Integer, "session_id")]
        public int[] SessionID_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.LessThan, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_LessThan { get; set; }

        [FilterOperator(QueryOperatorType.LessThanOrEqual, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_LessThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThan, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_GreaterThan { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThanOrEqual, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_GreaterThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int[] UserID_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int[] UserID_IsNotIn { get; set; }
    }

    /// <summary>
    ///     <para>Table name: 'users'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class UserFM : IFilterModel<UserPoco>
    {
        [FilterOperator(QueryOperatorType.Equal, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.StartsWith, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_StartsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotStartWith, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_DoesNotStartWith { get; set; }

        [FilterOperator(QueryOperatorType.EndsWith, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_EndsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotEndWith, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_DoesNotEndWith { get; set; }

        [FilterOperator(QueryOperatorType.Contains, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_Contains { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotContain, "Password", NpgsqlDbType.Varchar, "password")]
        public string Password_DoesNotContain { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "Password", NpgsqlDbType.Varchar, "password")]
        public string[] Password_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "Password", NpgsqlDbType.Varchar, "password")]
        public string[] Password_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "RegistrationDate", NpgsqlDbType.Timestamp, "registration_date")]
        public DateTime? RegistrationDate { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "RegistrationDate", NpgsqlDbType.Timestamp, "registration_date")]
        public DateTime? RegistrationDate_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.LessThan, "RegistrationDate", NpgsqlDbType.Timestamp, "registration_date")]
        public DateTime? RegistrationDate_LessThan { get; set; }

        [FilterOperator(QueryOperatorType.LessThanOrEqual, "RegistrationDate", NpgsqlDbType.Timestamp,
            "registration_date")]
        public DateTime? RegistrationDate_LessThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThan, "RegistrationDate", NpgsqlDbType.Timestamp, "registration_date")]
        public DateTime? RegistrationDate_GreaterThan { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThanOrEqual, "RegistrationDate", NpgsqlDbType.Timestamp,
            "registration_date")]
        public DateTime? RegistrationDate_GreaterThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "RegistrationDate", NpgsqlDbType.Timestamp, "registration_date")]
        public DateTime[] RegistrationDate_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "RegistrationDate", NpgsqlDbType.Timestamp, "registration_date")]
        public DateTime[] RegistrationDate_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.LessThan, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_LessThan { get; set; }

        [FilterOperator(QueryOperatorType.LessThanOrEqual, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_LessThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThan, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_GreaterThan { get; set; }

        [FilterOperator(QueryOperatorType.GreaterThanOrEqual, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int? UserID_GreaterThanOrEqual { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int[] UserID_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "UserID", NpgsqlDbType.Integer, "user_id")]
        public int[] UserID_IsNotIn { get; set; }

        [FilterOperator(QueryOperatorType.Equal, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username { get; set; }

        [FilterOperator(QueryOperatorType.NotEqual, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_NotEqual { get; set; }

        [FilterOperator(QueryOperatorType.StartsWith, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_StartsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotStartWith, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_DoesNotStartWith { get; set; }

        [FilterOperator(QueryOperatorType.EndsWith, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_EndsWith { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotEndWith, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_DoesNotEndWith { get; set; }

        [FilterOperator(QueryOperatorType.Contains, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_Contains { get; set; }

        [FilterOperator(QueryOperatorType.DoesNotContain, "Username", NpgsqlDbType.Varchar, "username")]
        public string Username_DoesNotContain { get; set; }

        [FilterOperator(QueryOperatorType.IsIn, "Username", NpgsqlDbType.Varchar, "username")]
        public string[] Username_IsIn { get; set; }

        [FilterOperator(QueryOperatorType.IsNotIn, "Username", NpgsqlDbType.Varchar, "username")]
        public string[] Username_IsNotIn { get; set; }
    }

    /// <summary>
    ///     <para>Table name: 'system_settings'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class SystemSettingBM : IBusinessModel<SystemSettingPoco>
    {
        /// <summary>
        ///     <para>Column name: 'setting_id'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>Primary key of table: 'system_settings'.</para>
        ///     <para>Primary key constraint name: 'system_settings_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int SettingID { get; set; }

        /// <summary>
        ///     <para>Column name: 'setting_name'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        ///     <para>Column name: 'setting_value'.</para>
        ///     <para>Table name: 'system_settings'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string SettingValue { get; set; }

        public SystemSettingPoco ToPoco()
        {
            return new SystemSettingPoco
            {
                SettingID = this.SettingID,
                SettingName = this.SettingName,
                SettingValue = this.SettingValue
            };
        }
    }

    /// <summary>
    ///     <para>Table name: 'user_sessions'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public class UserSessionBM : IBusinessModel<UserSessionPoco>
    {
        /// <summary>
        ///     <para>Column name: 'login_date'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        ///     <para>CLR type: 'DateTime'.</para>
        ///     <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        public DateTime LoginDate { get; set; }

        /// <summary>
        ///     <para>Column name: 'session_id'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>Primary key of table: 'user_sessions'.</para>
        ///     <para>Primary key constraint name: 'user_sessions_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int SessionID { get; set; }

        /// <summary>
        ///     <para>Column name: 'user_id'.</para>
        ///     <para>Table name: 'user_sessions'.</para>
        ///     <para>Foreign key column [public.user_sessions.user_id -> public.users.user_id].</para>
        ///     <para>Foreign key constraint name: 'user_sessions_user_id_fkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int UserID { get; set; }

        public UserSessionPoco ToPoco()
        {
            return new UserSessionPoco
            {
                LoginDate = this.LoginDate,
                SessionID = this.SessionID,
                UserID = this.UserID
            };
        }
    }

    /// <summary>
    ///     <para>Table name: 'users'.</para>
    ///     <para>Table schema: 'public'.</para>
    /// </summary>
    public partial class UserBM : IBusinessModel<UserPoco>
    {
        /// <summary>
        ///     <para>Column name: 'password'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     <para>Column name: 'registration_date'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        ///     <para>CLR type: 'DateTime'.</para>
        ///     <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        ///     <para>Column name: 'user_id'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>Primary key of table: 'users'.</para>
        ///     <para>Primary key constraint name: 'users_pkey'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'integer'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        ///     <para>CLR type: 'int'.</para>
        ///     <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        ///     <para>Column name: 'username'.</para>
        ///     <para>Table name: 'users'.</para>
        ///     <para>This column is not nullable.</para>
        ///     <para>PostgreSQL data type: 'character varying'.</para>
        ///     <para>NpgsqlDbType: 'NpgsqlDbType.Varchar'.</para>
        ///     <para>CLR type: 'string'.</para>
        ///     <para>linq2db data type: 'DataType.NVarChar'.</para>
        /// </summary>
        public string Username { get; set; }

        public UserPoco ToPoco()
        {
            return new UserPoco
            {
                Password = this.Password,
                RegistrationDate = this.RegistrationDate,
                UserID = this.UserID,
                Username = this.Username
            };
        }
    }

    public class DbPocos : IDbPocos<DbPocos>
    {
        /// <summary>
        ///     <para>Database table 'system_settings'.</para>
        /// </summary>
        public IQueryable<SystemSettingPoco> SystemSettings => this.DbService.GetTable<SystemSettingPoco>();

        /// <summary>
        ///     <para>Database table 'user_sessions'.</para>
        /// </summary>
        public IQueryable<UserSessionPoco> UserSessions => this.DbService.GetTable<UserSessionPoco>();

        /// <summary>
        ///     <para>Database table 'users'.</para>
        /// </summary>
        public IQueryable<UserPoco> Users => this.DbService.GetTable<UserPoco>();


        public IDbService<DbPocos> DbService { private get; set; }

        /// <summary>
        ///     <para>Database table 'system_settings'.</para>
        ///     <para>Filter model 'SystemSettingFM'.</para>
        ///     <para>Catalog model 'SystemSettingCM'.</para>
        /// </summary>
        public Task<List<SystemSettingCM>> Filter(SystemSettingFM filter)
        {
            return this.DbService.FilterInternal<SystemSettingPoco, SystemSettingCM>(filter);
        }

        /// <summary>
        ///     <para>Database table 'user_sessions'.</para>
        ///     <para>Filter model 'UserSessionFM'.</para>
        ///     <para>Catalog model 'UserSessionCM'.</para>
        /// </summary>
        public Task<List<UserSessionCM>> Filter(UserSessionFM filter)
        {
            return this.DbService.FilterInternal<UserSessionPoco, UserSessionCM>(filter);
        }

        /// <summary>
        ///     <para>Database table 'users'.</para>
        ///     <para>Filter model 'UserFM'.</para>
        ///     <para>Catalog model 'UserCM'.</para>
        /// </summary>
        public Task<List<UserCM>> Filter(UserFM filter)
        {
            return this.DbService.FilterInternal<UserPoco, UserCM>(filter);
        }
    }

    public static class DbPocosExtensions
    {
        /// <summary>
        ///     <para>Database table 'system_settings'.</para>
        /// </summary>
        public static IQueryable<SystemSettingCM> SelectCm(this IQueryable<SystemSettingPoco> collection)
        {
            return collection.SelectCm<SystemSettingPoco, SystemSettingCM>();
        }

        /// <summary>
        ///     <para>Database table 'user_sessions'.</para>
        /// </summary>
        public static IQueryable<UserSessionCM> SelectCm(this IQueryable<UserSessionPoco> collection)
        {
            return collection.SelectCm<UserSessionPoco, UserSessionCM>();
        }

        /// <summary>
        ///     <para>Database table 'users'.</para>
        /// </summary>
        public static IQueryable<UserCM> SelectCm(this IQueryable<UserPoco> collection)
        {
            return collection.SelectCm<UserPoco, UserCM>();
        }
    }

    public class DbMetadata : IDbMetadata
    {
        internal static TableMetadataModel<SystemSettingPoco> SystemSettingPocoMetadata;

        internal static TableMetadataModel<UserSessionPoco> UserSessionPocoMetadata;

        internal static TableMetadataModel<UserPoco> UserPocoMetadata;

        private static readonly object InitLock = new object();

        private static bool Initialized;

        static DbMetadata()
        {
            Initialize();
        }

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once CyclomaticComplexity
        private static void InitializeInternal()
        {
            SystemSettingPocoMetadata = new TableMetadataModel<SystemSettingPoco>
            {
                ClassName = "SystemSetting",
                PluralClassName = "SystemSettings",
                TableName = "system_settings",
                TableSchema = "public",
                PrimaryKeyColumnName = "setting_id",
                PrimaryKeyPropertyName = "SettingID",
                GetPrimaryKey = instance => instance.SettingID,
                SetPrimaryKey = (instance, val) => instance.SettingID = val,
                IsNew = instance => instance.SettingID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "int",
                        ClrType = typeof(int),
                        ClrNonNullableTypeName = "int",
                        ClrNonNullableType = typeof(int),
                        ClrNullableTypeName = "int?",
                        ClrNullableType = typeof(int?),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "setting_id",
                        DbDataType = "integer",
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName =
                            "system_settings_pkey" == string.Empty ? null : "system_settings_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("True"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("False"),
                        Linq2dbDataTypeName = "DataType.Int32",
                        Linq2dbDataType = DataType.Int32,
                        NpgsDataTypeName = "NpgsqlDbType.Integer",
                        NpgsDataType = NpgsqlDbType.Integer,
                        PropertyName = "SettingID",
                        TableName = "system_settings",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "string",
                        ClrType = typeof(string),
                        ClrNonNullableTypeName = "string",
                        ClrNonNullableType = typeof(string),
                        ClrNullableTypeName = "string",
                        ClrNullableType = typeof(string),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "setting_name",
                        DbDataType = "character varying",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("False"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("True"),
                        Linq2dbDataTypeName = "DataType.NVarChar",
                        Linq2dbDataType = DataType.NVarChar,
                        NpgsDataTypeName = "NpgsqlDbType.Varchar",
                        NpgsDataType = NpgsqlDbType.Varchar,
                        PropertyName = "SettingName",
                        TableName = "system_settings",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "string",
                        ClrType = typeof(string),
                        ClrNonNullableTypeName = "string",
                        ClrNonNullableType = typeof(string),
                        ClrNullableTypeName = "string",
                        ClrNullableType = typeof(string),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "setting_value",
                        DbDataType = "character varying",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("False"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("True"),
                        Linq2dbDataTypeName = "DataType.NVarChar",
                        Linq2dbDataType = DataType.NVarChar,
                        NpgsDataTypeName = "NpgsqlDbType.Varchar",
                        NpgsDataType = NpgsqlDbType.Varchar,
                        PropertyName = "SettingValue",
                        TableName = "system_settings",
                        TableSchema = "public"
                    }
                }
            };

            SystemSettingPocoMetadata.Clone = DbCodeGenerator.GetClone<SystemSettingPoco>();

            SystemSettingPocoMetadata.GenerateParameters =
                DbCodeGenerator.GetGenerateParameters(SystemSettingPocoMetadata);

            SystemSettingPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(SystemSettingPocoMetadata);
            SystemSettingPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(SystemSettingPocoMetadata);
            SystemSettingPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(SystemSettingPocoMetadata);

            SystemSettingPocoMetadata.ParseFm =
                DbCodeGenerator.GetParseFm(SystemSettingPocoMetadata, typeof(SystemSettingFM));

            UserSessionPocoMetadata = new TableMetadataModel<UserSessionPoco>
            {
                ClassName = "UserSession",
                PluralClassName = "UserSessions",
                TableName = "user_sessions",
                TableSchema = "public",
                PrimaryKeyColumnName = "session_id",
                PrimaryKeyPropertyName = "SessionID",
                GetPrimaryKey = instance => instance.SessionID,
                SetPrimaryKey = (instance, val) => instance.SessionID = val,
                IsNew = instance => instance.SessionID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "DateTime",
                        ClrType = typeof(DateTime),
                        ClrNonNullableTypeName = "DateTime",
                        ClrNonNullableType = typeof(DateTime),
                        ClrNullableTypeName = "DateTime?",
                        ClrNullableType = typeof(DateTime?),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "login_date",
                        DbDataType = "timestamp without time zone",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("True"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("False"),
                        Linq2dbDataTypeName = "DataType.DateTime2",
                        Linq2dbDataType = DataType.DateTime2,
                        NpgsDataTypeName = "NpgsqlDbType.Timestamp",
                        NpgsDataType = NpgsqlDbType.Timestamp,
                        PropertyName = "LoginDate",
                        TableName = "user_sessions",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "int",
                        ClrType = typeof(int),
                        ClrNonNullableTypeName = "int",
                        ClrNonNullableType = typeof(int),
                        ClrNullableTypeName = "int?",
                        ClrNullableType = typeof(int?),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "session_id",
                        DbDataType = "integer",
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "user_sessions_pkey" == string.Empty ? null : "user_sessions_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("True"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("False"),
                        Linq2dbDataTypeName = "DataType.Int32",
                        Linq2dbDataType = DataType.Int32,
                        NpgsDataTypeName = "NpgsqlDbType.Integer",
                        NpgsDataType = NpgsqlDbType.Integer,
                        PropertyName = "SessionID",
                        TableName = "user_sessions",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "int",
                        ClrType = typeof(int),
                        ClrNonNullableTypeName = "int",
                        ClrNonNullableType = typeof(int),
                        ClrNullableTypeName = "int?",
                        ClrNullableType = typeof(int?),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "user_id",
                        DbDataType = "integer",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("True"),
                        ForeignKeyConstraintName = "user_sessions_user_id_fkey" == string.Empty
                            ? null
                            : "user_sessions_user_id_fkey",
                        ForeignKeyReferenceColumnName = "user_id" == string.Empty ? null : "user_id",
                        ForeignKeyReferenceSchemaName = "public" == string.Empty ? null : "public",
                        ForeignKeyReferenceTableName = "users" == string.Empty ? null : "users",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("True"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("False"),
                        Linq2dbDataTypeName = "DataType.Int32",
                        Linq2dbDataType = DataType.Int32,
                        NpgsDataTypeName = "NpgsqlDbType.Integer",
                        NpgsDataType = NpgsqlDbType.Integer,
                        PropertyName = "UserID",
                        TableName = "user_sessions",
                        TableSchema = "public"
                    }
                }
            };

            UserSessionPocoMetadata.Clone = DbCodeGenerator.GetClone<UserSessionPoco>();
            UserSessionPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(UserSessionPocoMetadata);
            UserSessionPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(UserSessionPocoMetadata);
            UserSessionPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(UserSessionPocoMetadata);
            UserSessionPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(UserSessionPocoMetadata);

            UserSessionPocoMetadata.ParseFm =
                DbCodeGenerator.GetParseFm(UserSessionPocoMetadata, typeof(UserSessionFM));

            UserPocoMetadata = new TableMetadataModel<UserPoco>
            {
                ClassName = "User",
                PluralClassName = "Users",
                TableName = "users",
                TableSchema = "public",
                PrimaryKeyColumnName = "user_id",
                PrimaryKeyPropertyName = "UserID",
                GetPrimaryKey = instance => instance.UserID,
                SetPrimaryKey = (instance, val) => instance.UserID = val,
                IsNew = instance => instance.UserID == default,
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "string",
                        ClrType = typeof(string),
                        ClrNonNullableTypeName = "string",
                        ClrNonNullableType = typeof(string),
                        ClrNullableTypeName = "string",
                        ClrNullableType = typeof(string),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "password",
                        DbDataType = "character varying",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("False"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("True"),
                        Linq2dbDataTypeName = "DataType.NVarChar",
                        Linq2dbDataType = DataType.NVarChar,
                        NpgsDataTypeName = "NpgsqlDbType.Varchar",
                        NpgsDataType = NpgsqlDbType.Varchar,
                        PropertyName = "Password",
                        TableName = "users",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "DateTime",
                        ClrType = typeof(DateTime),
                        ClrNonNullableTypeName = "DateTime",
                        ClrNonNullableType = typeof(DateTime),
                        ClrNullableTypeName = "DateTime?",
                        ClrNullableType = typeof(DateTime?),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "registration_date",
                        DbDataType = "timestamp without time zone",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("True"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("False"),
                        Linq2dbDataTypeName = "DataType.DateTime2",
                        Linq2dbDataType = DataType.DateTime2,
                        NpgsDataTypeName = "NpgsqlDbType.Timestamp",
                        NpgsDataType = NpgsqlDbType.Timestamp,
                        PropertyName = "RegistrationDate",
                        TableName = "users",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "int",
                        ClrType = typeof(int),
                        ClrNonNullableTypeName = "int",
                        ClrNonNullableType = typeof(int),
                        ClrNullableTypeName = "int?",
                        ClrNullableType = typeof(int?),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "user_id",
                        DbDataType = "integer",
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "users_pkey" == string.Empty ? null : "users_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("True"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("False"),
                        Linq2dbDataTypeName = "DataType.Int32",
                        Linq2dbDataType = DataType.Int32,
                        NpgsDataTypeName = "NpgsqlDbType.Integer",
                        NpgsDataType = NpgsqlDbType.Integer,
                        PropertyName = "UserID",
                        TableName = "users",
                        TableSchema = "public"
                    },
                    new ColumnMetadataModel
                    {
                        ClrTypeName = "string",
                        ClrType = typeof(string),
                        ClrNonNullableTypeName = "string",
                        ClrNonNullableType = typeof(string),
                        ClrNullableTypeName = "string",
                        ClrNullableType = typeof(string),
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "username",
                        DbDataType = "character varying",
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        IsNullable = bool.Parse("False"),
                        IsClrValueType = bool.Parse("False"),
                        IsClrNullableType = bool.Parse("False"),
                        IsClrReferenceType = bool.Parse("True"),
                        Linq2dbDataTypeName = "DataType.NVarChar",
                        Linq2dbDataType = DataType.NVarChar,
                        NpgsDataTypeName = "NpgsqlDbType.Varchar",
                        NpgsDataType = NpgsqlDbType.Varchar,
                        PropertyName = "Username",
                        TableName = "users",
                        TableSchema = "public"
                    }
                }
            };

            UserPocoMetadata.Clone = DbCodeGenerator.GetClone<UserPoco>();
            UserPocoMetadata.GenerateParameters = DbCodeGenerator.GetGenerateParameters(UserPocoMetadata);
            UserPocoMetadata.WriteToImporter = DbCodeGenerator.GetWriteToImporter(UserPocoMetadata);
            UserPocoMetadata.GetColumnChanges = DbCodeGenerator.GetGetColumnChanges(UserPocoMetadata);
            UserPocoMetadata.GetAllColumns = DbCodeGenerator.GetGetAllColumns(UserPocoMetadata);
            UserPocoMetadata.ParseFm = DbCodeGenerator.GetParseFm(UserPocoMetadata, typeof(UserFM));
        }

        public static void Initialize()
        {
            if (Initialized)
            {
                return;
            }

            lock (InitLock)
            {
                if (Initialized)
                {
                    return;
                }

                InitializeInternal();

                Initialized = true;
            }
        }
    }
}