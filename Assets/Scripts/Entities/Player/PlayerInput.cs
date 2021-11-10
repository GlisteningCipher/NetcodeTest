using Unity.Netcode;
using UnityEngine;

namespace Breakout
{
    public class PlayerInput : NetworkBehaviour
    {
        Camera cam;
        PaddleController paddle;

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;
        }

        void Start()
        {
            cam = Camera.main;
            paddle = GetComponent<PaddleController>();
        }

        void Update()
        {
            var screenPos = Input.mousePosition;
            screenPos.z = -cam.transform.position.z;
            var worldPos = cam.ScreenToWorldPoint(screenPos);
            paddle.MoveY(worldPos.y);
        }
    }

}