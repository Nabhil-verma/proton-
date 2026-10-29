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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Client.Settings.Contracts;
using ProtonVPN.Client.Settings.Migrations;
using ProtonVPN.Configurations.Contracts;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.Serialization.Contracts.Json;

namespace ProtonVPN.Client.Common.Tests.Migrations;

[TestClass]
public class GlobalSettingsMigratorTest
{
    [TestMethod]
    public void Migrate_ShouldMarkMigrationDone_WhenLegacySettingsFileDoesNotExist()
    {
        IGlobalSettings globalSettings = Substitute.For<IGlobalSettings>();
        IConfiguration configuration = Substitute.For<IConfiguration>();
        configuration.LegacyAppLocalData.Returns(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        GlobalSettingsMigrator migrator = new(
            globalSettings,
            Substitute.For<ILogger>(),
            configuration,
            Substitute.For<IJsonSerializer>());

        migrator.Migrate();

        globalSettings.Received(1).IsGlobalSettingsMigrationDone = true;
    }
}
