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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Files;
using ProtonVPN.Files.Contracts;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.Serialization.Contracts;
using ProtonVPN.Serialization.Contracts.Json;

namespace ProtonVPN.Common.Tests.Files;

[TestClass]
public class FileReaderWriterTest
{
    private string _directory = null!;
    private string _filePath = null!;
    private IJsonSerializer _jsonSerializer = null!;
    private FileReaderWriter _writer = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"protonvpn-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "settings.json");

        _jsonSerializer = Substitute.For<IJsonSerializer>();
        _writer = new FileReaderWriter(
            Substitute.For<ILogger>(),
            Substitute.For<IProtobufSerializer>(),
            _jsonSerializer,
            Substitute.For<IPrettyJsonSerializer>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void Write_ShouldReplaceFile_WhenSerializationSucceeds()
    {
        // Arrange
        File.WriteAllText(_filePath, "old");
        _jsonSerializer
            .Serialize(Arg.Any<Dictionary<string, string?>>())
            .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes("new")));

        // Act
        FileOperationResult result = _writer.Write(
            new Dictionary<string, string?> { ["key"] = "value" },
            _filePath,
            Serializers.Json);

        // Assert
        Assert.AreEqual(FileOperationResult.Success, result);
        Assert.AreEqual("new", File.ReadAllText(_filePath));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void Write_ShouldPreserveExistingFile_WhenSerializationFails()
    {
        // Arrange
        File.WriteAllText(_filePath, "old");
        _jsonSerializer
            .Serialize(Arg.Any<Dictionary<string, string?>>())
            .Returns(_ => throw new IOException("Serialization failed."));

        // Act
        FileOperationResult result = _writer.Write(
            new Dictionary<string, string?> { ["key"] = "value" },
            _filePath,
            Serializers.Json);

        // Assert
        Assert.AreEqual(FileOperationResult.Failed, result);
        Assert.AreEqual("old", File.ReadAllText(_filePath));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }
}
