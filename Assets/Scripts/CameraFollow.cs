using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public static CameraFollow Instance;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    Transform player;

    public float cameraDistance = 5;
    public float smoothing = 4f;
    public Vector3 offset = new Vector3(-1, 0.8f, 0);
    // Start is called before the first frame update
    public void Initialize()
    {
        player = PlayerLocator.Instance.gameObject.transform;
        targetPosition = player.position;
        gameObject.transform.position = targetPosition + offset * cameraDistance;
        gameObject.transform.LookAt(player.position);
    }

    // Update is called once per frame
    Vector3 targetPosition;
    void Update()
    {
        targetPosition = player.position;
        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, targetPosition + offset * cameraDistance, smoothing * Time.deltaTime);
    }
}
