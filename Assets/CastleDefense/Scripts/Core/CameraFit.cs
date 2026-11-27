using UnityEngine;

/// <summary>Keeps the whole battlefield width visible on any screen aspect.</summary>
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public float worldWidth = 17.8f;
    public float minOrthoSize = 5f;
    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void Update()
    {
        float size = worldWidth / cam.aspect * 0.5f;
        cam.orthographicSize = Mathf.Max(minOrthoSize, size);
    }
}
