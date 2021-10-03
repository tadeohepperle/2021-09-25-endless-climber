using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float MOVEMENT_STEP = 0.05f;
    Transform playerTransform;
    public Vector3 actualPos;
    private Vector3Int _blockpos;
    public Vector3Int BlockPos

    {
        get { return _blockpos; }
        set
        {
            _blockpos = value;
        }
    }
    public Edge edge = new Edge(EdgeMode.None);
    Dictionary<Vector3Int, BlockType> surrounding;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        playerTransform = PlayerLocator.Instance.playerTransForm;
        actualPos = Vector3.zero;
        BlockPos = Vector3Int.zero;
    }

    void Update()
    {

    }


    void FixedUpdate()
    {
        Vector3Int input = InputHandler.CurrentInput;
        Vector3Int targetBlockPos = CalculateTargetBlockPos(input);

        Vector3 movementDirection = targetBlockPos - BlockPos;
        actualPos += movementDirection * MOVEMENT_STEP;
        Vector3Int prevBlockPos = BlockPos;
        BlockPos = actualPos.roundToInt();
        if (BlockPos != prevBlockPos)
        {
            surrounding = Landscape.Instance.GetSurrounding(_blockpos);
        }
        RecalculateRollEdge(movementDirection);
        UpdatePlayerTransform();

    }

    bool JumpToNewBlockPosNeeded()
    {
        Vector3 direction = actualPos - BlockPos;
        return (direction.sqrMagnitude > 0.25); // more than 0.5 away 

    }

    Vector3Int CalculateTargetBlockPos(Vector3Int input)
    {
        // TODO
        if (actualPos.x >= BlockPos.x) edge = new Edge(EdgeMode.Right);
        else edge = new Edge(EdgeMode.Left);

        return Vector3Int.right + BlockPos;
    }
    void RecalculateRollEdge(Vector3 movementDirection)
    {
        if (actualPos.x >= BlockPos.x) edge = new Edge(EdgeMode.Right);
        else edge = new Edge(EdgeMode.Left);
    }

    void UpdatePlayerTransform() //Vector3 actualPos, Edge edge)
    {

        Vector3 directionOfGroundToRollOn;

        switch (edge.edgeMode)
        {
            case (EdgeMode.ForwardUp):
                directionOfGroundToRollOn = Vector3.forward;
                break;
            case (EdgeMode.BackUp):
                directionOfGroundToRollOn = Vector3.back;
                break;
            case (EdgeMode.RightUp):
                directionOfGroundToRollOn = Vector3.right;
                break;
            case (EdgeMode.LeftUp):
                directionOfGroundToRollOn = Vector3.left;
                break;
            default:
                directionOfGroundToRollOn = Vector3.down;
                break;
        }


        Vector3 playerMiddlePoint = actualPos + new Vector3(0.5f, 0.5f, 0.5f);
        //Vector3 blockMiddlePoint = blockPos + new Vector3(0.5f, 0.5f, 0.5f);
        playerTransform.rotation = Quaternion.Euler(0, 0, 0);
        playerTransform.position = playerMiddlePoint;
        if (edge.IsNull) return;
        Vector3 playerCubeEdgeMiddlePoint = actualPos + edge.MiddlePoint;
        Vector3 terrainEdgeMiddlePoint = BlockPos + edge.MiddlePoint;

        // upperSide bekannt, hyp bekannt.
        // _______ <-- playerMiddlePoint 
        // |.    /
        // |    /
        // |   / <-- hyp = distanz von playerMiddlePoint Zu Edge
        // |__/___________ ground
        // |</-- overreachingEndLength
        // |/ <-- tipPoint


        Vector3 rechterWinkelPoint = terrainEdgeMiddlePoint - (directionOfGroundToRollOn * 0.5f);
        float hyp = Vector3.Distance(playerMiddlePoint, playerCubeEdgeMiddlePoint);
        float upperSide = Vector3.Distance(playerMiddlePoint, rechterWinkelPoint);
        float leftSide = Mathf.Sqrt(hyp * hyp - upperSide * upperSide);
        float overreachingEndLength = leftSide - 0.5f;
        Vector3 tipPoint = terrainEdgeMiddlePoint + (directionOfGroundToRollOn * overreachingEndLength);

        // calculate Angle between (playerMiddlePoint --> playerCubeEdgeMiddlePoint) and (playerMiddlePoint --> tipPoint)
        float angle = Vector3.Angle(playerMiddlePoint - playerCubeEdgeMiddlePoint, playerMiddlePoint - tipPoint);
        // apply angle in right direction:
        playerTransform.rotation = Quaternion.Euler(edge.EdgeDirection * angle);
        // adjust y level of playerTransform.position
        playerTransform.position = playerTransform.position - directionOfGroundToRollOn * overreachingEndLength;
        // Gizmos.color = Color.red;
        // Gizmos.DrawLine(playerCubeEdgeMiddlePoint + Vector3.forward, playerCubeEdgeMiddlePoint - Vector3.forward);
        // Gizmos.DrawLine(playerMiddlePoint, playerCubeEdgeMiddlePoint);
        // Gizmos.color = Color.green;
        // Gizmos.DrawLine(terrainEdgeMiddlePoint + Vector3.forward, terrainEdgeMiddlePoint - Vector3.forward);
        // Gizmos.DrawLine(playerMiddlePoint, terrainEdgeMiddlePoint);
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawLine(playerMiddlePoint, tipPoint);
        // Gizmos.DrawLine(rechterWinkelPoint, tipPoint);
        // Gizmos.DrawLine(rechterWinkelPoint, playerMiddlePoint);

    }

    // void OnDrawGizmos()
    // {
    //     // edge = new Edge(EdgeMode.RightUp);
    //     // PlayerCubeTransformFromActualPosAndEdge();
    // }
}

