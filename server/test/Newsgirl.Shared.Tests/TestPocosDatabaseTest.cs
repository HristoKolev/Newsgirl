namespace Newsgirl.Shared.Tests
{
    using Postgres;
    using Testing;

    public abstract class TestPocosDatabaseTest : DatabaseTest<IDbService<TestDbPocos>, TestDbPocos>
    {
        protected TestPocosDatabaseTest() : base(
            "before-db-tests.sql",
            TestHelper.TestConfig.DbTestConnectionString,
            x => new DbService<TestDbPocos>(x)
        ) { }
    }
}
