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

using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Api.Contracts;
using ProtonVPN.Api.Contracts.Servers;
using ProtonVPN.Client.EventMessaging.Contracts;
using ProtonVPN.Client.Logic.Servers.Cache;
using ProtonVPN.Client.Logic.Servers.Contracts;
using ProtonVPN.Client.Logic.Servers.Contracts.Models;
using ProtonVPN.Client.Logic.Servers.Files;
using ProtonVPN.Client.Logic.Servers.Loads;
using ProtonVPN.Client.Settings.Contracts;
using ProtonVPN.Common.Core.Geographical;
using ProtonVPN.Configurations.Contracts;
using ProtonVPN.EntityMapping.Contracts;
using ProtonVPN.Logging.Contracts;

namespace ProtonVPN.Client.Logic.Servers.Tests.Cache;

[TestClass]
public class ServersCacheTest
{
    [TestMethod]
    public async Task UpdateAsync_ShouldRemainOutdated_WhenLoadsUpdateFails()
    {
        // Arrange
        IApiClient apiClient = Substitute.For<IApiClient>();
        apiClient
            .GetServersAsync(Arg.Any<DeviceLocation?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetServersResponse()));

        apiClient
            .GetServerLoadsAndStatusBinaryStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ApiResponseResult<byte[]>.Ok(new HttpResponseMessage(), [1, 2, 3])));

        IEntityMapper entityMapper = Substitute.For<IEntityMapper>();
        entityMapper.Map<LogicalServerResponse, Server>(Arg.Any<List<LogicalServerResponse>>())
            .Returns(new List<Server>());

        IServerLoadsCalculator loadsCalculator = Substitute.For<IServerLoadsCalculator>();
        loadsCalculator.UpdateServerLoads(Arg.Any<List<Server>>(), Arg.Any<byte[]>(), Arg.Any<DeviceLocation?>())
            .Returns(false);

        IConfiguration configuration = Substitute.For<IConfiguration>();
        configuration.ServerUpdateInterval.Returns(TimeSpan.FromMinutes(5));

        ISettings settings = Substitute.For<ISettings>();
        settings.LastLogicalsStatusId.Returns("old-status");
        settings.LogicalsLastModifiedDate.Returns(DateTimeOffset.UnixEpoch);

        ServersCache cache = new(
            apiClient,
            entityMapper,
            Substitute.For<IServersFileReaderWriter>(),
            Substitute.For<IEventMessageSender>(),
            configuration,
            settings,
            Substitute.For<ILogger>(),
            Substitute.For<IFavoriteServersStorage>(),
            loadsCalculator);

        // Act
        await cache.UpdateAsync(CancellationToken.None);

        // Assert
        Assert.IsTrue(cache.IsOutdated());
        Assert.AreEqual("old-status", settings.LastLogicalsStatusId);
        Assert.AreEqual(DateTimeOffset.UnixEpoch, settings.LogicalsLastModifiedDate);
    }

    private static ApiResponseResult<ServersResponse> GetServersResponse()
    {
        HttpResponseMessage responseMessage = new(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty),
        };
        responseMessage.Content.Headers.LastModified = DateTimeOffset.UtcNow;

        ServersResponse response = new()
        {
            Servers = new List<LogicalServerResponse>(),
            StatusId = "new-status",
        };

        return ApiResponseResult<ServersResponse>.Ok(responseMessage, response);
    }
}
