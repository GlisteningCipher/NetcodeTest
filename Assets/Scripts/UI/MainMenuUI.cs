using UnityEngine;

using Breakout.Client;

namespace Breakout.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] GameNetPortal portal;
        [SerializeField] Canvas lobbyUI;

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
