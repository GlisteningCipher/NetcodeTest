
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using Breakout.Client;

namespace Breakout
{
    public class NetworkHUD : MonoBehaviour
    {
        [SerializeField] GameNetPortal gamePortal;

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!GameManager.GameStarted && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            // if (GUILayout.Button("Offline")) GameManager.Instance.StartGame(false);
            if (GUILayout.Button("Host")) gamePortal.StartHost("127.0.0.1", 7777);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Client")) ClientGameNetPortal.StartClient(gamePortal, "127.0.0.1", 7777);
            // UNetTransport transport = (UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            // transport.ConnectAddress = GUILayout.TextField(transport.ConnectAddress);
            GUILayout.EndHorizontal();
        }

        void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ? "Host"
                : NetworkManager.Singleton.IsServer ? "Server"
                : NetworkManager.Singleton.IsClient ? "Client"
                : "Offline";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
            if (GUILayout.Button("Leave"))
            {
                gamePortal.RequestDisconnect();
            }
        }
    }
}