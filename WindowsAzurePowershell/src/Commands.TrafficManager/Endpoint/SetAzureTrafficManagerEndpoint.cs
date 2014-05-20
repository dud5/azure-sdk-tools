﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
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


using System;
using System.Linq;
using Microsoft.WindowsAzure.Commands.Common.Properties;
using Microsoft.WindowsAzure.Management.TrafficManager.Models;

namespace Microsoft.WindowsAzure.Commands.TrafficManager.Endpoint
{
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Commands.TrafficManager.Models;
    using Microsoft.WindowsAzure.Commands.TrafficManager.Utilities;

    [Cmdlet(VerbsCommon.Set, "AzureTrafficManagerEndpoint"), OutputType(typeof(IProfileWithDefinition))]
    public class SetAzureTrafficManagerEndpoint : TrafficManagerConfigurationBaseCmdlet
    {
        [Parameter(Mandatory = true,
           ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string DomainName { get; set; }

        // Commented out due to bug in hydra spec: https://github.com/Azure/hydra-specs-pr/pull/339
        // This feature hasn't been announced.
        [Parameter(Mandatory = false)]
        public string Location { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateSet("CloudService", "AzureWebsite", "Any", IgnoreCase = false)]
        public string Type { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateSet("Enabled", "Disabled", IgnoreCase = false)]
        public string Status { get; set; }

        // Commented out because endpoints using this fields will be inconsistent
        // with Portal. This feature hasn't been announced.
        [Parameter(Mandatory = false)]
        public int? Weight { get; set; }

        public override void ExecuteCmdlet()
        {
            ProfileWithDefinition profile = TrafficManagerProfile.GetInstance();

            TrafficManagerEndpoint endpoint = profile.Endpoints.FirstOrDefault(e => e.DomainName == DomainName);

            if (endpoint == null)
            {
                // Adding the endpoint because it doesn't exist
                if (String.IsNullOrEmpty(Type) ||
                    String.IsNullOrEmpty(Status) ||
                    String.IsNullOrEmpty(DomainName))
                {
                    throw new Exception(Resources.SetTrafficManagerEndpointNeedsParameters);
                }

                WriteVerboseWithTimestamp(Resources.SetInexistentTrafficManagerEndpointMessage, Name, DomainName);
                endpoint = new TrafficManagerEndpoint();
                endpoint.DomainName = DomainName;
                endpoint.Location = Location;
                endpoint.Type = (EndpointType)Enum.Parse(typeof(EndpointType), Type);
                endpoint.Weight = Weight.HasValue ? Weight.Value : 1;
                endpoint.Status = (EndpointStatus)Enum.Parse(typeof(EndpointStatus), Status);

                // Add it because the endpoint didn't exist
                profile.Endpoints.Add(endpoint);
            }

            endpoint.Location = Location ?? endpoint.Location;

            endpoint.Type = !String.IsNullOrEmpty(Type)
                ? (EndpointType)Enum.Parse(typeof(EndpointType), Type)
                : endpoint.Type;

            endpoint.Weight = Weight.HasValue ? Weight.Value : endpoint.Weight;

            endpoint.Status = !String.IsNullOrEmpty(Status)
                ? (EndpointStatus)Enum.Parse(typeof (EndpointStatus), Status)
                : endpoint.Status;

            WriteObject(profile);
        }
    }
}
