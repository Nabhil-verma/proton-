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

using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Api.Contracts;
using ProtonVPN.Api.Contracts.Servers;
using ProtonVPN.Client.EventMessaging.Contracts;
using ProtonVPN.Client.Logic.Servers.Cache;
using ProtonVPN.Client.Logic.Servers.Contracts;
using ProtonVPN.Configurations.Contracts;

namespace ProtonVPN.Client.Logic.Servers.Tests;

[TestClass]
public class ServerFinderTest
{
    [TestMethod]
    public async Task Search_ShouldUseCache_ForEquivalentNames()
    {
        // Arrange
        IConfiguration configuration = Substitute.For<IConfiguration>();
        configuration.ServerSearchDelay.Returns(TimeSpan.FromMilliseconds(20));

        int lookupCount = 0;
        IServersCache serversCache = Substitute.For<IServersCache>();
        serversCache
            .LookupAsync(Arg.Any<string>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref lookupCount);

                LookupServerResponse response = new()
                {
                    Code = 1000,
                    LogicalServer = new LogicalServerResponse { Id = "server-id", Name = "NL#123" },
                };

                return Task.FromResult<ApiResponseResult<LookupServerResponse>?>(
                    ApiResponseResult<LookupServerResponse>.Ok(new HttpResponseMessage(), response));
            });

        ServerFinder finder = new(
            configuration,
            serversCache,
            Substitute.For<IEventMessageSender>(),
            Substitute.For<IFavoriteServersStorage>());

        // Act
        finder.Search("nl123");
        await WaitUntilAsync(() => Volatile.Read(ref lookupCount) == 1);

        finder.Search("nl123");
        await Task.Delay(300);

        // Assert
        Assert.AreEqual(1, Volatile.Read(ref lookupCount));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(20, cts.Token);
        }
    }
}
