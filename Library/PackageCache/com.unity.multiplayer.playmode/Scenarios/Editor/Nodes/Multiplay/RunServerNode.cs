// TODO: This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    class RunServerNode : MultiplayNode, IInstanceRunNode
    {
        // The monitoring task will send requests to the server every interval to get the logs
        // and confirm the running state.
        private const int k_MonitorStepIntervalMS = 1000;
        private const int k_MaxLogsPerRequest = 100;

        [SerializeReference] public NodeInput<long> ServerId;
        [SerializeReference] public NodeInput<bool> StreamLogs;
        [SerializeReference] public NodeInput<Color> LogsColor;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionData;

        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionData;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        [SerializeField] private long m_LastLogsTime; // This is long instead of DateTime to make sure it survives domain reloads.
        [SerializeField] private int m_MonitorStepIntervalMS;
        [SerializeField] private ServerStatus m_ServerStatus;

        public bool IsRunning() => m_ServerStatus is ServerStatus.ONLINE or ServerStatus.ALLOCATED;

        public RunServerNode(string name, IScopedServiceProvider serviceProviderOverride = null, int monitorStepInteval = k_MonitorStepIntervalMS) : base(name, serviceProviderOverride)
        {
            ServerId = new(this);
            StreamLogs = new(this);
            LogsColor = new(this);
            ConnectionData = new(this);
            ConnectionDataOut = new(this);
            m_MonitorStepIntervalMS = monitorStepInteval;
            m_LastLogsTime = DateTime.Now.ToBinary();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var serverId = GetInput(ServerId);

            using var provider = await CreateAndValidateServiceProvider();
            var servers = provider.GetService<IServersApi>();
            await servers.InitAsync();

            var success = await servers.TriggerServerActionAsync(serverId, ServerAction.Start, cancellationToken);
            if (!success)
                throw new Exception($"Failed to start server {serverId}");

            SetOutput(ConnectionDataOut, GetInput(ConnectionData));
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(ServerId, nameof(ServerId));
            ValidateInputIsSet(LogsColor, nameof(LogsColor));
            ValidateInputIsSet(ConnectionData, nameof(ConnectionData));
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            try
            {
                var serverId = GetInput(ServerId);
                var streamLogs = GetInput(StreamLogs);

                using var provider = await CreateAndValidateServiceProvider();

                var serversApi = provider.GetService<IServersApi>();
                await serversApi.InitAsync();

                var logsApi = provider.GetService<ILogsApi>();
                await logsApi.InitAsync();

                var serverInfo = await serversApi.GetServerAsync(serverId, cancellationToken);
                var fleetId = serverInfo.FleetID;

                //This check is a consistency guard on domain reloads.
                // Depending on how fields are implemented, domain reloads could reset their values.
                Assert.IsTrue(m_LastLogsTime != 0, "Start time must be set before monitoring the server.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (streamLogs)
                        await StreamLogsStepAsync(logsApi, fleetId, serverId, cancellationToken);

                    await UpdateServerStatus(serversApi, serverId, cancellationToken);
                    if (!IsRunning())
                    {
                        Debug.LogWarning($"Server {serverId} is not online anymore. Review the logs for more details.");

                        var logs = await serversApi.GetServerActionLogsAsync(serverId, cancellationToken);
                        foreach (var log in logs)
                        {
                            Debug.Log($"Time: {log.Date} Server {serverId} log: {log.Message}, attachment: {log.Attachment}");
                        }
                        break;
                    }

                    Assert.IsTrue(m_MonitorStepIntervalMS > 0, "Monitor step interval must be greater than 0");

                    await Task.Delay(m_MonitorStepIntervalMS, cancellationToken);
                    await Task.Yield();
                }
            }
            catch
            {
                await StopServer();
                throw;
            }

            await StopServer();
        }

        private async Task UpdateServerStatus(IServersApi serversApi, long serverId, CancellationToken cancellationToken)
        {
            var serverInfo = await serversApi.GetServerAsync(serverId, cancellationToken);
            m_ServerStatus = serverInfo.Status;
        }

        private async Task StopServer()
        {
            using var provider = await CreateAndValidateServiceProvider();
            var servers = provider.GetService<IServersApi>();
            await servers.InitAsync();

            var serverId = GetInput(ServerId);
            var success = await servers.TriggerServerActionAsync(serverId, ServerAction.Stop);
            if (!success)
                Debug.LogError($"Failed to stop server {serverId}");
        }

        private List<string> m_LogsCache;
        private async Task StreamLogsStepAsync(ILogsApi logsApi, Guid fleetId, long serverId, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var paginationToken = string.Empty;

            m_LogsCache ??= new List<string>();
            m_LogsCache.Clear();

            // Collect the logs, potentially through multiple requests if there are many logs.
            do
            {
                var searchParams = new LogSearchParams(fleetId, serverId, "", k_MaxLogsPerRequest, DateTime.FromBinary(m_LastLogsTime), now, paginationToken);
                var result = await logsApi.SearchLogsAsync(searchParams, cancellationToken);

                if (result.Entries != null)
                {
                    foreach (var log in result.Entries)
                        m_LogsCache.Add(log.Message);
                }

                paginationToken = result.Count == 0 ? string.Empty : result.PaginationToken;
            } while (!string.IsNullOrEmpty(paginationToken) && !cancellationToken.IsCancellationRequested);

            // Stream the logs to the console
            var ip = GetInput(ConnectionData).IpAddress;
            var port = GetInput(ConnectionData).Port;

            var color = GetInput(LogsColor);
            var identifier = $"Remote({ip}:{port})";
            // Logs are received from newest to oldest, so we print them in reverse order.
            for (int i = m_LogsCache.Count - 1; i >= 0; i--)
                IInstanceRunNode.PrintReceivedLog(identifier, color, m_LogsCache[i]);

            m_LastLogsTime = now.ToBinary();
            m_LogsCache.Clear();
        }
    }
}
#endif
