﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus.Amqp
{
    using System;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Sasl;
    using Microsoft.Azure.Amqp.Transport;
    using Microsoft.Azure.ServiceBus.Primitives;

    internal class AmqpConnectionHelper
    {
        const string CbsSaslMechanismName = "MSSBCBS";

        public static AmqpSettings CreateAmqpSettings(
            Version amqpVersion,
            bool useSslStreamSecurity,
            bool hasTokenProvider,
            string sslHostName = null,
            bool useWebSockets = false,
            bool sslStreamUpgrade = false,
            System.Net.NetworkCredential networkCredential = null,
            bool forceTokenProvider = true)
        {
            AmqpSettings settings = new AmqpSettings();
            if (useSslStreamSecurity && !useWebSockets && sslStreamUpgrade)
            {
                var tlsSettings = new TlsTransportSettings
                {
                    TargetHost = sslHostName
                };

                var tlsProvider = new TlsTransportProvider(tlsSettings);
                tlsProvider.Versions.Add(new AmqpVersion(amqpVersion));
                settings.TransportProviders.Add(tlsProvider);
            }

            if (hasTokenProvider || networkCredential != null)
            {
                SaslTransportProvider saslProvider = new SaslTransportProvider();
                saslProvider.Versions.Add(new AmqpVersion(amqpVersion));
                settings.TransportProviders.Add(saslProvider);

                if (forceTokenProvider)
                {
                    saslProvider.AddHandler(new SaslAnonymousHandler(CbsSaslMechanismName));
                }
                else if (networkCredential != null)
                {
                    var plainHandler = new SaslPlainHandler
                    {
                        AuthenticationIdentity = networkCredential.UserName,
                        Password = networkCredential.Password
                    };
                    saslProvider.AddHandler(plainHandler);
                }
                else
                {
                    // old client behavior: keep it for validation only
                    saslProvider.AddHandler(new SaslExternalHandler());
                }
            }

            AmqpTransportProvider amqpProvider = new AmqpTransportProvider();
            amqpProvider.Versions.Add(new AmqpVersion(amqpVersion));
            settings.TransportProviders.Add(amqpProvider);

            return settings;
        }

        public static TransportSettings CreateTcpTransportSettings(
            string networkHost,
            string hostName,
            int port,
            bool useSslStreamSecurity,
            bool sslStreamUpgrade = false,
            string sslHostName = null,
            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = null)
        {
            TcpTransportSettings tcpSettings = new TcpTransportSettings
            {
                Host = networkHost,
                Port = port < 0 ? AmqpConstants.DefaultSecurePort : port,
                ReceiveBufferSize = AmqpConstants.TransportBufferSize,
                SendBufferSize = AmqpConstants.TransportBufferSize
            };

            TransportSettings tpSettings = tcpSettings;
            if (useSslStreamSecurity && !sslStreamUpgrade)
            {
                TlsTransportSettings tlsSettings = new TlsTransportSettings(tcpSettings)
                {
                    TargetHost = sslHostName ?? hostName
                };
                tpSettings = tlsSettings;
            }

            return tpSettings;
        }

        public static AmqpConnectionSettings CreateAmqpConnectionSettings(uint maxFrameSize, string containerId, string hostName)
        {
            var connectionSettings = new AmqpConnectionSettings
            {
                MaxFrameSize = maxFrameSize,
                ContainerId = containerId,
                HostName = hostName
            };

            connectionSettings.AddProperty("product", ClientInfo.Product);
            connectionSettings.AddProperty("version", ClientInfo.Version);
            connectionSettings.AddProperty("framework", ClientInfo.Framework);
            connectionSettings.AddProperty("platform", ClientInfo.Platform);
            return connectionSettings;
        }
    }
}