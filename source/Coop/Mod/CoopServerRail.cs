﻿using System;
using System.Collections.Generic;
using Common;
using Coop.Mod.DebugUtil;
using Coop.Mod.Persistence;
using Coop.Mod.Persistence.RPC;
using Coop.NetImpl.LiteNet;
using JetBrains.Annotations;
using Network.Infrastructure;
using RailgunNet.Connection.Server;
using RailgunNet.Factory;
using Sync.Store;

namespace Coop.Mod
{
    public class CoopServerRail : IUpdateable
    {
        [NotNull] private readonly RailServer m_Instance;

        private readonly Dictionary<ConnectionServer, RailNetPeerWrapper> m_RailConnections =
            new Dictionary<ConnectionServer, RailNetPeerWrapper>();

        [NotNull] private readonly Server m_Server;

        private readonly IReplay Replay;

        public CoopServerRail(
            [NotNull] Server server,
            [NotNull] SharedRemoteStore store,
            [NotNull] RailRegistry registry,
            IReplay replay,
            ICoop coop,
            TimeSpan eventTimeout)
        {
            m_Server = server;
            EventQueue = new EventBroadcastingQueue(store, eventTimeout);
            m_Instance = new RailServer(registry);
            EntityManager = new EntityManager(m_Instance, coop);
            Replay = replay;
        }

        [CanBeNull] public RailServerRoom Room => m_Instance.Room;

        [NotNull]
        public IReadOnlyCollection<RailServerPeer> ConnectedClients => m_Instance.ConnectedClients;

        public EventBroadcastingQueue EventQueue { get; }

        [NotNull] public EntityManager EntityManager { get; }

        public void Update(TimeSpan frameTime)
        {
            Replay.ReplayPlayback?.Invoke();
            m_Instance.Update();
            EventQueue.Update(frameTime);
        }

        ~CoopServerRail()
        {
            m_Server.Updateables.Remove(this);
        }

        public void ClientJoined(ConnectionServer connection)
        {
            RailNetPeerWrapper peer = connection.GameStatePersistence as RailNetPeerWrapper;
            m_RailConnections.Add(connection, peer);
            m_Instance.AddClient(peer, "Unknown");
        }

        public void Disconnected(ConnectionServer connection)
        {
            if (m_RailConnections.ContainsKey(connection))
            {
                m_Instance.RemoveClient(m_RailConnections[connection]);
            }
        }
    }
}
