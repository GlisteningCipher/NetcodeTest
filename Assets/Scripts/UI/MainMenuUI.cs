using UnityEngine;
using UnityEngine.Assertions;

using Breakout.Client;

namespace Breakout.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] Canvas lobbyUI;

        GameNetPortal portal;

        void Awake()
        {
            portal = GameNetPortal.Instance;
            Assert.IsNotNull(portal, "No GameNetPortal found. Did you start the game from the startup scene?");
        }

        // Start is called before the first frame update
        public void OnHostClicked()
        {
            portal.StartHost("127.0.0.1", 7777);
            lobbyUI.gameObject.SetActive(true);
            gameObject.SetActive(false);

        }

        // Update is called once per frame
        public void OnJoinClicked()
        {
            ClientGameNetPortal.StartClient(portal, "127.0.0.1", 7777);
            lobbyUI.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }

}
