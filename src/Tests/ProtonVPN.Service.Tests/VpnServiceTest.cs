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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Configurations.Contracts;
using ProtonVPN.IssueReporting.Contracts;
using ProtonVPN.Logging.Contracts;
using ProtonVPN.OperatingSystems.NRPT.Contracts;
using ProtonVPN.OperatingSystems.PowerEvents.Contracts;
using ProtonVPN.ProcessCommunication.Contracts;
using ProtonVPN.ProTun.Contracts;
using ProtonVPN.Service.Firewall;
using ProtonVPN.Service.StateMachine;
using ProtonVPN.Vpn.Common;

namespace ProtonVPN.Service.Tests;

[TestClass]
public class VpnServiceTest
{
    [TestMethod]
    public async Task OnStop_ShouldWaitForDisconnectToComplete()
    {
        // Arrange
        TaskCompletionSource disconnectCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IVpnConnectionStateMachine stateMachine = Substitute.For<IVpnConnectionStateMachine>();
        stateMachine.DisconnectAsync().Returns(disconnectCompletion.Task);

        ILogger logger = Substitute.For<ILogger>();
        IIpv6 ipv6 = Substitute.For<IIpv6>();
        IGrpcServer grpcServer = Substitute.For<IGrpcServer>();
        IIssueReporter issueReporter = Substitute.For<IIssueReporter>();

        VpnService service = (VpnService)RuntimeHelpers.GetUninitializedObject(typeof(VpnService));
        SetField(service, "_logger", logger);
        SetField(service, "_vpnConnectionStateMachine", stateMachine);
        SetField(service, "_ipv6", ipv6);
        SetField(service, "_grpcServer", grpcServer);
        SetField(service, "_issueReporter", issueReporter);
        SetField(service, "_cancellationTokenSource", new CancellationTokenSource());

        MethodInfo onStop = typeof(VpnService).GetMethod(
            "OnStop",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        Task onStopTask = Task.Run(() => onStop.Invoke(service, null));
        await WaitUntilAsync(() => stateMachine.ReceivedCalls().Any());

        // Assert
        Assert.IsFalse(onStopTask.IsCompleted, "OnStop returned before connection cleanup completed.");

        disconnectCompletion.SetResult();
        await onStopTask.WaitAsync(TimeSpan.FromSeconds(5));
        await stateMachine.Received(1).DisconnectAsync();
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, value);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using System.Threading.CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(20, cts.Token);
        }
    }
}
