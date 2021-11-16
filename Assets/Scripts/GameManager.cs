using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Breakout
{
    public class GameManager : NetworkSingleton<GameManager>
    {
        public int PlayerCount => playerList.Items.Count;
        public const int maxPlayers = 2;

        [SerializeField] GameObject paddlePrefab;
        [SerializeField] GameObject ballPrefab;

        [SerializeField] Transform[] spawns = new Transform[maxPlayers];

        [SerializeField] PersistentPlayerRuntimeCollection playerList;

        public static bool IsOnline => NetworkManager.Singleton && NetworkManager.Singleton.IsListening;

        private static NetworkVariable<bool> gameStartedNetVar = new NetworkVariable<bool>();
        public static bool GameStarted { get => gameStartedNetVar.Value; private set { gameStartedNetVar.Value = value; } }

        public void StartGame()
        {
            if (IsOnline && !IsServer) return;

            //spawn players
            for (int i = 0; i < maxPlayers; ++i)
            {
                var paddle = Instantiate(paddlePrefab, spawns[i].position, Quaternion.identity);
                if (IsOnline)
                {
                    //assign paddle to client
                    var no = paddle.GetComponent<NetworkObject>();
                    no.SpawnWithOwnership(playerList.Items[i].OwnerClientId, true);
                    no.TrySetParent(transform);
                }
            }

            //spawn ball
            var ball = Instantiate(ballPrefab);
            if (IsOnline)
            {
                var no = ball.GetComponent<NetworkObject>();
                no.Spawn(true);
                no.TrySetParent(transform);

            }

            GameStarted = true;
        }

        public void EndGame()
        {
            if (!GameStarted) return;
            if (IsOnline && !IsServer) return;
            foreach (Transform child in transform) Destroy(child.gameObject);
            GameStarted = false;
        }

        public void LeaveSession()
        {
            GameNetPortal.Instance.RequestDisconnect();
            SceneManager.LoadScene("MainMenu");
        }

    }
}