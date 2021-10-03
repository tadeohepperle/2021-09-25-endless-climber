using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void ReceiveInput(Vector3Int vec);

public class InputHandler : MonoBehaviour
{

    public static InputHandler Instance;
    public static Vector3Int CurrentInput;
    private event ReceiveInput onReceiveInput;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        Vector3Int vec = GetInput();
        CurrentInput = vec;
        onReceiveInput?.Invoke(vec);
    }

    Vector3Int GetInput()
    {
        Vector3Int input = Vector3Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))
            input += Vector3Int.forward;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            input += Vector3Int.back;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            input += Vector3Int.right;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            input += Vector3Int.left;
        return input;
    }


    public void Subscribe(ReceiveInput callback)
    {
        onReceiveInput += callback;
    }

    public void UnSubscribe(ReceiveInput callback)
    {
        onReceiveInput -= callback;
    }
}
