using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        Landscape.Instance.UpdateChunkGameObjectListToChunkPos(Vector3Int.zero);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
