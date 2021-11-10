using UnityEngine;

[ExecuteInEditMode]
public class MatchWidth : MonoBehaviour
{
    public float horizontalFOV = 90.0f;
    Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        float halfWidth = Mathf.Tan(0.5f * horizontalFOV * Mathf.Deg2Rad);

        float halfHeight = halfWidth * Screen.height / Screen.width;

        float verticalFoV = 2.0f * Mathf.Atan(halfHeight) * Mathf.Rad2Deg;

        _camera.fieldOfView = verticalFoV;
    }
}
