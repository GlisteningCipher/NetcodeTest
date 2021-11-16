using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace Breakout.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI playerCountText;
        [SerializeField] PersistentPlayerRuntimeCollection playerList;

        [SerializeField] Canvas mainMenuUI;
        [SerializeField] Button readyButton;

        GameNetPortal portal;

        void Awake()
        {
            portal = GameNetPortal.Instance;
            Assert.IsNotNull(portal, "No GameNetPortal found. Did you start the game from the startup scene?");

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