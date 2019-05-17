using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using TestContainers.Core.Builders;
using TestContainers.Core.Containers;
using Xunit;

namespace TestContainers.Tests.ContainerTests
{

    public class CouchbaseFixture : IAsyncLifetime
    {

        public string ConnectionString => Container.ConnectionString;

        private CouchbaseContainer Container { get; }

        public CouchbaseFixture()
        {
            Container = new DatabaseContainerBuilder<CouchbaseContainer>()
                .Begin()
                .WithImage("couchbase")
                .WithExposedPorts(8091)
                .WithEnv(("USERNAME", "admin"))
                .WithEnv(("PASSWORD", "passwort1234"))
                .Build();
        }

        public Task InitializeAsync() => Container.Start();

        public Task DisposeAsync() => Container.Stop();
    }

    public class CouchbaseTests : IClassFixture<CouchbaseFixture>

    {
        private readonly Cluster _cluster;

        public CouchbaseTests(CouchbaseFixture fixture) => _cluster =
            new Cluster(new ClientConfiguration {
                Servers = new List<Uri>
                {
                    new Uri(fixture.ConnectionString)

                }
            });

        [Fact]
        public async Task SimpleTest()
        {
            string query = "SELECT 1";
            var createManager = this._cluster.CreateManager();
            await createManager.CreateBucketAsync("Test");
            var bucket = await this._cluster.OpenBucketAsync("Test");
            await bucket.QueryAsync<dynamic>(query);

            Assert.True(true);

            this._cluster.CloseBucket(bucket);

        }
    }
}