using Unity.Netcode;
using UnityEngine;

namespace Breakout
{
    public class PaddleController : NetworkBehaviour
    {
        public void MoveY(float yPos)
        {
            if (GameManager.IsOnline) MoveYServerRpc(yPos);
            else transform.position = new Vector2(transform.position.x, yPos);
        }

        [ServerRpc]
        void MoveYServerRpc(float yPos, ServerRpcParams rpcParams = default)
        {
            transform.position = new Vector2(transform.position.x, yPos);
        }
    }
}