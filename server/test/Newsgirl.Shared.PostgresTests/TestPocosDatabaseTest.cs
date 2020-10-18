namespace Newsgirl.Shared.PostgresTests
{
    using Postgres;
    using Testing;

    public abstract class TestPocosDatabaseTest : DatabaseTest<IDbService<TestDbPocos>, TestDbPocos>
    {
        protected TestPocosDatabaseTest() : base(
            "before-db-tests.sql",
            TestHelper.TestConfig.TestMasterConnectionString,
            x => new DbService<TestDbPocos>(x)
        ) { }
    }
}
