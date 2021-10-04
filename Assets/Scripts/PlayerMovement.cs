using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float MOVEMENT_STEP = 0.1f;
    public Material mat;
    Transform playerTransform;
    public Vector3 actualPos;
    public Vector3Int _blockpos;
    public Vector3Int BlockPos

    {
        get { return _blockpos; }
        set
        {
            _blockpos = value;
        }
    }
    public Edge edge = new Edge(EdgeMode.None);
    public Vector3Int lastRollUpInput;

    Dictionary<Vector3Int, BlockType> surrounding;

    public GameObject point1;
    public GameObject point2;
    public GameObject actualPosObj;
    public GameObject blockPosObj;


    public static PlayerMovement Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public void Initialize()
    {
        playerTransform = PlayerLocator.Instance.playerTransForm;
        Vector3Int playerStartingPosition = Landscape.Instance.GetPlayerStartingPos();
        actualPos = playerStartingPosition;
        BlockPos = playerStartingPosition;
        playerTransform.position = actualPos;
        playerTransform.rotation = Quaternion.Euler(0, 0, 0);
        surrounding = Landscape.Instance.GetSurrounding(BlockPos);
    }

    void Update()
    {

    }

    public Vector3Int input;
    public Vector3Int targetBlockPos;
    public Vector3 movementDirection;
    void FixedUpdate()
    {
        input = InputHandler.CurrentInput;

        targetBlockPos = CalculateTargetBlockPos(input);

        movementDirection = (((Vector3)targetBlockPos) - actualPos).normalized;

        actualPos += (movementDirection) * MOVEMENT_STEP;

        Vector3Int prevBlockPos = BlockPos;
        BlockPos = actualPos.roundToInt();
        if (BlockPos != prevBlockPos)
        {

            surrounding = Landscape.Instance.GetSurrounding(BlockPos);
        }
        if (actualPos.Approximately(BlockPos))
        {
            actualPos = BlockPos;
            lastRollUpInput = Vector3Int.zero;
        }

        Vector3Int actualDirFromBlockPos = (actualPos - (Vector3)BlockPos).normalized.roundToInt();
        RecalculateRollEdge(actualDirFromBlockPos);
        UpdatePlayerTransform(actualDirFromBlockPos);


        // graphical indicators

        // point1.transform.position = BlockPos + edge.point1;
        // point2.transform.position = BlockPos + edge.point2;
        // actualPosObj.transform.position = actualPos + (Vector3.one / 2);
        // blockPosObj.transform.position = BlockPos + (Vector3.one / 2);
    }


    bool SameDirectionOrZero(Vector3 vec1, Vector3 vec2)
    {
        float addedUpsqrMagnitude = (vec1 + vec2).sqrMagnitude;
        return Vector3.Cross(vec1, vec2).sqrMagnitude == 0 && addedUpsqrMagnitude >= vec1.sqrMagnitude && addedUpsqrMagnitude >= vec2.sqrMagnitude;
    }


    bool VerifyInput(Vector3Int input)
    {
        Vector3Int[] valid = new Vector3Int[5] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        bool containsInput = false;
        foreach (Vector3Int v in valid)
        {
            if (input == v)
            {
                containsInput = true;
            }
        }
        if (containsInput == false) throw new System.ArgumentException("input is not valid!");
        return containsInput;
    }

    Vector3Int CalculateTargetBlockPos(Vector3Int input)
    {
        Vector3 actualDir = actualPos - BlockPos;
        bool HorizontalDirFree()
        {
            return surrounding[BlockPos + input] == BlockType.None &&
            surrounding[BlockPos + Vector3Int.up] == BlockType.None &&
            surrounding[BlockPos + input + Vector3Int.up] == BlockType.None &&
            (surrounding[BlockPos + input + Vector3Int.down] == BlockType.Filled || surrounding[BlockPos + Vector3Int.down] == BlockType.Filled);
        }
        bool VecticalDirFree()
        {
            return surrounding[BlockPos + input] == BlockType.Filled &&
            surrounding[BlockPos + Vector3Int.up] == BlockType.None &&
            surrounding[BlockPos + input + Vector3Int.up] == BlockType.None &&
            surrounding[BlockPos - input] == BlockType.None &&
            surrounding[BlockPos - input + Vector3Int.up] == BlockType.None;
        }
        bool CanFall()
        {
            return SameDirectionOrZero(actualDir, Vector3.down) && surrounding[BlockPos + Vector3Int.down] == BlockType.None;
        }
        Vector3Int targetBlockPos = BlockPos;
        VerifyInput(input);
        bool horizontalDirFree = HorizontalDirFree();
        bool vecticalDirFree = VecticalDirFree();

        if (input == Vector3Int.zero)
        {
            // roll back to stay position, do nothing here
        }
        else
        {

            if (actualDir.Approximately(Vector3.zero)) actualDir = Vector3.zero;

            //DebugUtility.Watch("verticalClimb", vecticalDirFree.ToString());
            //DebugUtility.Watch("horizontal Move possible", horizontalDirFree.ToString());

            if (horizontalDirFree && SameDirectionOrZero(input, actualDir))
            {
                lastRollUpInput = Vector3Int.zero;
                targetBlockPos = BlockPos + input;
            }
            else if (vecticalDirFree && SameDirectionOrZero(Vector3.up, actualDir))
            {
                if (lastRollUpInput == input || lastRollUpInput == Vector3.zero) // check prevents players from switching roll directions while holding on to one edge. (while actualPos being above Blockpos)
                {
                    lastRollUpInput = input;
                    targetBlockPos = BlockPos + Vector3Int.up;
                }

            }

        }
        if (targetBlockPos == BlockPos && CanFall() && !(horizontalDirFree && input != Vector3.zero))
        {
            targetBlockPos = BlockPos + Vector3Int.down;
        }
        //DebugUtility.Watch("targetBlockPos", targetBlockPos.ToString());
        return targetBlockPos;
    }

    void RecalculateRollEdge(Vector3Int actualDir)
    {

        edge = new Edge(actualDir, lastRollUpInput);
    }

    void UpdatePlayerTransform(Vector3Int actualDirFromBlockPos) //Vector3 actualPos, Edge edge)
    {
        bool isBelowBlockPos = actualDirFromBlockPos == Vector3.down;
        Vector3 directionOfGroundToRollOn;
        switch (edge.edgeMode)
        {
            case (EdgeMode.RightUp):
                directionOfGroundToRollOn = Vector3.right;
                break;
            case (EdgeMode.LeftUp):
                directionOfGroundToRollOn = Vector3.left;
                break;
            case (EdgeMode.ForwardUp):
                directionOfGroundToRollOn = Vector3.forward;
                break;
            case (EdgeMode.BackUp):
                directionOfGroundToRollOn = Vector3.back;
                break;
            case (EdgeMode.Right):
                directionOfGroundToRollOn = isBelowBlockPos ? Vector3.right : Vector3.down;
                break;
            case (EdgeMode.Left):
                directionOfGroundToRollOn = isBelowBlockPos ? Vector3.left : Vector3.down;
                break;
            case (EdgeMode.Forward):
                directionOfGroundToRollOn = isBelowBlockPos ? Vector3.forward : Vector3.down;
                break;
            case (EdgeMode.Back):
                directionOfGroundToRollOn = isBelowBlockPos ? Vector3.back : Vector3.down;
                break;
            default:
                directionOfGroundToRollOn = Vector3.down;
                break;
        }


        Vector3 playerMiddlePoint = actualPos + new Vector3(0.5f, 0.5f, 0.5f);
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
        if (isBelowBlockPos) angle *= -1;
        playerTransform.rotation = Quaternion.Euler(edge.EdgeDirection * angle);
        // adjust y level of playerTransform.position
        playerTransform.position -= directionOfGroundToRollOn * overreachingEndLength;

    }
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

    public Edge(Vector3Int direction, Vector3Int lastRollUpInput)
    {
        EdgeMode em;
        Dictionary<(Vector3Int, Vector3Int), EdgeMode> d = new Dictionary<(Vector3Int, Vector3Int), EdgeMode>
        {
            [(Vector3Int.right, Vector3Int.zero)] = EdgeMode.Right,
            [(Vector3Int.left, Vector3Int.zero)] = EdgeMode.Left,
            [(Vector3Int.forward, Vector3Int.zero)] = EdgeMode.Forward,
            [(Vector3Int.back, Vector3Int.zero)] = EdgeMode.Back,
            [(Vector3Int.up, Vector3Int.right)] = EdgeMode.RightUp,
            [(Vector3Int.up, Vector3Int.left)] = EdgeMode.LeftUp,
            [(Vector3Int.up, Vector3Int.forward)] = EdgeMode.ForwardUp,
            [(Vector3Int.up, Vector3Int.back)] = EdgeMode.BackUp,
            [(Vector3Int.down, Vector3Int.right)] = EdgeMode.Right,
            [(Vector3Int.down, Vector3Int.left)] = EdgeMode.Left,
            [(Vector3Int.down, Vector3Int.forward)] = EdgeMode.Forward,
            [(Vector3Int.down, Vector3Int.back)] = EdgeMode.Back,

        };
        if (d.ContainsKey((direction, lastRollUpInput)))
        {
            em = d[(direction, lastRollUpInput)];
        }
        else
        {
            em = EdgeMode.None;
        }
        this.edgeMode = em;
        this.point1 = new Vector3Int();
        this.point2 = new Vector3Int();
        (Vector3Int p1, Vector3Int p2) = Points(em);
        this.point1 = p1;
        this.point2 = p2;
    }

    public Vector3 EdgeDirection
    {
        get { return point2 - point1; }
    }

    // public Edge() : this(EdgeMode.None) { }

    (Vector3Int, Vector3Int) Points(EdgeMode em)
    {
        Vector3Int point1, point2;
        switch (em)
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
        return (point1, point2);
    }


    public Edge(EdgeMode edgeMode)
    {
        this.edgeMode = edgeMode;
        this.point1 = new Vector3Int();
        this.point2 = new Vector3Int();
        (Vector3Int p1, Vector3Int p2) = Points(edgeMode);
        this.point1 = p1;
        this.point2 = p2;

    }
}
