using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Breakout.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI playerCountText;
        [SerializeField] PersistentPlayerRuntimeCollection playerList;
        [SerializeField] GameNetPortal portal;

        [SerializeField] Canvas mainMenuUI;
        [SerializeField] Button readyButton;

        void Awake()
        {
            playerList.ItemAdded += UpdateNumberOfPlayers;
            playerList.ItemRemoved += UpdateNumberOfPlayers;
            playerList.ItemAdded += UpdateReadyStatus;
            playerList.ItemRemoved += UpdateReadyStatus;
        }

        void OnDestroy()
        {
            playerList.ItemAdded -= UpdateNumberOfPlayers;
            playerList.ItemRemoved -= UpdateNumberOfPlayers;
            playerList.ItemAdded -= UpdateReadyStatus;
            playerList.ItemRemoved -= UpdateReadyStatus;
        }

        void UpdateNumberOfPlayers(PersistentPlayer player)
        {
            playerCountText.SetText($"Players: {playerList.Items.Count}/{GameManager.maxPlayers}");
        }

        void UpdateReadyStatus(PersistentPlayer player)
        {
            readyButton.interactable = (playerList.Items.Count == GameManager.maxPlayers);
        }

        public void OnReadyClicked()
        {
            portal.NetManager.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public void OnLeaveClicked()
        {
            portal.RequestDisconnect();
            mainMenuUI.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}