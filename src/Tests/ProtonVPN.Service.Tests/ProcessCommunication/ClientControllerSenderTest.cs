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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Common.Core.Networking;
using ProtonVPN.EntityMapping.Contracts;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.ProcessCommunication.Contracts.Entities.Settings;
using ProtonVPN.Service.KillSwitch;
using ProtonVPN.Service.ProcessCommunication;
using ProtonVPN.Service.StateMachine;
using ProtonVPN.Vpn.Connection;
using ProtonVPN.Vpn.LocalAgent;
using ProtonVPN.Vpn.PortMapping;

namespace ProtonVPN.Service.Tests.ProcessCommunication;

[TestClass]
public class ClientControllerSenderTest
{
    [TestMethod]
    public async Task OnServiceSettingsChanged_ShouldNotThrow_WhenStoppingPortMappingFails()
    {
        // Arrange
        IPortMappingProtocolClient portMappingClient = Substitute.For<IPortMappingProtocolClient>();
        portMappingClient.StopAsync().Returns(Task.FromException(new InvalidOperationException("NAT-PMP stop failed.")));

        IVpnConnectionStateMachine stateMachine = Substitute.For<IVpnConnectionStateMachine>();
        Func<VpnState, Task>? onVpnStateChanged = null;
        stateMachine
            .When(x => x.SubscribeToStateChanged(Arg.Any<Func<VpnState, Task>>()))
            .Do(call => onVpnStateChanged = call.Arg<Func<VpnState, Task>>());

        ClientControllerSender sender = new(
            Substitute.For<IKillSwitch>(),
            Substitute.For<ILogger>(),
            Substitute.For<IEntityMapper>(),
            Substitute.For<ILocalAgent>(),
            Substitute.For<ILocalAgentEventReceiver>(),
            stateMachine,
            portMappingClient);

        Assert.IsNotNull(onVpnStateChanged);
        await onVpnStateChanged(new VpnState(VpnStatus.Connected, VpnProtocol.WireGuardUdp));

        // Act
        sender.OnServiceSettingsChanged(new MainSettingsIpcEntity { PortForwarding = false });
        await Task.Delay(50);

        // Assert
        portMappingClient.Received(1).StopAsync();
    }
}
