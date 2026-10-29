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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Common.Core.Networking;
using ProtonVPN.Common.Legacy.Vpn;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.Vpn.Gateways;
using ProtonVPN.Vpn.Management;

namespace ProtonVPN.Vpn.Tests.Management;

[TestClass]
public class ManagementClientTest
{
    [TestMethod]
    public void Bandwidth_ShouldReturnZero_WhenMessageHasNoPayload()
    {
        // Arrange
        ReceivedManagementMessage message = new(">BYTECOUNT");

        // Act
        NetworkTraffic traffic = message.Bandwidth();

        // Assert
        traffic.Should().Be(NetworkTraffic.Zero);
    }

    [TestMethod]
    public async Task StartVpnConnectionAsync_ShouldNotThrow_WhenByteCountMessageIsMalformed()
    {
        // Arrange
        ManagementClient client = CreateClient(">BYTECOUNT", "");

        // Act
        Func<Task> act = () => client.StartVpnConnectionAsync(default, null!, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        client.NetworkTraffic.Should().Be(NetworkTraffic.Zero);
    }

    [TestMethod]
    public async Task StartVpnConnectionAsync_ShouldNotThrow_WhenDnsAddressIsInvalid()
    {
        // Arrange
        IDnsServerCache dnsServerCache = Substitute.For<IDnsServerCache>();
        ManagementClient client = CreateClient(
            dnsServerCache,
            "PUSH: Received control message: 'dhcp-option DNS 010.0.0.1'",
            "");

        // Act
        Func<Task> act = () => client.StartVpnConnectionAsync(default, null!, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task StartVpnConnectionAsync_ShouldSaveValidDnsAddress()
    {
        // Arrange
        IDnsServerCache dnsServerCache = Substitute.For<IDnsServerCache>();
        List<IPAddress> savedDnsServers = new();
        dnsServerCache
            .When(x => x.Save(Arg.Any<IEnumerable<IPAddress>>()))
            .Do(call => savedDnsServers = call.Arg<IEnumerable<IPAddress>>().ToList());

        ManagementClient client = CreateClient(
            dnsServerCache,
            "PUSH: Received control message: 'dhcp-option DNS 1.1.1.1'",
            "");

        // Act
        await client.StartVpnConnectionAsync(default, null!, CancellationToken.None);

        // Assert
        savedDnsServers.Should().ContainSingle();
        savedDnsServers[0].Should().Be(IPAddress.Parse("1.1.1.1"));
    }

    private static ManagementClient CreateClient(params string[] messages)
    {
        return CreateClient(Substitute.For<IDnsServerCache>(), messages);
    }

    private static ManagementClient CreateClient(IDnsServerCache dnsServerCache, params string[] messages)
    {
        IMessagingManagementChannel channel = Substitute.For<IMessagingManagementChannel>();
        channel.Messages.Returns(new ManagementMessages());

        int messageIndex = 0;
        channel.ReadMessageAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new ReceivedManagementMessage(
                messages[Math.Min(messageIndex++, messages.Length - 1)])));

        return new ManagementClient(
            Substitute.For<ILogger>(),
            Substitute.For<IGatewayCache>(),
            dnsServerCache,
            channel);
    }
}
