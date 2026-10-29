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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtonVPN.Common.Core.Dns;
using ProtonVPN.Common.Core.Networking;
using ProtonVPN.Common.Legacy;
using ProtonVPN.Common.Legacy.Vpn;
using ProtonVPN.Service.StateMachine;

namespace ProtonVPN.Service.Tests.StateMachine;

[TestClass]
public class VpnConnectionStateMachineTest
{
    [TestMethod]
    public void CreateUpdatedVpnConfig_ShouldPreserveSettingsThatAreNotPartOfVpnFeatures()
    {
        // Arrange
        VpnConfig current = CreateConfig();
        VpnFeatures features = new()
        {
            SplitTcp = false,
            NetShieldMode = 1,
            PortForwarding = false,
            ModerateNat = true,
        };

        // Act
        VpnConfig updated = VpnConnectionStateMachine.CreateUpdatedVpnConfig(current, features);

        // Assert
        updated.ModerateNat.Should().BeTrue();
        updated.ShouldDisableWeakHostSetting.Should().BeTrue();
        updated.IsWireGuardServerRouteEnabled.Should().BeTrue();
        updated.IsIpv6Enabled.Should().BeFalse();
        updated.WireGuardConnectionTimeout.Should().Be(current.WireGuardConnectionTimeout);
        updated.DnsBlockMode.Should().Be(current.DnsBlockMode);
        updated.Ports.Should().BeSameAs(current.Ports);
        updated.CustomDns.Should().BeSameAs(current.CustomDns);
        updated.SplitTunnelMode.Should().Be(current.SplitTunnelMode);
        updated.SplitTunnelIPs.Should().BeSameAs(current.SplitTunnelIPs);
        updated.OpenVpnAdapter.Should().Be(current.OpenVpnAdapter);
        updated.VpnProtocol.Should().Be(current.VpnProtocol);
        updated.PreferredProtocols.Should().BeSameAs(current.PreferredProtocols);

        updated.SplitTcp.Should().BeFalse();
        updated.NetShieldMode.Should().Be(1);
        updated.PortForwarding.Should().BeFalse();
    }

    private static VpnConfig CreateConfig()
    {
        return new VpnConfig(new VpnConfigParameters
        {
            Ports = new Dictionary<VpnProtocol, IReadOnlyCollection<int>>
            {
                [VpnProtocol.WireGuardUdp] = new List<int> { 51820 },
            },
            CustomDns = new List<string> { "1.1.1.1" },
            SplitTunnelMode = SplitTunnelMode.Permit,
            SplitTunnelIPs = new List<string> { "10.0.0.0/8" },
            OpenVpnAdapter = OpenVpnAdapter.Tap,
            VpnProtocol = VpnProtocol.WireGuardUdp,
            PreferredProtocols = new List<VpnProtocol> { VpnProtocol.WireGuardUdp },
            NetShieldMode = 2,
            SplitTcp = true,
            ModerateNat = false,
            PortForwarding = true,
            IsIpv6Enabled = false,
            WireGuardConnectionTimeout = TimeSpan.FromSeconds(15),
            DnsBlockMode = DnsBlockMode.Disabled,
            ShouldDisableWeakHostSetting = true,
            IsWireGuardServerRouteEnabled = true,
        });
    }
}
