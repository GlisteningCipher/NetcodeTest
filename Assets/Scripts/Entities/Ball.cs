using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Breakout
{
    public class Ball : NetworkBehaviour
    {
        [SerializeField] float speed = 5f;
        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.velocity = Vector2.right * speed;

            GetComponent<NetworkRigidbody2D>().enabled = GameManager.IsOnline;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) rb.simulated = false;
        }

        float HitFactor(Vector2 ballPos, Vector2 racketPos, float racketHeight)
        {
            return (ballPos.y - racketPos.y) / racketHeight;
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            if (col.transform.GetComponent<PaddleController>())
            {
                float y = HitFactor(transform.position,
                                    col.transform.position,
                                    col.collider.bounds.size.y);

                float x = col.relativeVelocity.x > 0 ? 1 : -1;

                Vector2 dir = new Vector2(x, y).normalized;

                rb.velocity = dir * speed;
            }
        }
    }

}