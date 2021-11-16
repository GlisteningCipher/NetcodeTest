using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;

using Breakout.Server;
using Breakout.Client;

namespace Breakout
{

    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string clientGUID;
        public int clientScene = -1;
        public string playerName;
    }

    /// <summary>
    /// The GameNetPortal is the general purpose entry-point for game network messages between the client and server. It is available
    /// as soon as the initial network connection has completed, and persists across all scenes. Its purpose is to move non-GameObject-specific
    /// methods between server and client. Generally these have to do with connection, and match end conditions.
    /// </summary>
    ///
    /// <remarks>
    /// Why is there a C2S_ConnectFinished event here? How is that different from the "ApprovalCheck" logic that Netcode
    /// for GameObjects (Netcode) optionally runs when establishing a new client connection?
    /// Netcode's ApprovalCheck logic doesn't offer a way to return rich data. We need to know certain things directly upon logging in, such as
    /// whether the game-layer even wants us to join (we could fail because the server is full, or some other non network related reason), and also
    /// what BossRoomState to transition to. We do this with a Custom Named Message, which fires on the server immediately after the approval check delegate
    /// has run.
    ///
    /// Why do we need to send a client GUID? What is it? Don't we already have a clientID?
    /// ClientIDs are assigned on login. If you connect to a server, then your connection drops, and you reconnect, you get a new ClientID. This
    /// makes it awkward to get back your old character, which the server is going to hold onto for a fixed timeout. To properly reconnect and recover
    /// your character, you need a persistent identifier for your own client install. We solve that by generating a random GUID and storing it
    /// in player prefs, so it persists across sessions of the game.
    /// </remarks>
    // todo this should be refactored to 2 classes and should be renamed connection manager or something more clear like that.
    public class GameNetPortal : Singleton<GameNetPortal>
    {
        [SerializeField]
        private NetworkManager networkManager;
        public NetworkManager NetManager => networkManager;

        private ClientGameNetPortal clientPortal;
        private ServerGameNetPortal serverPortal;

        /// <summary> The name of the player chosen at game start </summary>
        public string PlayerName;

        protected override void Awake()
        {
            base.Awake();
            clientPortal = GetComponent<ClientGameNetPortal>();
            serverPortal = GetComponent<ServerGameNetPortal>();
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
            NetManager.OnServerStarted += OnNetworkReady;
            NetManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;
        }

        protected override void OnDestroy()
        {
            if (NetManager != null)
            {
                NetManager.OnServerStarted -= OnNetworkReady;
                NetManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
                if (NetManager.SceneManager != null) NetManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            base.OnDestroy();
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;
            serverPortal.OnClientSceneChanged(sceneEvent.ClientId, SceneManager.GetSceneByName(sceneEvent.SceneName).buildIndex);
        }

        private void ClientNetworkReadyWrapper(ulong clientId)
        {
            if (clientId == NetManager.LocalClientId)
            {
                OnNetworkReady();
                NetManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        /// <summary>
        /// This method runs when NetworkManager has started up (following a succesful connect on the client, or directly after StartHost is invoked
        /// on the host). It is named to match NetworkBehaviour.OnNetworkSpawn, and serves the same role, even though GameNetPortal itself isn't a NetworkBehaviour.
        /// </summary>
        private void OnNetworkReady()
        {
            if (NetManager.IsHost)
            {
                clientPortal.OnConnectFinished(ConnectStatus.Success);
            }
            clientPortal.OnNetworkReady();
            serverPortal.OnNetworkReady();
        }


        /// <summary>
        /// Initializes host mode on this client. Call this and then other clients should connect to us!
        /// </summary>
        /// <param name="ipaddress">The IP address to connect to (currently IPV4 only).</param>
        /// <param name="port">The port to connect to. </param>
        public void StartHost(string ipaddress, int port)
        {
            UNetTransport transport = (UNetTransport)NetManager.NetworkConfig.NetworkTransport;
            transport.ConnectAddress = ipaddress;
            transport.ConnectPort = port;

            NetManager.StartHost();
        }

        /// <summary>
        /// This will disconnect (on the client) or shutdown the server (on the host).
        /// It's a local signal (not from the network), indicating that the user has requested a disconnect.
        /// </summary>
        public void RequestDisconnect()
        {
            clientPortal.OnUserDisconnectRequest();
            serverPortal.OnUserDisconnectRequest();
            NetManager.Shutdown();
        }
    }

}