using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.Core.Buckets;
using Couchbase.Management;
using Couchbase.N1QL;
using Polly;
using TestContainers.Core.Builders;

namespace TestContainers.Core.Containers
{
    public class CouchbaseContainer : DatabaseContainer
    {
        public const string NAME = "couchbase";
        public const string IMAGE = "hueby/couchbase-no-setup";
        public const int COUCHBASE_PORT = 8091;

        public override string DatabaseName => base.DatabaseName ?? _databaseName;

        public override string UserName => base.UserName ?? _username;

        public override string Password => base.Password ?? _password;

        private string _databaseName = "test";
        private string _username = "Administrator";
        private string _password = "password";

        int GetMappedPort(int portNumber) => portNumber;

        public override string ConnectionString => $"http://{GetDockerHostIpAddress()}:{COUCHBASE_PORT}";

        protected override string TestQueryString => "SELECT 1";

        protected override async Task WaitUntilContainerStarted()
        {
            await base.WaitUntilContainerStarted();

            var clientConfiguration = new ClientConfiguration
            {
                Servers = new List<Uri> {new Uri(ConnectionString)}
            };

            var authenticator = new PasswordAuthenticator(UserName, Password);


            var result = await Policy
                .TimeoutAsync(TimeSpan.FromMinutes(2))
                .WrapAsync(Policy
                    .Handle<Exception>()
                    .WaitAndRetryForeverAsync(
                        iteration => TimeSpan.FromSeconds(10)))
                .ExecuteAndCaptureAsync(async () =>
                {
                    ClusterHelper.Initialize(clientConfiguration, authenticator);
                });
  
            if (result.Outcome == OutcomeType.Failure)
            {
                ClusterHelper.Close();
                throw new Exception(result.FinalException.Message);
            }

        }
    }
}