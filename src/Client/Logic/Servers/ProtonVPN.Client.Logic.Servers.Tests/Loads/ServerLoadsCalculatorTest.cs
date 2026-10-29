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

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtonVPN.Client.Logic.Servers.Contracts.Models;
using ProtonVPN.Client.Logic.Servers.Loads;

namespace ProtonVPN.Client.Logic.Servers.Tests.Loads;

[TestClass]
public class ServerLoadsCalculatorTest
{
    [TestMethod]
    public void MapServersToLogicals_ShouldReturnNull_WhenServerLacksLegacyLocationData()
    {
        // Arrange
        Server server = new()
        {
            Id = "server-id",
            Name = "NL#1",
            City = "Amsterdam",
            State = string.Empty,
            EntryCountry = "NL",
            ExitCountry = "NL",
            HostCountry = "NL",
            Domain = "nl-1.protonvpn.net",
            Servers = [],
            GatewayName = string.Empty,
            StatusReference = null!,
            EntryLocation = null!,
            ExitLocation = null!,
        };

        MethodInfo method = typeof(ServerLoadsCalculator).GetMethod(
            "MapServersToLogicals",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        // Act
        object? result = method.Invoke(null, [new List<Server> { server }]);

        // Assert
        Assert.IsNull(result);
    }
}
