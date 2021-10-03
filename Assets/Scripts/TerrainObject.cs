using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainObject : MonoBehaviour
{
    public static TerrainObject instance;

    void Awake()
    {
        if (TerrainObject.instance == null)
        {
            TerrainObject.instance = this;
        }
        else
        {
            Destroy(this);
        }
    }


    void Start()
    {
        Landscape.Instance.SendPlayerPositionForUpdates(this.gameObject.transform.position);
    }


    // Update is called once per frame
    void Update()
    {

    }
}
