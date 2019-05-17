using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Management;
using Couchbase.N1QL;
using Polly;

namespace TestContainers.Core.Containers
{
    public class CouchbaseContainer : DatabaseContainer
    {
        public const string NAME = "couchbase";
        public const string IMAGE = "couchbase";
        public const int COUCHBASE_PORT = 8091;

        public override string DatabaseName => base.DatabaseName ?? _databaseName;

        public override string UserName => base.UserName ?? _username;

        public override string Password => base.Password ?? _password;

        private string _databaseName = "test";
        private string _username = "admin";
        private string _password = "passwort1234";

        public CouchbaseContainer() : base()
        {

        }

        int GetMappedPort(int portNumber) => portNumber;

        public override string ConnectionString => $"http://{GetDockerHostIpAddress()}";

        protected override string TestQueryString => "SELECT 1";

        protected override async Task WaitUntilContainerStarted()
        {
            await base.WaitUntilContainerStarted();

            var clientConfiguration = new ClientConfiguration
            {
                Servers = new List<Uri> {new Uri(ConnectionString)}
            };

            var cluster = new Cluster(clientConfiguration);
            var provisioner = new ClusterProvisioner(cluster, Password, UserName);
            var createManager = cluster.CreateManager();

            var result = await Policy
                .TimeoutAsync(TimeSpan.FromMinutes(2))
                .WrapAsync(Policy
                    .Handle<CouchbaseResponseException>()
                    .WaitAndRetryForeverAsync(
                        iteration => TimeSpan.FromSeconds(10)))
                .ExecuteAndCaptureAsync(async () =>
                {
                    await provisioner.ProvisionEntryPointAsync();
                    await createManager.CreateBucketAsync(DatabaseName);
                    var bucket = await cluster.OpenBucketAsync(DatabaseName);
                    var query = new QueryRequest().Statement(TestQueryString);
                    await bucket.QueryAsync<dynamic>(query);

                });

            if (result.Outcome == OutcomeType.Failure)
            {
                provisioner.Dispose();
                throw new Exception(result.FinalException.Message);
            }
        }
    }
}