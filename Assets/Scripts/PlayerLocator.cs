using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocator : MonoBehaviour
{

    public static PlayerLocator Instance;
    public Transform playerTransForm;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        playerTransForm = gameObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Landscape.Instance.SendPlayerPositionForUpdates(this.gameObject.transform.position);
    }
}
