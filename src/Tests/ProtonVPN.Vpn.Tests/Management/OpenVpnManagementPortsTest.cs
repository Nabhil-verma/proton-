/*
 * Copyright (c) 2023 Proton AG
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtonVPN.Vpn.Management;

namespace ProtonVPN.Vpn.Tests.Management
{
    [TestClass]
    public class OpenVpnManagementPortsTest
    {
        [TestMethod]
        public void Port_ShouldBeInOperatingSystemEphemeralRange()
        {
            // Arrange
            OpenVpnManagementPorts ports = new OpenVpnManagementPorts();
            (int minimumPort, int maximumPort) = GetEphemeralPortRange();

            // Act
            int result = ports.Port();

            // Assert
            result.Should().BeInRange(minimumPort, maximumPort);
        }

        private static (int MinimumPort, int MaximumPort) GetEphemeralPortRange()
        {
            if (OperatingSystem.IsLinux())
            {
                string[] range = File.ReadAllText("/proc/sys/net/ipv4/ip_local_port_range")
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                if (range.Length >= 2 &&
                    int.TryParse(range[0], out int minimumPort) &&
                    int.TryParse(range[1], out int maximumPort))
                {
                    return (minimumPort, maximumPort);
                }
            }

            // Windows and macOS both use the IANA dynamic port range.
            return (49152, 65535);
        }
    }
}
