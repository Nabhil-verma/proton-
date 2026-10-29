/*
 * Copyright (c) 2026 Proton AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Common.Core.Dns;
using ProtonVPN.Common.Core.Networking;
using ProtonVPN.Common.Legacy;
using ProtonVPN.Common.Legacy.Vpn;
using ProtonVPN.Configurations.Contracts;
using ProtonVPN.Configurations.Contracts.Entities;
using ProtonVPN.Crypto.Contracts;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.OperatingSystems.Network.Contracts;
using ProtonVPN.Vpn.Common;
using ProtonVPN.Vpn.Connection;
using ProtonVPN.Vpn.Management;
using ProtonVPN.Vpn.NetworkAdapters;
using ProtonVPN.Vpn.OpenVpn;
using ProtonVPN.Vpn.Wintun;

namespace ProtonVPN.Vpn.Tests.Connection;

[TestClass]
public class OpenVpnConnectionTest
{
    [TestMethod]
    public async Task ConnectAsync_ShouldReturnUnknown_WhenConnectActionThrows()
    {
        // Arrange
        ILogger logger = Substitute.For<ILogger>();

        IStaticConfiguration config = Substitute.For<IStaticConfiguration>();
        IOpenVpnConfigurations openVpnConfig = Substitute.For<IOpenVpnConfigurations>();
        openVpnConfig.ConfigPath.Returns(Path.Combine(Path.GetTempPath(), $"protonvpn-{Guid.NewGuid():N}.ovpn"));
        config.OpenVpn.Returns(openVpnConfig);

        INetworkInterface networkInterface = Substitute.For<INetworkInterface>();
        networkInterface.Index.Returns(0u);
        INetworkInterfaceProvider networkInterfaceProvider = Substitute.For<INetworkInterfaceProvider>();
        networkInterfaceProvider
            .GetByVpnProtocol(Arg.Any<VpnProtocol>(), Arg.Any<OpenVpnAdapter?>())
            .Returns(networkInterface);

        IOpenVpnProcess process = Substitute.For<IOpenVpnProcess>();
        process.Start(Arg.Any<OpenVpnProcessParams>()).Returns(Task.FromResult(true));

        IRandomStringGenerator randomStringGenerator = Substitute.For<IRandomStringGenerator>();
        randomStringGenerator.Generate(Arg.Any<int>()).Returns("0123456789abcdef");

        IManagementClient managementClient = Substitute.For<IManagementClient>();
        managementClient
            .ConnectAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Management connection failed.")));

        OpenVpnConnection connection = new(
            logger,
            config,
            networkInterfaceProvider,
            process,
            randomStringGenerator,
            managementClient,
            Substitute.For<IWintunAdapter>(),
            Substitute.For<ITapAdapter>(),
            Substitute.For<INetworkUtilities>(),
            Substitute.For<IWintunRegistryFixer>());

        // Act
        VpnError error = await connection.ConnectAsync(
            CreateEndpoint(),
            CreateCredentials(),
            CreateConfig(),
            CancellationToken.None);

        // Assert
        error.Should().Be(VpnError.Unknown);
    }

    [TestMethod]
    public async Task ConnectAsync_ShouldReturnUnknown_WhenProcessDoesNotStartInTime()
    {
        // Arrange
        ILogger logger = Substitute.For<ILogger>();

        IStaticConfiguration config = Substitute.For<IStaticConfiguration>();
        IOpenVpnConfigurations openVpnConfig = Substitute.For<IOpenVpnConfigurations>();
        openVpnConfig.ConfigPath.Returns(Path.Combine(Path.GetTempPath(), $"protonvpn-{Guid.NewGuid():N}.ovpn"));
        config.OpenVpn.Returns(openVpnConfig);

        INetworkInterface networkInterface = Substitute.For<INetworkInterface>();
        networkInterface.Index.Returns(0u);
        INetworkInterfaceProvider networkInterfaceProvider = Substitute.For<INetworkInterfaceProvider>();
        networkInterfaceProvider
            .GetByVpnProtocol(Arg.Any<VpnProtocol>(), Arg.Any<OpenVpnAdapter?>())
            .Returns(networkInterface);

        IOpenVpnProcess process = Substitute.For<IOpenVpnProcess>();
        process.Start(Arg.Any<OpenVpnProcessParams>()).Returns(new TaskCompletionSource<bool>().Task);

        IRandomStringGenerator randomStringGenerator = Substitute.For<IRandomStringGenerator>();
        randomStringGenerator.Generate(Arg.Any<int>()).Returns("0123456789abcdef");

        IManagementClient managementClient = Substitute.For<IManagementClient>();

        OpenVpnConnection connection = new(
            logger,
            config,
            networkInterfaceProvider,
            process,
            randomStringGenerator,
            managementClient,
            Substitute.For<IWintunAdapter>(),
            Substitute.For<ITapAdapter>(),
            Substitute.For<INetworkUtilities>(),
            Substitute.For<IWintunRegistryFixer>())
        {
            ProcessStartTimeout = TimeSpan.FromMilliseconds(100),
        };

        // Act
        VpnError error = await connection.ConnectAsync(
            CreateEndpoint(),
            CreateCredentials(),
            CreateConfig(),
            CancellationToken.None);

        // Assert
        error.Should().Be(VpnError.Unknown);
        process.Received(1).Stop();
    }

    private static VpnConfig CreateConfig()
    {
        return new VpnConfig(new VpnConfigParameters
        {
            Ports = new Dictionary<VpnProtocol, IReadOnlyCollection<int>>
            {
                [VpnProtocol.OpenVpnUdp] = new List<int> { 1194 },
            },
            CustomDns = new List<string>(),
            SplitTunnelMode = SplitTunnelMode.Disabled,
            SplitTunnelIPs = new List<string>(),
            OpenVpnAdapter = OpenVpnAdapter.Tun,
            VpnProtocol = VpnProtocol.OpenVpnUdp,
            PreferredProtocols = new List<VpnProtocol> { VpnProtocol.OpenVpnUdp },
            NetShieldMode = 2,
            SplitTcp = true,
            ModerateNat = false,
            PortForwarding = true,
            IsIpv6Enabled = false,
            WireGuardConnectionTimeout = TimeSpan.FromSeconds(10),
            DnsBlockMode = DnsBlockMode.Disabled,
            ShouldDisableWeakHostSetting = true,
            IsWireGuardServerRouteEnabled = true,
        });
    }

    private static VpnCredentials CreateCredentials()
    {
        SecretKey secretKey = new(new byte[32], KeyAlgorithm.X25519);
        PublicKey publicKey = new(new byte[32], KeyAlgorithm.X25519);
        return new VpnCredentials(new AsymmetricKeyPair(secretKey, publicKey), "user", "password");
    }

    private static VpnEndpoint CreateEndpoint()
    {
        VpnHost host = new(
            "nl-1.protonvpn.net",
            "1.2.3.4",
            "NL#1",
            new PublicKey(new byte[32], KeyAlgorithm.X25519),
            "signature",
            false,
            null!);

        return new VpnEndpoint(host, VpnProtocol.OpenVpnUdp, 1194);
    }
}
