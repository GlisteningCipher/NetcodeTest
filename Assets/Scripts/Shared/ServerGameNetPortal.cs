using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

using Breakout.Client;

namespace Breakout.Server
{
    public struct PlayerData
    {
        public ulong clientId;
        public string playerName;
        public PlayerData(ulong id, string name) { clientId = id; playerName = name; }
    }

    public class ServerGameNetPortal : MonoBehaviour
    {
        private GameNetPortal portal;

        /// <summary> Maps a given client guid to the data for a given client player. </summary>
        private Dictionary<string, PlayerData> clientData;

        /// <summary> Map to allow us to cheaply map from guid to player data. </summary>
        private Dictionary<ulong, string> clientIdToGuid;

        /// <remarks> This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.</remarks>
        private const int maxConnectPayload = 1024;

        /// <summary> Keeps a list of what clients are in what scenes. </summary>
        private Dictionary<ulong, int> clientSceneMap = new Dictionary<ulong, int>();

        /// <summary> The active server scene index. </summary>
        public int ServerScene { get { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex; } }

        void Start()
        {
            portal = GetComponent<GameNetPortal>();
            portal.NetManager.ConnectionApprovalCallback += ApprovalCheck;
            portal.NetManager.OnServerStarted += ServerStartedHandler;
            clientData = new Dictionary<string, PlayerData>();
            clientIdToGuid = new Dictionary<ulong, string>();
        }

        void OnDestroy()
        {
            if (portal != null && portal.NetManager != null)
            {
                portal.NetManager.ConnectionApprovalCallback -= ApprovalCheck;
                portal.NetManager.OnServerStarted -= ServerStartedHandler;
            }
        }

        public void OnNetworkReady()
        {
            if (!portal.NetManager.IsServer)
            {
                enabled = false;
            }
            else
            {
                portal.NetManager.OnClientDisconnectCallback += OnClientDisconnect;

                //The "BossRoom" server always advances to CharSelect immediately on start. Different games
                //may do this differently.
                // NetworkManager.Singleton.SceneManager.LoadScene("CharSelect", LoadSceneMode.Single);

                if (portal.NetManager.IsHost)
                {
                    clientSceneMap[portal.NetManager.LocalClientId] = ServerScene;
                }
            }
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            clientSceneMap.Remove(clientId);
            if (clientIdToGuid.TryGetValue(clientId, out var guid))
            {
                clientIdToGuid.Remove(clientId);
                if (clientData[guid].clientId == clientId)
                {
                    //be careful to only remove the ClientData if it is associated with THIS clientId; in a case where a new connection
                    //for the same GUID kicks the old connection, this could get complicated. In a game that fully supported the reconnect flow,
                    //we would NOT remove ClientData here, but instead time it out after a certain period, since the whole point of it is
                    //to remember client information on a per-guid basis after the connection has been lost.
                    clientData.Remove(guid);
                }
            }

            if (clientId == portal.NetManager.LocalClientId)
            {
                //the ServerGameNetPortal may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        public void OnClientSceneChanged(ulong clientId, int sceneIndex)
        {
            clientSceneMap[clientId] = sceneIndex;
        }

        /// <summary>
        /// Handles the flow when a user has requested a disconnect via UI (which can be invoked on the Host, and thus must be
        /// handled in server code).
        /// </summary>
        public void OnUserDisconnectRequest()
        {
            Clear();
        }

        private void Clear()
        {
            //resets all our runtime state.
            clientData.Clear();
            clientIdToGuid.Clear();
            clientSceneMap.Clear();
        }

        public bool AreAllClientsInServerScene()
        {
            foreach (var kvp in clientSceneMap)
            {
                if (kvp.Value != ServerScene) { return false; }
            }

            return true;
        }

        /// <param name="clientId"> Guid of the client whose data is requested </param>
        /// <returns> Player data struct matching the given ID </returns>
        public PlayerData? GetPlayerData(ulong clientId)
        {
            if (clientIdToGuid.TryGetValue(clientId, out string clientGuid))
            {
                if (clientData.TryGetValue(clientGuid, out PlayerData data))
                {
                    return data;
                }
                else
                {
                    Debug.Log("No PlayerData of matching guid found");
                }
            }
            else
            {
                Debug.Log("No client guid found mapped to the given client ID");
            }
            return null;
        }

        /// <summary>
        /// Convenience method to get player name from player data
        /// Returns name in data or default name using playerNum
        /// </summary>
        public string GetPlayerName(ulong clientId, int playerNum)
        {
            var playerData = GetPlayerData(clientId);
            return (playerData != null) ? playerData.Value.playerName : ("Player" + playerNum);
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalCallback" exposed by Netcode.NetworkManager, and is run every time a client connects to us.
        /// See ClientGameNetPortal.StartClient for the complementary logic that runs when the client starts its connection.
        /// </summary>
        /// <remarks>
        /// Since our game doesn't have to interact with some third party authentication service to validate the identity of the new connection, our ApprovalCheck
        /// method is simple, and runs synchronously, invoking "callback" to signal approval at the end of the method. Netcode currently doesn't support the ability
        /// to send back more than a "true/false", which means we have to work a little harder to provide a useful error return to the client. To do that, we invoke a
        /// custom message in the same channel that Netcode uses for its connection callback. Since the delivery is NetworkDelivery.ReliableSequenced, we can be
        /// confident that our login result message will execute before any disconnect message.
        /// </remarks>
        /// <param name="connectionData">binary data passed into StartClient. In our case this is the client's GUID, which is a unique identifier for their install of the game that persists across app restarts. </param>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="connectionApprovedCallback">The delegate we must invoke to signal that the connection was approved or not. </param>
        private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            if (connectionData.Length > maxConnectPayload)
            {
                connectionApprovedCallback(false, null, false, null, null);
                return;
            }

            // Approval check happens for Host too, but obviously we want it to be approved
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                connectionApprovedCallback(true, null, true, null, null);
                return;
            }

            ConnectStatus gameReturnStatus = ConnectStatus.Success;

            // Test for over-capacity connection. This needs to be done asap, to make sure we refuse connections asap and don't spend useless time server side
            // on invalid users trying to connect
            // todo this is currently still spending too much time server side.
            if (clientData.Count >= GameManager.maxPlayers)
            {
                gameReturnStatus = ConnectStatus.ServerFull;
                //TODO-FIXME:Netcode Issue #796. We should be able to send a reason and disconnect without a coroutine delay.
                //TODO:Netcode: In the future we expect Netcode to allow us to return more information as part of
                //the approval callback, so that we can provide more context on a reject. In the meantime we must provide the extra information ourselves,
                //and then manually close down the connection.
                SendServerToClientConnectResult(clientId, gameReturnStatus);
                SendServerToClientSetDisconnectReason(clientId, gameReturnStatus);
                StartCoroutine(WaitToDisconnect(clientId));
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

            Debug.Log("Host ApprovalCheck: connecting client GUID: " + connectionPayload.clientGUID);

            //Test for Duplicate Login.
            if (clientData.ContainsKey(connectionPayload.clientGUID))
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"Client GUID {connectionPayload.clientGUID} already exists. Because this is a debug build, we will still accept the connection");
                    while (clientData.ContainsKey(connectionPayload.clientGUID)) { connectionPayload.clientGUID += "_Secondary"; }
                }
                else
                {
                    ulong oldClientId = clientData[connectionPayload.clientGUID].clientId;
                    // kicking old client to leave only current
                    SendServerToClientSetDisconnectReason(oldClientId, ConnectStatus.LoggedInAgain);
                    StartCoroutine(WaitToDisconnect(clientId));
                    return;
                }
            }

