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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.IaaS
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Net;
    using System.Xml.Serialization;
    using Management.VirtualNetworks;
    using Model;
    using Properties;
    using Utilities.Common;

    [Cmdlet(VerbsCommon.Get, "AzureVNetConfig"), OutputType(typeof(VirtualNetworkConfigContext))]
    public class GetAzureVNetConfigCommand : ServiceManagementBaseCmdlet
    {
        [Parameter(HelpMessage = "The file path to save the network configuration to.")]
        [ValidateNotNullOrEmpty]
        public string ExportToFile
        {
            get;
            set;
        }

        public VirtualNetworkConfigContext GetVirtualNetworkConfigProcess()
        {
            this.ValidateParameters();

            VirtualNetworkConfigContext result = null;

            InvokeInOperationContext(() =>
            {
                try
                {
                    WriteVerboseWithTimestamp(string.Format(Resources.AzureVNetConfigBeginOperation, CommandRuntime.ToString()));

                    var netcfg = this.NetworkClient.Networks.GetConfiguration();
                    var operation = GetOperationNewSM(netcfg.RequestId);

                    if (netcfg != null)
                    {
                        // TODO: might want to change this to an XML object of some kind...
                        //var configReader = new StreamReader(netConfigStream);
                        //var xml = configReader.ReadToEnd();
                        NetworkConfiguration netobj = new NetworkConfiguration();
                        netobj.VirtualNetworkConfiguration.Dns.DnsServers = new DnsServer[netcfg.DnsServers.Count];
                        int i = 0;
                        foreach (var ds in netcfg.DnsServers)
                        {
                            netobj.VirtualNetworkConfiguration.Dns.DnsServers[i++] = new DnsServer
                            {
                                IPAddress = ds.IPAddress.ToString(),
                                name = ds.Name
                            };
                        }
                        
                        netobj.VirtualNetworkConfiguration.LocalNetworkSites = new LocalNetworkSite[netcfg.LocalNetworkSites.Count];
                        i = 0;
                        foreach (var lns in netcfg.LocalNetworkSites)
                        {
                            netobj.VirtualNetworkConfiguration.LocalNetworkSites[i] = new LocalNetworkSite
                            {
                                name = lns.Name,
                                VPNGatewayAddress = lns.VpnGatewayAddress.ToString()
                            };

                            netobj.VirtualNetworkConfiguration.LocalNetworkSites[i].AddressSpace = lns.AddressSpace.ToArray();
                            i++;
                        }

                        netobj.VirtualNetworkConfiguration.VirtualNetworkSites = new VirtualNetworkSite[netcfg.VirtualNetworkSites.Count];
                        i = 0;
                        foreach (var vns in netcfg.VirtualNetworkSites)
                        {
                            netobj.VirtualNetworkConfiguration.VirtualNetworkSites[i] = new VirtualNetworkSite
                            {
                                AddressSpace = vns.AddressSpace.ToArray(),
                                name = vns.Name,
                                AffinityGroup = vns.AffinityGroup,
                                Gateway = new Gateway
                                {
                                    profile = (GatewaySize)Enum.Parse(typeof(GatewaySize), vns.Gateway.Profile, true),
                                    VPNClientAddressPool = vns.Gateway.VpnClientAddressPool.ToArray(),
                                    ConnectionsToLocalNetwork = (from ln in vns.Gateway.ConnectionsToLocalNetwork
                                                                 select new LocalNetworkSiteRef
                                                                 {
                                                                     name = ln.Name,
                                                                     Connection = new Connection[1]
                                                                     {
                                                                         new Connection
                                                                         {
                                                                             type = (ConnectionType)Enum.Parse(typeof(ConnectionType), ln.ConnectionType.ToString(), true)
                                                                         }
                                                                     }
                                                                 }).ToArray(),
                                },
                                Subnets = new Subnet[vns.Subnets.Count],
                                InternetGatewayNetwork = new InternetGatewayNetwork
                                {
                                    name = vns.Label
                                },
                                DnsServersRef = (from dsr in vns.DnsServersReference
                                                 select new DnsServerRef
                                                 {
                                                     name = dsr.Name
                                                 }).ToArray()
                            };
                        }
                        
                        XmlSerializer ser = new XmlSerializer(typeof(NetworkConfiguration));
                        StringWriter sw = new StringWriter();
                        ser.Serialize(sw, netobj);
                        var xml = sw.ToString();

                        var networkConfig = new VirtualNetworkConfigContext
                        {
                            XMLConfiguration = xml,
                            OperationId = operation.Id,
                            OperationDescription = CommandRuntime.ToString(),
                            OperationStatus = operation.Status.ToString()
                        };

                        if (!string.IsNullOrEmpty(this.ExportToFile))
                        {
                            networkConfig.ExportToFile(this.ExportToFile);
                        }

                        result = networkConfig;
                    }

                    WriteVerboseWithTimestamp(string.Format(Resources.AzureVNetConfigCompletedOperation, CommandRuntime.ToString()));
                }
                catch (CloudException ex)
                {
                    if (ex.Response.StatusCode == HttpStatusCode.NotFound && !IsVerbose())
                    {
                        result = null;
                    }
                    else
                    {
                        this.WriteExceptionDetails(ex);
                    }
                }
            });

            return result;
        }

        protected override void OnProcessRecord()
        {
            var networkConfig = this.GetVirtualNetworkConfigProcess();

            if (networkConfig != null)
            {
                WriteObject(networkConfig, true);
            }
        }

        private void ValidateParameters()
        {
            if (!string.IsNullOrEmpty(this.ExportToFile) && !Directory.Exists(Path.GetDirectoryName(this.ExportToFile)))
            {
                throw new ArgumentException(Resources.NetworkConfigurationDirectoryDoesNotExist);
            }
        }
    }
}