[System.Serializable]
public enum EdgeMode
{
    None,
    Forward,
    ForwardUp,
    Back,
    BackUp,
    Right,
    RightUp,
    Left,
    LeftUp

}

[System.Serializable]
public struct Edge
{
    public EdgeMode edgeMode;
    public Vector3Int point1;
    public Vector3Int point2;

    public bool IsNull
    {
        get { return this.edgeMode == EdgeMode.None; }
    }
    public Vector3 MiddlePoint
    {
        get { return new Vector3(point1.x + point2.x, point1.y + point2.y, point1.z + point2.z) / 2; }
    }

    public Vector3 EdgeDirection
    {
        get { return point2 - point1; }
    }

    // public Edge() : this(EdgeMode.None) { }


    public Edge(EdgeMode edgeMode)
    {
        this.edgeMode = edgeMode;
        switch (edgeMode)
        {

            case (EdgeMode.Forward):
                point1 = new Vector3Int(0, 0, 1);
                point2 = new Vector3Int(1, 0, 1);
                break;
            case (EdgeMode.ForwardUp):
                point1 = new Vector3Int(0, 1, 1);
                point2 = new Vector3Int(1, 1, 1);
                break;
            case (EdgeMode.Back):
                point1 = new Vector3Int(1, 0, 0);
                point2 = new Vector3Int(0, 0, 0);
                break;
            case (EdgeMode.BackUp):
                point1 = new Vector3Int(1, 1, 0);
                point2 = new Vector3Int(0, 1, 0);
                break;
            case (EdgeMode.Right):
                point1 = new Vector3Int(1, 0, 1);
                point2 = new Vector3Int(1, 0, 0);
                break;
            case (EdgeMode.RightUp):
                point1 = new Vector3Int(1, 1, 1);
                point2 = new Vector3Int(1, 1, 0);
                break;
            case (EdgeMode.Left):
                point1 = new Vector3Int(0, 0, 0);
                point2 = new Vector3Int(0, 0, 1);
                break;
            case (EdgeMode.LeftUp):
                point1 = new Vector3Int(0, 1, 0);
                point2 = new Vector3Int(0, 1, 1);
                break;
            default:
                point1 = Vector3Int.zero;
                point2 = Vector3Int.zero;
                break;
        }
    }
}