            SendServerToClientConnectResult(clientId, gameReturnStatus);

            //Populate our dictionaries with the playerData
            clientSceneMap[clientId] = connectionPayload.clientScene;
            clientIdToGuid[clientId] = connectionPayload.clientGUID;
            clientData[connectionPayload.clientGUID] = new PlayerData(clientId, connectionPayload.playerName);

            connectionApprovedCallback(true, null, true, null, null);

            // connection approval will create a player object for you
            AssignPlayerName(clientId, connectionPayload.playerName);
        }

        private IEnumerator WaitToDisconnect(ulong clientId)
        {
            yield return new WaitForSeconds(0.5f);
            portal.NetManager.DisconnectClient(clientId);
        }

        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public void SendServerToClientSetDisconnectReason(ulong clientId, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientSetDisconnectReason_CustomMessage), clientId, writer);
        }

        /// <summary>
        /// Responsible for the Server->Client custom message of the connection result.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> the status to pass to the client</param>
        public void SendServerToClientConnectResult(ulong clientId, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientConnectResult_CustomMessage), clientId, writer);
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        private void ServerStartedHandler()
        {
            clientData.Add("host_guid", new PlayerData(NetworkManager.Singleton.LocalClientId, portal.PlayerName));
            clientIdToGuid.Add(NetworkManager.Singleton.LocalClientId, "host_guid");

            AssignPlayerName(NetworkManager.Singleton.LocalClientId, portal.PlayerName);

            // server spawns game state
            // var gameState = Instantiate(m_GameState);

            // gameState.Spawn();
        }

        static void AssignPlayerName(ulong clientId, string playerName)
        {
            // get this client's player NetworkObject
            var networkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            // update client's name
            if (networkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
            {
                persistentPlayer.NetworkNameState.Name.Value = playerName;
            }
        }
    }
}
