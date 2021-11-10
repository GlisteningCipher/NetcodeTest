// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;

// namespace Breakout
// {
//     public struct PlayerData
//     {
//         public ulong id;
//         public PlayerData(ulong id) { this.id = id; }
//     }

//     public class RoomManager : NetworkSingleton<RoomManager>
//     {
//         Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();

//         void Start()
//         {
//             AddListeners();
//         }

//         public override void OnDestroy()
//         {
//             RemoveListeners();
//             base.OnDestroy();
//         }

//         void AddListeners()
//         {
//             if (NetworkManager.Singleton == null) return;
//             NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
//             NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
//             NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
//         }

//         void RemoveListeners()
//         {
//             if (NetworkManager.Singleton == null) return;
//             NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
//             NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
//             NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
//         }

//         private void HandleServerStarted()
//         {
//         }

//         private void HandleClientConnected(ulong obj)
//         {


//             if (!IsServer) return;
//             if (NetworkManager.Singleton.ConnectedClients.Count == 2)
//             {
//                 GameManager.Instance.StartGame(true);
//             }
//         }

//         private void HandleClientDisconnect(ulong obj)
//         {
//             if (!IsServer) return;
//             GameManager.Instance.EndGame();
//         }

//         private void ApprovalCheck(byte[] connData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
//         {
//             bool approveConnection = true;

//             var playerCount = NetworkManager.Singleton.ConnectedClients.Count;
//             if (playerCount == 2)
//                 approveConnection = false;

//             callback(false, null, approveConnection, null, null);
//         }

//         public void CreateRoom()
//         {
//             NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
//             NetworkManager.Singleton.StartHost();
//         }

//         public void JoinRoom()
//         {
//             NetworkManager.Singleton.StartClient();
//         }

//         public void LeaveRoom()
//         {
//             if (NetworkManager.Singleton.IsHost)
//                 NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;

//             NetworkManager.Singleton.Shutdown();
//         }
//     }

// }
