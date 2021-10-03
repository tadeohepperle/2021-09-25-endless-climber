using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public Transform playerTransform;
    public Vector3 actualPos;
    public Vector3Int blockPos;
    public Edge edge = new Edge(EdgeMode.None);

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = PlayerLocator.Instance.playerTransForm;
        PlayerCubeTransformFromActualPosAndEdge();
    }

    // Update is called once per frame
    void Update()
    {


    }

    void PlayerCubeTransformFromActualPosAndEdge() //Vector3 actualPos, Edge edge)
    {

        Vector3 playerMiddlePoint = actualPos + new Vector3(0.5f, 0.5f, 0.5f);
        //Vector3 blockMiddlePoint = blockPos + new Vector3(0.5f, 0.5f, 0.5f);
        playerTransform.rotation = Quaternion.Euler(0, 0, 0);
        playerTransform.position = playerMiddlePoint;
        if (edge.IsNull) return;
        Vector3 playerCubeEdgeMiddlePoint = actualPos + edge.MiddlePoint;
        Vector3 terrainEdgeMiddlePoint = blockPos + edge.MiddlePoint;
        Vector3 playerMiddlePointFlat = new Vector3(playerMiddlePoint.x, 0, playerMiddlePoint.z);
        Vector3 terrainEdgeMiddlePointFlat = new Vector3(terrainEdgeMiddlePoint.x, 0, terrainEdgeMiddlePoint.z);
        Vector3 yDirectionOfTerrainEdge = terrainEdgeMiddlePoint.y > playerMiddlePoint.y ? Vector3.up : Vector3.down;

        // upperSide bekannt, hyp bekannt.
        // _______ <-- playerMiddlePoint 
        // |.    /
        // |    /
        // |   / <-- hyp = distanz von playerMiddlePoint Zu Edge
        // |__/___________ ground
        // |</-- overreachingEndLength
        // |/ <-- tipPoint

        float hyp = Vector3.Distance(playerMiddlePoint, playerCubeEdgeMiddlePoint);
        float upperSide = Vector3.Distance(playerMiddlePointFlat, terrainEdgeMiddlePointFlat);
        float leftSide = Mathf.Sqrt(hyp * hyp - upperSide * upperSide);
        float overreachingEndLength = leftSide - 0.5f;
        Vector3 tipPoint = terrainEdgeMiddlePoint + (yDirectionOfTerrainEdge * overreachingEndLength);
        Vector3 rechterWinkelPoint = terrainEdgeMiddlePoint - yDirectionOfTerrainEdge * 0.5f;
        // calculate Angle between (playerMiddlePoint --> playerCubeEdgeMiddlePoint) and (playerMiddlePoint --> tipPoint)
        float angle = Vector3.Angle(playerMiddlePoint - playerCubeEdgeMiddlePoint, playerMiddlePoint - tipPoint);
        // apply angle in right direction:
        playerTransform.rotation = Quaternion.Euler(edge.EdgeDirection * angle);
        // adjust y level of playerTransform.position
        playerTransform.position += -yDirectionOfTerrainEdge * overreachingEndLength;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerCubeEdgeMiddlePoint + Vector3.forward, playerCubeEdgeMiddlePoint - Vector3.forward);
        Gizmos.DrawLine(playerMiddlePoint, playerCubeEdgeMiddlePoint);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(terrainEdgeMiddlePoint + Vector3.forward, terrainEdgeMiddlePoint - Vector3.forward);
        Gizmos.DrawLine(playerMiddlePoint, terrainEdgeMiddlePoint);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerMiddlePoint, tipPoint);
        Gizmos.DrawLine(rechterWinkelPoint, tipPoint);
        Gizmos.DrawLine(rechterWinkelPoint, playerMiddlePoint);

    }

    void OnDrawGizmos()
    {
        edge = new Edge(EdgeMode.Right);
        PlayerCubeTransformFromActualPosAndEdge();
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
