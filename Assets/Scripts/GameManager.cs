using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Breakout
{
    public class GameManager : NetworkSingleton<GameManager>
    {
        [SerializeField] GameObject paddlePrefab;
        [SerializeField] GameObject ballPrefab;

        [SerializeField] List<Transform> spawns;

        [SerializeField] PersistentPlayerRuntimeCollection playerList;

        public static bool IsOnline => NetworkManager.Singleton && NetworkManager.Singleton.IsListening;

        private static NetworkVariable<bool> gameStartedNetVar = new NetworkVariable<bool>();
        public static bool GameStarted { get => gameStartedNetVar.Value; private set { gameStartedNetVar.Value = value; } }

        public int PlayerCount => playerList.Items.Count;
        public const int maxPlayers = 2;

        public void StartGame()
        {
            if (IsOnline && !IsServer) return;

            //spawn players
            for (int i = 0; i < maxPlayers; ++i)
            {
                var paddle = Instantiate(paddlePrefab, spawns[i].position, Quaternion.identity, transform);
                if (IsOnline)
                {
                    //assign paddle to client
                    var no = paddle.GetComponent<NetworkObject>();
                    no.SpawnWithOwnership(playerList.Items[i].OwnerClientId, true);
                }
            }

            //spawn ball
            var ball = Instantiate(ballPrefab, transform);
            if (IsOnline) ball.GetComponent<NetworkObject>().Spawn(true);

            GameStarted = true;
        }

        public void EndGame()
        {
            if (!GameStarted) return;
            if (IsOnline && !IsServer) return;
            foreach (Transform child in transform) Destroy(child.gameObject);
            GameStarted = false;
        }

    }
}