using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using TestContainers.Core.Builders;
using TestContainers.Core.Containers;
using Xunit;

namespace TestContainers.Tests.ContainerTests
{

    public class CouchbaseFixture : IAsyncLifetime
    {

        public string ConnectionString => Container.ConnectionString;
        public string UserName => Container.UserName;
        public string Password => Container.Password;

        private CouchbaseContainer Container { get; }

        public CouchbaseFixture()
        {
            Container = new DatabaseContainerBuilder<CouchbaseContainer>()
                .Begin()
                .WithImage(CouchbaseContainer.IMAGE)
                .WithExposedPorts(CouchbaseContainer.COUCHBASE_PORT)
                .WithEnv(("USERNAME", "admin"))
                .WithEnv(("PASSWORD", "passwort1234"))
                .Build();
        }

        public Task InitializeAsync() => Container.Start();

        public Task DisposeAsync() => Container.Stop();
    }

    public class CouchbaseTests : IClassFixture<CouchbaseFixture>

    {
        public CouchbaseTests(CouchbaseFixture fixture)
        {
            var clientConfig = new ClientConfiguration
            {
                Servers = new List<Uri>
                {
                    new Uri(fixture.ConnectionString)
                }
            };

            var authenticator = new PasswordAuthenticator(fixture.UserName, fixture.Password);
            ClusterHelper.Initialize(clientConfig, authenticator);
        }

        [Fact]
        public void SimpleTest()
        {
            Assert.True(ClusterHelper.Initialized);
        }
    }
}