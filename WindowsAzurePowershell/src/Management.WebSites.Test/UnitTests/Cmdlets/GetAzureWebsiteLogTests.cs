﻿// ----------------------------------------------------------------------------------
//
// Copyright 2011 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Management.Websites.Test.UnitTests.Cmdlets
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Management.Services;
    using Management.Test.Stubs;
    using Management.Test.Tests.Utilities;
    using Model;
    using Utilities;
    using VisualStudio.TestTools.UnitTesting;
    using Websites.Cmdlets;
    using Websites.Services.DeploymentEntities;
    using Websites.Services.WebEntities;

    [TestClass]
    public class GetAzureWebsiteLogTests
    {
        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.AzureAppDir = Path.Combine(Directory.GetCurrentDirectory(), "Windows Azure Powershell");
            Extensions.CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
        }

        [TestMethod]
        public void GetAzureWebsiteLogTest()
        {
            // Setup
            SimpleWebsitesManagement channel = new SimpleWebsitesManagement();

            channel.GetWebSpacesThunk = ar => new WebSpaces(new List<WebSpace> { new WebSpace { Name = "webspace1" }, new WebSpace { Name = "webspace2" } });
            channel.GetSitesThunk = ar =>
            {
                if (ar.Values["webspaceName"].Equals("webspace1"))
                {
                    return new Sites(new List<Site> { new Site { Name = "website1", WebSpace = "webspace1", SiteProperties = new SiteProperties
                        {
                            Properties = new List<NameValuePair>
                            {
                                new NameValuePair { Name = "repositoryuri", Value = "http" },
                                new NameValuePair { Name = "PublishingUsername", Value = "user1" },
                                new NameValuePair { Name = "PublishingPassword", Value = "password1" }
                            }
                        }} 
                    });
                }

                return new Sites(new List<Site> { new Site { Name = "website2", WebSpace = "webspace2" } });
            };

            SimpleDeploymentServiceManagement deploymentChannel = new SimpleDeploymentServiceManagement();
            deploymentChannel.GetDeploymentsThunk = ar => new Deployments { new Deployment { Id = "commit1" }, new Deployment { Id = "commit2" } };
            deploymentChannel.GetDeploymentLogsThunk = ar =>
            {
                if (ar.Values["commitId"].Equals("commit1"))
                {
                    return new Logs { new Log { Id = "log1" }, new Log { Id = "log2" } };
                }

                return new Logs();
            };

            // Test
            GetAzureWebsiteLogCommand getAzureWebsiteLogCommand = new GetAzureWebsiteLogCommand(channel, deploymentChannel)
            {
                Name = "website1", 
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new SubscriptionData { SubscriptionId = "fake" }
            };

            getAzureWebsiteLogCommand.ExecuteCommand();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteLogCommand.CommandRuntime).WrittenObjects.Count);
            var logs = (IEnumerable<Log>)((MockCommandRuntime)getAzureWebsiteLogCommand.CommandRuntime).WrittenObjects.FirstOrDefault();
            Assert.IsNotNull(logs);
            Assert.AreEqual(2, logs.Count());
        }
    }
}
