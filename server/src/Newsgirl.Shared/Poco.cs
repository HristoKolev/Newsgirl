// ReSharper disable InconsistentNaming
// ReSharper disable HeuristicUnreachableCode

namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using LinqToDB;
    using LinqToDB.Mapping;
    using Npgsql;
    using NpgsqlTypes;
    using Postgres;

    /// <summary>
    /// <para>Table name: 'feed_items'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "feed_items")]
    [ExcludeFromCodeCoverage]
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
        [LinqToDB.Mapping.NotNull]
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
        [LinqToDB.Mapping.NotNull]
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
        [PrimaryKey]
        [Identity]
        [Column(Name = "feed_item_id", DataType = DataType.Int32)]
        public int FeedItemID { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_string_id'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "feed_item_string_id", DataType = DataType.Text)]
        public string FeedItemStringID { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_string_id_hash'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "feed_item_string_id_hash", DataType = DataType.Int64)]
        public long FeedItemStringIDHash { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_item_title'.</para>
        /// <para>Table name: 'feed_items'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
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

        public NpgsqlParameter[] GetNonPkParameters()
        {
            // ReSharper disable once RedundantExplicitArrayCreation
            return new NpgsqlParameter[]
            {
                new NpgsqlParameter<int>
                {
                    TypedValue = this.FeedID,
                    NpgsqlDbType = NpgsqlDbType.Integer,
                },
                new NpgsqlParameter<DateTime>
                {
                    TypedValue = this.FeedItemAddedTime,
                    NpgsqlDbType = NpgsqlDbType.Timestamp,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.FeedItemDescription,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.FeedItemStringID,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<long>
                {
                    TypedValue = this.FeedItemStringIDHash,
                    NpgsqlDbType = NpgsqlDbType.Bigint,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.FeedItemTitle,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.FeedItemUrl,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
            };
        }

        public int GetPrimaryKey()
        {
            return this.FeedItemID;
        }

        public void SetPrimaryKey(int value)
        {
            this.FeedItemID = value;
        }

        public bool IsNew()
        {
            return this.FeedItemID == default;
        }

        public async Task WriteToImporter(NpgsqlBinaryImporter importer)
        {
            await importer.WriteAsync(this.FeedID, NpgsqlDbType.Integer);

            await importer.WriteAsync(this.FeedItemAddedTime, NpgsqlDbType.Timestamp);

            if (this.FeedItemDescription == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedItemDescription, NpgsqlDbType.Text);
            }

            if (this.FeedItemStringID == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedItemStringID, NpgsqlDbType.Text);
            }

            await importer.WriteAsync(this.FeedItemStringIDHash, NpgsqlDbType.Bigint);

            if (this.FeedItemTitle == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedItemTitle, NpgsqlDbType.Text);
            }

            if (this.FeedItemUrl == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedItemUrl, NpgsqlDbType.Text);
            }
        }

        public static TableMetadataModel Metadata => DbMetadata.FeedItemPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'feeds'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "feeds")]
    [ExcludeFromCodeCoverage]
    public class FeedPoco : IPoco<FeedPoco>
    {
        /// <summary>
        /// <para>Column name: 'feed_content_hash'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long?'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "feed_content_hash", DataType = DataType.Int64)]
        public long? FeedContentHash { get; set; }

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
        [PrimaryKey]
        [Identity]
        [Column(Name = "feed_id", DataType = DataType.Int32)]
        public int FeedID { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_items_hash'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'bigint'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Bigint'.</para>
        /// <para>CLR type: 'long?'.</para>
        /// <para>linq2db data type: 'DataType.Int64'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "feed_items_hash", DataType = DataType.Int64)]
        public long? FeedItemsHash { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_name'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "feed_name", DataType = DataType.Text)]
        public string FeedName { get; set; }

        /// <summary>
        /// <para>Column name: 'feed_url'.</para>
        /// <para>Table name: 'feeds'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "feed_url", DataType = DataType.Text)]
        public string FeedUrl { get; set; }

        public NpgsqlParameter[] GetNonPkParameters()
        {
            // ReSharper disable once RedundantExplicitArrayCreation
            return new NpgsqlParameter[]
            {
                this.FeedContentHash.HasValue
                    ? new NpgsqlParameter<long> {TypedValue = this.FeedContentHash.Value, NpgsqlDbType = NpgsqlDbType.Bigint}
                    : new NpgsqlParameter {Value = DBNull.Value},
                this.FeedItemsHash.HasValue
                    ? new NpgsqlParameter<long> {TypedValue = this.FeedItemsHash.Value, NpgsqlDbType = NpgsqlDbType.Bigint}
                    : new NpgsqlParameter {Value = DBNull.Value},
                new NpgsqlParameter<string>
                {
                    TypedValue = this.FeedName,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.FeedUrl,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
            };
        }

        public int GetPrimaryKey()
        {
            return this.FeedID;
        }

        public void SetPrimaryKey(int value)
        {
            this.FeedID = value;
        }

        public bool IsNew()
        {
            return this.FeedID == default;
        }

        public async Task WriteToImporter(NpgsqlBinaryImporter importer)
        {
            if (!this.FeedContentHash.HasValue)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedContentHash.Value, NpgsqlDbType.Bigint);
            }

            if (!this.FeedItemsHash.HasValue)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedItemsHash.Value, NpgsqlDbType.Bigint);
            }

            if (this.FeedName == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedName, NpgsqlDbType.Text);
            }

            if (this.FeedUrl == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.FeedUrl, NpgsqlDbType.Text);
            }
        }

        public static TableMetadataModel Metadata => DbMetadata.FeedPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'user_logins'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "user_logins")]
    [ExcludeFromCodeCoverage]
    public class UserLoginPoco : IPoco<UserLoginPoco>
    {
        /// <summary>
        /// <para>Column name: 'enabled'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'boolean'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Boolean'.</para>
        /// <para>CLR type: 'bool'.</para>
        /// <para>linq2db data type: 'DataType.Boolean'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "enabled", DataType = DataType.Boolean)]
        public bool Enabled { get; set; }

        /// <summary>
        /// <para>Column name: 'login_id'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>Primary key of table: 'user_logins'.</para>
        /// <para>Primary key constraint name: 'user_logins_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "login_id", DataType = DataType.Int32)]
        public int LoginID { get; set; }

        /// <summary>
        /// <para>Column name: 'password_hash'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "password_hash", DataType = DataType.Text)]
        public string PasswordHash { get; set; }

        /// <summary>
        /// <para>Column name: 'user_profile_id'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>Foreign key column [public.user_logins.user_profile_id -> public.user_profiles.user_profile_id].</para>
        /// <para>Foreign key constraint name: 'user_logins_user_profile_id_fkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "user_profile_id", DataType = DataType.Int32)]
        public int UserProfileID { get; set; }

        /// <summary>
        /// <para>Column name: 'username'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "username", DataType = DataType.Text)]
        public string Username { get; set; }

        /// <summary>
        /// <para>Column name: 'verification_code'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "verification_code", DataType = DataType.Text)]
        public string VerificationCode { get; set; }

        /// <summary>
        /// <para>Column name: 'verified'.</para>
        /// <para>Table name: 'user_logins'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'boolean'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Boolean'.</para>
        /// <para>CLR type: 'bool'.</para>
        /// <para>linq2db data type: 'DataType.Boolean'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "verified", DataType = DataType.Boolean)]
        public bool Verified { get; set; }

        public NpgsqlParameter[] GetNonPkParameters()
        {
            // ReSharper disable once RedundantExplicitArrayCreation
            return new NpgsqlParameter[]
            {
                new NpgsqlParameter<bool>
                {
                    TypedValue = this.Enabled,
                    NpgsqlDbType = NpgsqlDbType.Boolean,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.PasswordHash,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<int>
                {
                    TypedValue = this.UserProfileID,
                    NpgsqlDbType = NpgsqlDbType.Integer,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.Username,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<string>
                {
                    TypedValue = this.VerificationCode,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<bool>
                {
                    TypedValue = this.Verified,
                    NpgsqlDbType = NpgsqlDbType.Boolean,
                },
            };
        }

        public int GetPrimaryKey()
        {
            return this.LoginID;
        }

        public void SetPrimaryKey(int value)
        {
            this.LoginID = value;
        }

        public bool IsNew()
        {
            return this.LoginID == default;
        }

        public async Task WriteToImporter(NpgsqlBinaryImporter importer)
        {
            await importer.WriteAsync(this.Enabled, NpgsqlDbType.Boolean);

            if (this.PasswordHash == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.PasswordHash, NpgsqlDbType.Text);
            }

            await importer.WriteAsync(this.UserProfileID, NpgsqlDbType.Integer);

            if (this.Username == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.Username, NpgsqlDbType.Text);
            }

            if (this.VerificationCode == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.VerificationCode, NpgsqlDbType.Text);
            }

            await importer.WriteAsync(this.Verified, NpgsqlDbType.Boolean);
        }

        public static TableMetadataModel Metadata => DbMetadata.UserLoginPocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'user_profiles'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "user_profiles")]
    [ExcludeFromCodeCoverage]
    public class UserProfilePoco : IPoco<UserProfilePoco>
    {
        /// <summary>
        /// <para>Column name: 'email_address'.</para>
        /// <para>Table name: 'user_profiles'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "email_address", DataType = DataType.Text)]
        public string EmailAddress { get; set; }

        /// <summary>
        /// <para>Column name: 'registration_date'.</para>
        /// <para>Table name: 'user_profiles'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "registration_date", DataType = DataType.DateTime2)]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// <para>Column name: 'user_profile_id'.</para>
        /// <para>Table name: 'user_profiles'.</para>
        /// <para>Primary key of table: 'user_profiles'.</para>
        /// <para>Primary key constraint name: 'user_profiles_pkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Column(Name = "user_profile_id", DataType = DataType.Int32)]
        public int UserProfileID { get; set; }

        public NpgsqlParameter[] GetNonPkParameters()
        {
            // ReSharper disable once RedundantExplicitArrayCreation
            return new NpgsqlParameter[]
            {
                new NpgsqlParameter<string>
                {
                    TypedValue = this.EmailAddress,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                new NpgsqlParameter<DateTime>
                {
                    TypedValue = this.RegistrationDate,
                    NpgsqlDbType = NpgsqlDbType.Timestamp,
                },
            };
        }

        public int GetPrimaryKey()
        {
            return this.UserProfileID;
        }

        public void SetPrimaryKey(int value)
        {
            this.UserProfileID = value;
        }

        public bool IsNew()
        {
            return this.UserProfileID == default;
        }

        public async Task WriteToImporter(NpgsqlBinaryImporter importer)
        {
            if (this.EmailAddress == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.EmailAddress, NpgsqlDbType.Text);
            }

            await importer.WriteAsync(this.RegistrationDate, NpgsqlDbType.Timestamp);
        }

        public static TableMetadataModel Metadata => DbMetadata.UserProfilePocoMetadata;
    }

    /// <summary>
    /// <para>Table name: 'user_sessions'.</para>
    /// <para>Table schema: 'public'.</para>
    /// </summary>
    [Table(Schema = "public", Name = "user_sessions")]
    [ExcludeFromCodeCoverage]
    public class UserSessionPoco : IPoco<UserSessionPoco>
    {
        /// <summary>
        /// <para>Column name: 'csrf_token'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'text'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Text'.</para>
        /// <para>CLR type: 'string'.</para>
        /// <para>linq2db data type: 'DataType.Text'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "csrf_token", DataType = DataType.Text)]
        public string CsrfToken { get; set; }

        /// <summary>
        /// <para>Column name: 'expiration_date'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>This column is nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime?'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [Nullable]
        [Column(Name = "expiration_date", DataType = DataType.DateTime2)]
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// <para>Column name: 'login_date'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'timestamp without time zone'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Timestamp'.</para>
        /// <para>CLR type: 'DateTime'.</para>
        /// <para>linq2db data type: 'DataType.DateTime2'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "login_date", DataType = DataType.DateTime2)]
        public DateTime LoginDate { get; set; }

        /// <summary>
        /// <para>Column name: 'login_id'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>Foreign key column [public.user_sessions.login_id -> public.user_logins.login_id].</para>
        /// <para>Foreign key constraint name: 'user_sessions_login_id_fkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "login_id", DataType = DataType.Int32)]
        public int LoginID { get; set; }

        /// <summary>
        /// <para>Column name: 'profile_id'.</para>
        /// <para>Table name: 'user_sessions'.</para>
        /// <para>Foreign key column [public.user_sessions.profile_id -> public.user_profiles.user_profile_id].</para>
        /// <para>Foreign key constraint name: 'user_sessions_profile_id_fkey'.</para>
        /// <para>This column is not nullable.</para>
        /// <para>PostgreSQL data type: 'integer'.</para>
        /// <para>NpgsqlDbType: 'NpgsqlDbType.Integer'.</para>
        /// <para>CLR type: 'int'.</para>
        /// <para>linq2db data type: 'DataType.Int32'.</para>
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        [Column(Name = "profile_id", DataType = DataType.Int32)]
        public int ProfileID { get; set; }

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
        [PrimaryKey]
        [Identity]
        [Column(Name = "session_id", DataType = DataType.Int32)]
        public int SessionID { get; set; }

        public NpgsqlParameter[] GetNonPkParameters()
        {
            // ReSharper disable once RedundantExplicitArrayCreation
            return new NpgsqlParameter[]
            {
                new NpgsqlParameter<string>
                {
                    TypedValue = this.CsrfToken,
                    NpgsqlDbType = NpgsqlDbType.Text,
                },
                this.ExpirationDate.HasValue
                    ? new NpgsqlParameter<DateTime> {TypedValue = this.ExpirationDate.Value, NpgsqlDbType = NpgsqlDbType.Timestamp}
                    : new NpgsqlParameter {Value = DBNull.Value},
                new NpgsqlParameter<DateTime>
                {
                    TypedValue = this.LoginDate,
                    NpgsqlDbType = NpgsqlDbType.Timestamp,
                },
                new NpgsqlParameter<int>
                {
                    TypedValue = this.LoginID,
                    NpgsqlDbType = NpgsqlDbType.Integer,
                },
                new NpgsqlParameter<int>
                {
                    TypedValue = this.ProfileID,
                    NpgsqlDbType = NpgsqlDbType.Integer,
                },
            };
        }

        public int GetPrimaryKey()
        {
            return this.SessionID;
        }

        public void SetPrimaryKey(int value)
        {
            this.SessionID = value;
        }

        public bool IsNew()
        {
            return this.SessionID == default;
        }

        public async Task WriteToImporter(NpgsqlBinaryImporter importer)
        {
            if (this.CsrfToken == null)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.CsrfToken, NpgsqlDbType.Text);
            }

            if (!this.ExpirationDate.HasValue)
            {
                await importer.WriteNullAsync();
            }
            else
            {
                await importer.WriteAsync(this.ExpirationDate.Value, NpgsqlDbType.Timestamp);
            }

            await importer.WriteAsync(this.LoginDate, NpgsqlDbType.Timestamp);

            await importer.WriteAsync(this.LoginID, NpgsqlDbType.Integer);

            await importer.WriteAsync(this.ProfileID, NpgsqlDbType.Integer);
        }

        public static TableMetadataModel Metadata => DbMetadata.UserSessionPocoMetadata;
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
        /// <para>Database table 'user_logins'.</para>
        /// </summary>
        public IQueryable<UserLoginPoco> UserLogins => this.LinqProvider.GetTable<UserLoginPoco>();

        /// <summary>
        /// <para>Database table 'user_profiles'.</para>
        /// </summary>
        public IQueryable<UserProfilePoco> UserProfiles => this.LinqProvider.GetTable<UserProfilePoco>();

        /// <summary>
        /// <para>Database table 'user_sessions'.</para>
        /// </summary>
        public IQueryable<UserSessionPoco> UserSessions => this.LinqProvider.GetTable<UserSessionPoco>();

        /// <summary>
        /// <para>Database function 'get_missing_feed_items'.</para>
        /// </summary>
        [Sql.FunctionAttribute(ServerSideOnly = true, Name = "get_missing_feed_items")]
        public static long[] GetMissingFeedItems(int? p_feed_id, long[] p_new_item_hashes)
        {
            throw new NotImplementedException();
        }

        public ILinqProvider LinqProvider { private get; set; }
    }

    public class DbMetadata
    {
        internal static readonly TableMetadataModel FeedItemPocoMetadata;

        internal static readonly TableMetadataModel FeedPocoMetadata;

        internal static readonly TableMetadataModel UserLoginPocoMetadata;

        internal static readonly TableMetadataModel UserProfilePocoMetadata;

        internal static readonly TableMetadataModel UserSessionPocoMetadata;

        // ReSharper disable once CollectionNeverQueried.Global
        internal static readonly List<FunctionMetadataModel> Functions = new List<FunctionMetadataModel>();

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once CyclomaticComplexity
        static DbMetadata()
        {
            FeedItemPocoMetadata = new TableMetadataModel
            {
                ClassName = "FeedItem",
                PluralClassName = "FeedItems",
                TableName = "feed_items",
                TableSchema = "public",
                PrimaryKeyColumnName = "feed_item_id",
                PrimaryKeyPropertyName = "FeedItemID",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_string_id",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemStringID",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_item_string_id_hash",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemStringIDHash",
                        TableName = "feed_items",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                },
                NonPkColumnNames = new[]
                {
                    "feed_id",
                    "feed_item_added_time",
                    "feed_item_description",
                    "feed_item_string_id",
                    "feed_item_string_id_hash",
                    "feed_item_title",
                    "feed_item_url",
                },
            };

            FeedPocoMetadata = new TableMetadataModel
            {
                ClassName = "Feed",
                PluralClassName = "Feeds",
                TableName = "feeds",
                TableSchema = "public",
                PrimaryKeyColumnName = "feed_id",
                PrimaryKeyPropertyName = "FeedID",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_content_hash",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedContentHash",
                        TableName = "feeds",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "feed_items_hash",
                        DbDataType = "bigint",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "FeedItemsHash",
                        TableName = "feeds",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                },
                NonPkColumnNames = new[]
                {
                    "feed_content_hash",
                    "feed_items_hash",
                    "feed_name",
                    "feed_url",
                },
            };

            UserLoginPocoMetadata = new TableMetadataModel
            {
                ClassName = "UserLogin",
                PluralClassName = "UserLogins",
                TableName = "user_logins",
                TableSchema = "public",
                PrimaryKeyColumnName = "login_id",
                PrimaryKeyPropertyName = "LoginID",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "enabled",
                        DbDataType = "boolean",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Enabled",
                        TableName = "user_logins",
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
                        ColumnName = "login_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "user_logins_pkey" == string.Empty ? null : "user_logins_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "LoginID",
                        TableName = "user_logins",
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
                        ColumnName = "password_hash",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "PasswordHash",
                        TableName = "user_logins",
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
                        ColumnName = "user_profile_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("True"),
                        ForeignKeyConstraintName = "user_logins_user_profile_id_fkey" == string.Empty ? null : "user_logins_user_profile_id_fkey",
                        ForeignKeyReferenceColumnName = "user_profile_id" == string.Empty ? null : "user_profile_id",
                        ForeignKeyReferenceSchemaName = "public" == string.Empty ? null : "public",
                        ForeignKeyReferenceTableName = "user_profiles" == string.Empty ? null : "user_profiles",
                        PropertyName = "UserProfileID",
                        TableName = "user_logins",
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
                        TableName = "user_logins",
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
                        ColumnName = "verification_code",
                        DbDataType = "text",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "VerificationCode",
                        TableName = "user_logins",
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
                        ColumnName = "verified",
                        DbDataType = "boolean",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "Verified",
                        TableName = "user_logins",
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
                },
                NonPkColumnNames = new[]
                {
                    "enabled",
                    "password_hash",
                    "user_profile_id",
                    "username",
                    "verification_code",
                    "verified",
                },
            };

            UserProfilePocoMetadata = new TableMetadataModel
            {
                ClassName = "UserProfile",
                PluralClassName = "UserProfiles",
                TableName = "user_profiles",
                TableSchema = "public",
                PrimaryKeyColumnName = "user_profile_id",
                PrimaryKeyPropertyName = "UserProfileID",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "email_address",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "EmailAddress",
                        TableName = "user_profiles",
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
                        TableName = "user_profiles",
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
                        ColumnName = "user_profile_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("True"),
                        PrimaryKeyConstraintName = "user_profiles_pkey" == string.Empty ? null : "user_profiles_pkey",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "UserProfileID",
                        TableName = "user_profiles",
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
                },
                NonPkColumnNames = new[]
                {
                    "email_address",
                    "registration_date",
                },
            };

            UserSessionPocoMetadata = new TableMetadataModel
            {
                ClassName = "UserSession",
                PluralClassName = "UserSessions",
                TableName = "user_sessions",
                TableSchema = "public",
                PrimaryKeyColumnName = "session_id",
                PrimaryKeyPropertyName = "SessionID",
                Columns = new List<ColumnMetadataModel>
                {
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "csrf_token",
                        DbDataType = "text",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "CsrfToken",
                        TableName = "user_sessions",
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
                        ColumnName = "expiration_date",
                        DbDataType = "timestamp without time zone",
                        IsNullable = bool.Parse("True"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("False"),
                        ForeignKeyConstraintName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceColumnName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceSchemaName = "" == string.Empty ? null : "",
                        ForeignKeyReferenceTableName = "" == string.Empty ? null : "",
                        PropertyName = "ExpirationDate",
                        TableName = "user_sessions",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "login_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("True"),
                        ForeignKeyConstraintName = "user_sessions_login_id_fkey" == string.Empty ? null : "user_sessions_login_id_fkey",
                        ForeignKeyReferenceColumnName = "login_id" == string.Empty ? null : "login_id",
                        ForeignKeyReferenceSchemaName = "public" == string.Empty ? null : "public",
                        ForeignKeyReferenceTableName = "user_logins" == string.Empty ? null : "user_logins",
                        PropertyName = "LoginID",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                        ColumnName = "profile_id",
                        DbDataType = "integer",
                        IsNullable = bool.Parse("False"),
                        IsPrimaryKey = bool.Parse("False"),
                        PrimaryKeyConstraintName = "" == string.Empty ? null : "",
                        IsForeignKey = bool.Parse("True"),
                        ForeignKeyConstraintName = "user_sessions_profile_id_fkey" == string.Empty ? null : "user_sessions_profile_id_fkey",
                        ForeignKeyReferenceColumnName = "user_profile_id" == string.Empty ? null : "user_profile_id",
                        ForeignKeyReferenceSchemaName = "public" == string.Empty ? null : "public",
                        ForeignKeyReferenceTableName = "user_profiles" == string.Empty ? null : "user_profiles",
                        PropertyName = "ProfileID",
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
                        },
                    },
                    new ColumnMetadataModel
                    {
                        ColumnComment = "" == string.Empty ? null : "",
                        Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
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
                        },
                    },
                },
                NonPkColumnNames = new[]
                {
                    "csrf_token",
                    "expiration_date",
                    "login_date",
                    "login_id",
                    "profile_id",
                },
            };

            Functions.Add(new FunctionMetadataModel
            {
                SchemaName = "public" == string.Empty ? null : "public",
                FunctionName = "get_missing_feed_items" == string.Empty ? null : "get_missing_feed_items",
                MethodName = "GetMissingFeedItems" == string.Empty ? null : "GetMissingFeedItems",
                FunctionReturnTypeName = "_int8" == string.Empty ? null : "_int8",
                FunctionComment = "" == string.Empty ? null : "",
                Comments = "".Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                FunctionArgumentsAsString = "p_feed_id integer, p_new_item_hashes bigint[]" switch
                {
                    "" => null,
                    _ => "p_feed_id integer, p_new_item_hashes bigint[]",
                },
                FunctionReturnType = new SimpleType
                {
                    ClrTypeName = "long[]",
                    ClrType = typeof(long[]),
                    ClrNonNullableTypeName = "long[]",
                    ClrNonNullableType = typeof(long[]),
                    ClrNullableTypeName = "long[]",
                    ClrNullableType = typeof(long[]),
                    DbDataType = "_int8",
                    IsNullable = bool.Parse("True"),
                    IsClrValueType = bool.Parse("True"),
                    IsClrNullableType = bool.Parse("True"),
                    IsClrReferenceType = bool.Parse("True"),
                    Linq2DbDataTypeName = "DataType.Undefined",
                    Linq2DbDataType = DataType.Undefined,
                    NpgsqlDbTypeName = "NpgsqlDbType.Bigint | NpgsqlDbType.Array",
                    NpgsqlDbType = NpgsqlDbType.Bigint | NpgsqlDbType.Array,
                },
                FunctionArguments = new Dictionary<string, SimpleType>
                {
                    {
                        "p_feed_id", new SimpleType
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
                    {
                        "p_new_item_hashes", new SimpleType
                        {
                            ClrTypeName = "long[]",
                            ClrType = typeof(long[]),
                            ClrNonNullableTypeName = "long[]",
                            ClrNonNullableType = typeof(long[]),
                            ClrNullableTypeName = "long[]",
                            ClrNullableType = typeof(long[]),
                            DbDataType = "bigint[]",
                            IsNullable = bool.Parse("True"),
                            IsClrValueType = bool.Parse("True"),
                            IsClrNullableType = bool.Parse("True"),
                            IsClrReferenceType = bool.Parse("True"),
                            Linq2DbDataTypeName = "DataType.Undefined",
                            Linq2DbDataType = DataType.Undefined,
                            NpgsqlDbTypeName = "NpgsqlDbType.Bigint | NpgsqlDbType.Array",
                            NpgsqlDbType = NpgsqlDbType.Bigint | NpgsqlDbType.Array,
                        }
                    },
                },
            });
        }
    }
}
