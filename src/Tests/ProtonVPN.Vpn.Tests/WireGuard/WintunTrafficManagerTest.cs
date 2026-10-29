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
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Common.Core.Networking;
using ProtonVPN.Configurations.Contracts;
using ProtonVPN.Configurations.Contracts.Entities;
using ProtonVPN.Vpn.WireGuard;

namespace ProtonVPN.Vpn.Tests.WireGuard;

[TestClass]
public class WintunTrafficManagerTest
{
    [TestMethod]
    public async Task WatchTrafficAsync_ShouldPublishCompleteResponse()
    {
        // Arrange
        string pipeName = $"protonvpn-test-{Guid.NewGuid():N}";
        using NamedPipeServerStream server = new(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        Task serverTask = Task.Run(async () =>
        {
            await server.WaitForConnectionAsync();
            using StreamReader reader = new(server);
            using StreamWriter writer = new(server) { AutoFlush = true };

            await reader.ReadLineAsync();
            await writer.WriteAsync("rx_bytes=100\n");
            await Task.Delay(100);
            await writer.WriteAsync("tx_bytes=50\n\n");
            await Task.Delay(1500);
        });

        IStaticConfiguration configuration = Substitute.For<IStaticConfiguration>();
        IWireGuardConfigurations wireGuardConfigurations = Substitute.For<IWireGuardConfigurations>();
        wireGuardConfigurations.PipeName.Returns(pipeName);
        configuration.WireGuard.Returns(wireGuardConfigurations);

        WintunTrafficManager manager = new(configuration);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

        // Act
        NetworkTraffic? firstSample = null;
        await foreach (NetworkTraffic sample in manager.WatchTrafficAsync(cts.Token))
        {
            firstSample = sample;
            break;
        }

        await serverTask;
        cts.Cancel();

        // Assert
        Assert.IsNotNull(firstSample);
        Assert.AreEqual(100UL, firstSample.Value.BytesDownloaded);
        Assert.AreEqual(50UL, firstSample.Value.BytesUploaded);
    }
}
