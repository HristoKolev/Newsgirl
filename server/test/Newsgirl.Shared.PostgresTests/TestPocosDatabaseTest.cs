namespace Newsgirl.Shared.PostgresTests
{
    using Postgres;
    using Testing;
    using Xunit;

    [Trait("Category", "orm_tests")]
    public abstract class TestPocosDatabaseTest : DatabaseTest<IDbService<TestDbPocos>, TestDbPocos>
    {
        protected TestPocosDatabaseTest() : base(
            "before-db-tests.sql",
            TestHelper.TestConfig.TestMasterConnectionString,
            x => new DbService<TestDbPocos>(x)
        ) { }
    }
}
