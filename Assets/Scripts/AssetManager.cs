using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetManager : MonoBehaviour
{

    public Material WorldMaterial;
    public static AssetManager instance;

    void Awake()
    {
        if (AssetManager.instance == null)
        {
            AssetManager.instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
