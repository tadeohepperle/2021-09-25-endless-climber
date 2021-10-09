
using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using System.Diagnostics;
using System.Threading.Tasks;

static class Vector3IntExtrensions
{
    const float EPSILON = 0.0001f;

    static int mod(int num, int m)
    {
        return (num % m + m) % m;
    }

    static int div(int num, int d)
    {
        return num >= 0 ? num / d : num / d - 1;
    }
    public static Vector3Int DivideBy(this Vector3Int vec, int i)
    {
        return new Vector3Int(div(vec.x, i), div(vec.y, i), div(vec.z, i));
    }
    public static Vector3Int Modolo(this Vector3Int vec, int i)
    {
        return new Vector3Int(mod(vec.x, i), mod(vec.y, i), mod(vec.z, i));
    }

    public static bool Approximately(this Vector3Int v1, Vector3 v2)
    {
        return Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y) + Mathf.Abs(v1.z - v2.z) < EPSILON;
    }
    public static bool Approximately(this Vector3 v1, Vector3Int v2)
    {
        return Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y) + Mathf.Abs(v1.z - v2.z) < EPSILON;
    }
    public static bool Approximately(this Vector3 v1, Vector3 v2)
    {
        return Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y) + Mathf.Abs(v1.z - v2.z) < EPSILON;
    }

    public static Vector3Int roundToInt(this Vector3 vec)
    {
        return new Vector3Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y), Mathf.RoundToInt(vec.z));
    }

    public static Vector3 ToVector3(this Vector3Int vec)
    {
        return new Vector3(vec.x, vec.y, vec.z);
    }


}


class Landscape
{
    private static Landscape _instance;
    public static Landscape Instance
    {
        get { if (_instance == null) _instance = new Landscape(); return _instance; }
    }

    public const int CHUNKSIZE = 10;
    public const int BLOCKSIZE = 1;
    public const float NOISEAMPLITUDE = 6f;
    public const float NOISESCALE = 0.2f;
    public const int CHUNKRADIUSAROUNDPLAYER = 2;
    public const int GETSURROUNDINGRADIUS = 2;
    Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();


    private Vector3Int lastUpdateChunkPos = Vector3Int.zero;


    public Dictionary<Vector3Int, BlockType> GetSurrounding(Vector3Int blockPos)
    {
        Dictionary<Vector3Int, BlockType> surrounding = new Dictionary<Vector3Int, BlockType>();
        for (int x = blockPos.x - GETSURROUNDINGRADIUS; x <= blockPos.x + GETSURROUNDINGRADIUS; x++)
        {
            for (int y = blockPos.y - GETSURROUNDINGRADIUS; y <= blockPos.y + GETSURROUNDINGRADIUS; y++)
            {
                for (int z = blockPos.z - GETSURROUNDINGRADIUS; z <= blockPos.z + GETSURROUNDINGRADIUS; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    Vector3Int chunkPos = pos.DivideBy(CHUNKSIZE);
                    Vector3Int blockPosInChunk = pos.Modolo(CHUNKSIZE);

                    Chunk c = GetChunk(chunkPos);
                    if (c != null) surrounding[pos] = c[blockPosInChunk];
                    else { surrounding[pos] = BlockType.None; UnityEngine.Debug.Log("ERRRR\n"); }

                }
            }
        }
        return surrounding;
    }

    public void SendPlayerPositionForUpdates(Vector3 playerPosition)
    {
        Vector3Int chunkPos = ChunkPosFromPlayerPosition(playerPosition);
        if (chunkPos != lastUpdateChunkPos)
        {
            lastUpdateChunkPos = new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z);
            UpdateChunkGameObjectListToChunkPos(chunkPos);
        }

    }

    public async void UpdateChunkGameObjectListToChunkPos(Vector3Int chunkPos)
    {
        List<ChunkData> newlyCreatedChunks = new List<ChunkData>();
        foreach (KeyValuePair<Vector3Int, Chunk> keyValuePair in _chunks)
        {
            keyValuePair.Value.markedForDeletion = true;
        }
        for (int x = chunkPos.x - CHUNKRADIUSAROUNDPLAYER; x <= chunkPos.x + CHUNKRADIUSAROUNDPLAYER; x++)
        {
            for (int y = chunkPos.y - CHUNKRADIUSAROUNDPLAYER; y <= chunkPos.y + CHUNKRADIUSAROUNDPLAYER; y++)
            {
                for (int z = chunkPos.z - CHUNKRADIUSAROUNDPLAYER; z <= chunkPos.z + CHUNKRADIUSAROUNDPLAYER; z++)
                {
                    Vector3Int vec3 = new Vector3Int(x, y, z);
                    Chunk chunkAtVec3 = GetChunk(vec3);
                    if (chunkAtVec3 != null)
                    {
                        chunkAtVec3.markedForDeletion = false;
                    }
                    else
                    {
                        AddChunk(vec3);
                        newlyCreatedChunks.Add(GetChunk(vec3).data);
                    }
                }
            }
        }
        // REMOVE UNNECESSARY CHUNKS:
        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, Chunk> keyValuePair in _chunks)
        {
            if (keyValuePair.Value.markedForDeletion)
            {
                toRemove.Add(keyValuePair.Key);
            }
        }
        foreach (Vector3Int vec in toRemove)
        {
            RemoveChunk(vec);
        }

        // GENERATE NEWLY ADDED CHUNKS:
        Stopwatch s = new Stopwatch();
        s.Start();

        // Wihtout multithreding: approx. 1,4ms per Chunk
        // for (int i = 0; i < newlyCreatedChunks.Count; i++)
        // {
        //     newlyCreatedChunks[i].GenerateBlocks();
        //     newlyCreatedChunks[i].GenerateMesh();
        //     var pos = newlyCreatedChunks[i].pos;
        //     Chunk c = GetChunk(pos);
        //     c.data = newlyCreatedChunks[i];
        //     c.CreateGameObject();
        // }

        // With multithreading: approx. 0,4ms per Chunk
        List<Task<ChunkData>> taskList = new List<Task<ChunkData>>();
        for (int i = 0; i < newlyCreatedChunks.Count; i++)
        {
            taskList.Add(ChunkGenerationTask(newlyCreatedChunks[i]));
        }
        var results = await Task.WhenAll(taskList);
        for (int i = 0; i < results.Length; i++)
        {
            var pos = results[i].pos;
            Chunk c = GetChunk(pos);
            c.data = results[i];
            c.CreateGameObject();
        }

        s.Stop();
        UnityEngine.Debug.Log("Time elampsed: " + s.ElapsedMilliseconds + "ms | Chunks handled: " + newlyCreatedChunks.Count + " | Time per Chunk: " + (float)s.ElapsedMilliseconds / newlyCreatedChunks.Count + "ms");
    }

    public Task<ChunkData> ChunkGenerationTask(ChunkData chunkData)
    {
        return Task.Run(() =>
        {
            chunkData.GenerateBlocks();
            chunkData.GenerateMesh();
            return chunkData;
        });
    }


    public Landscape()
    {
        this._chunks = new Dictionary<Vector3Int, Chunk>();

    }

    public Vector3Int ChunkPosFromPlayerPosition(Vector3 playerPosition)
    {
        Vector3 v = playerPosition / (CHUNKSIZE * BLOCKSIZE);
        return new Vector3Int((int)v.x, (int)v.y, (int)v.z);
    }

    public Vector3Int GetPlayerStartingPos()
    {
        return new Vector3Int(0, (int)GetNoise(new Vector3Int(0, 0, 0)), 0);
    }


    // public void Draw()
    // {
    //     // Material mat = new Material(Shader.Find("Standard"));
    //     foreach (Chunk c in _chunks)
    //     {
    //         // c.Iter((x, y, z) =>
    //         // {
    //         //     if (c.blocks[x, y, z] > 0)
    //         //     {
    //         //         GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //         //         g.transform.position = new Vector3((c.pos.x * CHUNKSIZE + x) * BLOCKSIZE, (c.pos.y * CHUNKSIZE + y) * BLOCKSIZE, (c.pos.z * CHUNKSIZE + z) * BLOCKSIZE);

    //         //     }
    //         // });

    //         GameObject g = new GameObject();
    //         g.transform.position = c.GlobalPosition;
    //         MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();
    //         meshRenderer.sharedMaterial = AssetManager.instance.WorldMaterial;
    //         MeshFilter meshFilter = g.AddComponent<MeshFilter>();
    //         meshFilter.mesh = c.ToMesh();
    //     }
    // }


    public Chunk this[int x, int y, int z]
    {
        get { return GetChunk(x, y, z); }

    }

    public Chunk this[Vector3Int vec]
    {
        get { return GetChunk(vec); }

    }



    public Chunk GetChunk(int x, int y, int z)
    {
        return GetChunk(new Vector3Int(x, y, z));
    }

    public Chunk GetChunk(Vector3Int vec)
    {
        if (_chunks.ContainsKey(vec))
        {
            return _chunks[vec];
        }
        return null;
    }

    bool RemoveChunk(int x, int y, int z)
    {
        return RemoveChunk(new Vector3Int(x, y, z));
    }

    bool RemoveChunk(Vector3Int vec)
    {
        Chunk c = GetChunk(vec);
        if (c != null)
        {
            c.DestroyGameObject();
            this._chunks.Remove(vec); return true;
        }
        else return false;
    }

    public bool AddChunk(int x, int y, int z)
    {
        return AddChunk(new Vector3Int(x, y, z));
    }

    public bool AddChunk(Vector3Int vec)
    {
        Chunk c = GetChunk(vec);
        if (c != null)
        {
            return false;
        }
        else
        {
            Chunk newChunk = new Chunk(vec);
            _chunks[vec] = newChunk;
            return true;
        }
    }


    // public Chunk GenerateChunk(Vector3Int pos)
    // {

    //     Chunk c = new Chunk(pos);
    //     c.Iter((int x, int y, int z) =>
    //     {
    //         Vector3Int globalPos = Landscape.GlobalPos(c.pos, new Vector3Int(x, y, z));
    //         float cutoff = (globalPos.x + globalPos.z) / 2;
    //         // add some PerlinNoise:
    //         float noise = GetNoise(globalPos);
    //         cutoff += noise;
    //         bool filled = globalPos.y < cutoff;
    //         if (filled) c.filledCount++;
    //         c.blocks[x, y, z] = filled ? BlockType.Filled : BlockType.None;
    //     });
    //     c.Activate(); // vielleicht auslagern in asynchrone methode
    //     c.landscape = this;
    //     return c;
    // }

    public BlockType GetBlockType(Vector3Int globalPos)
    {
        float height = (globalPos.x + globalPos.z) / 2;
        height += GetNoise(globalPos);
        if (globalPos.y > height) return BlockType.None;
        else return BlockType.Filled;
    }

    public float GetNoise(Vector3Int pos)
    {
        float x = pos.x * NOISESCALE;
        float z = pos.z * NOISESCALE;
        float wave1 = Mathf.PerlinNoise(x, z) * NOISEAMPLITUDE * 2 - NOISEAMPLITUDE;
        float wave2 = Mathf.PerlinNoise(x / 10, z / 10) * NOISEAMPLITUDE * 10 - NOISEAMPLITUDE * 5;
        return wave1 + wave2;
    }

    public static Vector3Int GlobalPos(Vector3Int chunkPos, Vector3Int localPos)
    {
        return chunkPos * CHUNKSIZE + localPos;
    }

}



enum BlockType : short
{
    OutOfBounds = -1,
    None = 0,
    Filled = 1,
}


class ChunkData
{
    public Vector3Int pos;
    public BlockType[,,] blocks;
    public int filledCount;
    public bool generated;
    public Vector3[] meshVertices;
    public Vector3[] meshNormals;
    public int[] meshTriangles;
    public Vector2[] meshUV;

    public ChunkData(Vector3Int pos)
    {
        this.pos = pos;
        this.blocks = new BlockType[Landscape.CHUNKSIZE, Landscape.CHUNKSIZE, Landscape.CHUNKSIZE];
        this.filledCount = 0;
        this.generated = false;
        this.meshVertices = new Vector3[0];
        this.meshNormals = new Vector3[0];
        this.meshTriangles = new int[0];
        this.meshUV = new Vector2[0];
    }

    public void GenerateBlocks()
    {

        for (int x = 0; x < Landscape.CHUNKSIZE; x++)
        {
            for (int y = 0; y < Landscape.CHUNKSIZE; y++)
            {
                for (int z = 0; z < Landscape.CHUNKSIZE; z++)
                {
                    Vector3Int globalPos = Landscape.GlobalPos(this.pos, new Vector3Int(x, y, z));
                    this.blocks[x, y, z] = Landscape.Instance.GetBlockType(globalPos);
                }
            }
        }
    }
    public BlockType this[Vector3Int v]
    {
        get => this[v.x, v.y, v.z];
        set => this[v.x, v.y, v.z] = value;
    }

    public BlockType this[int x, int y, int z]
    {
        get
        {
            if (IsValidIndex(x, y, z))
            {
                return this.blocks[x, y, z];
            }
            else return BlockType.OutOfBounds;
        }
        set
        {
            if (IsValidIndex(x, y, z))
            {
                this.blocks[x, y, z] = value;
            }
        }
    }

    public void GenerateMesh()
    {
        int vertsLength = 0;
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> tris = new List<int>();

        bool shouldWallFromTo(Vector3Int from, Vector3Int to)
        {
            return this[from] == BlockType.Filled && (this[to] == BlockType.None || this[to] == BlockType.OutOfBounds);
        }
        void Add2TriesAnd4Normals(Vector3 normalVector, int x, int y, int z)
        {
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normalVector);
            }
            tris.Add(vertsLength);
            tris.Add(vertsLength + 1);
            tris.Add(vertsLength + 3);
            tris.Add(vertsLength + 1);
            tris.Add(vertsLength + 2);
            tris.Add(vertsLength + 3);
            vertsLength += 4;

            if (normalVector == Vector3.up)
            {
                uv.Add(new Vector2(0.5f, 1));
                uv.Add(new Vector2(1, 1));
                uv.Add(new Vector2(1, 0.5f));
                uv.Add(new Vector2(0.5f, 0.5f));
            }
            else if (normalVector == Vector3.down)
            {
                uv.Add(new Vector2(0, 0.5f));
                uv.Add(new Vector2(0.5f, 0.5f));
                uv.Add(new Vector2(0.5f, 0));
                uv.Add(new Vector2(0, 0));
            }
            else // 4 sides
            {
                if (this[new Vector3Int(x, y + 1, z)] == BlockType.Filled)
                {
                    // pure dirt:
                    uv.Add(new Vector2(0, 0.5f));
                    uv.Add(new Vector2(0.5f, 0.5f));
                    uv.Add(new Vector2(0.5f, 0));
                    uv.Add(new Vector2(0, 0));
                }
                else
                {
                    // dirt side with grass top
                    uv.Add(new Vector2(0.5f, 0));
                    uv.Add(new Vector2(0.5f, 0.5f));
                    uv.Add(new Vector2(1f, 0.5f));
                    uv.Add(new Vector2(1f, 0));
                }

            }
        }
        for (int x = 0; x < Landscape.CHUNKSIZE; x++)
        {
            for (int y = 0; y < Landscape.CHUNKSIZE; y++)
            {
                for (int z = 0; z < Landscape.CHUNKSIZE; z++)
                {
                    if (shouldWallFromTo(new Vector3Int(x, y, z), new Vector3Int(x + 1, y, z)))
                    {
                        verts.Add(RelativePosition(x + 1, y, z));
                        verts.Add(RelativePosition(x + 1, y + 1, z));
                        verts.Add(RelativePosition(x + 1, y + 1, z + 1));
                        verts.Add(RelativePosition(x + 1, y, z + 1));


                        Add2TriesAnd4Normals(Vector3.right, x, y, z);
                    }
                    if (shouldWallFromTo(new Vector3Int(x, y, z), new Vector3Int(x - 1, y, z)))
                    {

                        verts.Add(RelativePosition(x, y, z + 1));
                        verts.Add(RelativePosition(x, y + 1, z + 1));
                        verts.Add(RelativePosition(x, y + 1, z));
                        verts.Add(RelativePosition(x, y, z));
                        Add2TriesAnd4Normals(Vector3.left, x, y, z);
                    }
                    if (shouldWallFromTo(new Vector3Int(x, y, z), new Vector3Int(x, y, z + 1)))
                    {

                        verts.Add(RelativePosition(x + 1, y, z + 1));
                        verts.Add(RelativePosition(x + 1, y + 1, z + 1));
                        verts.Add(RelativePosition(x, y + 1, z + 1));
                        verts.Add(RelativePosition(x, y, z + 1));
                        Add2TriesAnd4Normals(Vector3.forward, x, y, z);
                    }
                    if (shouldWallFromTo(new Vector3Int(x, y, z), new Vector3Int(x, y, z - 1)))
                    {
                        verts.Add(RelativePosition(x, y, z));
                        verts.Add(RelativePosition(x, y + 1, z));
                        verts.Add(RelativePosition(x + 1, y + 1, z));
                        verts.Add(RelativePosition(x + 1, y, z));
                        Add2TriesAnd4Normals(Vector3.back, x, y, z);
                    }
                    if (shouldWallFromTo(new Vector3Int(x, y, z), new Vector3Int(x, y + 1, z)))
                    {
                        verts.Add(RelativePosition(x, y + 1, z));
                        verts.Add(RelativePosition(x, y + 1, z + 1));
                        verts.Add(RelativePosition(x + 1, y + 1, z + 1));
                        verts.Add(RelativePosition(x + 1, y + 1, z));
                        Add2TriesAnd4Normals(Vector3.up, x, y, z);
                    }

                    if (shouldWallFromTo(new Vector3Int(x, y, z), new Vector3Int(x, y - 1, z)))
                    {
                        verts.Add(RelativePosition(x, y, z));
                        verts.Add(RelativePosition(x + 1, y, z));
                        verts.Add(RelativePosition(x + 1, y, z + 1));
                        verts.Add(RelativePosition(x, y, z + 1));
                        Add2TriesAnd4Normals(Vector3.down, x, y, z);
                    }

                }
            }
        }

        this.meshVertices = verts.ToArray();
        this.meshNormals = normals.ToArray();
        this.meshUV = uv.ToArray();
        this.meshTriangles = tris.ToArray();
    }

    public bool IsValidIndex(int x, int y, int z)
    {
        return x >= 0 && x < Landscape.CHUNKSIZE && y >= 0 && y < Landscape.CHUNKSIZE && z >= 0 && z < Landscape.CHUNKSIZE;
    }

    Vector3 RelativePosition(int x, int y, int z)
    {
        return new Vector3(x, y, z) * Landscape.BLOCKSIZE;
    }

}

class Chunk
{
    public ChunkData data;
    public GameObject gameObject;
    public bool markedForDeletion;
    public bool gameObjectCreated;

    public bool IsUniform()
    {
        return data.filledCount == 0 || data.filledCount == Math.Pow(Landscape.CHUNKSIZE, 3);
    }

    public BlockType this[Vector3Int v]
    {
        get => this.data[v];
        set => this.data[v] = value;
    }

    public BlockType this[int x, int y, int z]
    {
        get => this.data[x, y, z];
        set => this.data[x, y, z] = value;
    }


    public Vector3 GlobalPosition
    {
        get { return new Vector3(this.data.pos.x, this.data.pos.y, this.data.pos.z) * Landscape.CHUNKSIZE * Landscape.BLOCKSIZE; }
    }

    public Mesh GetMesh()
    {
        Mesh m = new Mesh();
        m.vertices = data.meshVertices;
        m.normals = data.meshNormals;
        m.triangles = data.meshTriangles;
        m.uv = data.meshUV;
        return m;
    }

    public Chunk()
    {
        this.data = new ChunkData(Vector3Int.zero);
    }

    public Chunk(Vector3Int pos)
    {
        this.data = new ChunkData(pos);
    }

    public void CreateGameObject()
    {
        if (gameObjectCreated) return;
        GameObject g = new GameObject();
        g.transform.position = this.GlobalPosition;
        MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = AssetManager.Instance.WorldMaterial;
        MeshFilter meshFilter = g.AddComponent<MeshFilter>();
        meshFilter.mesh = this.GetMesh();
        g.AddComponent<Destroyer>();
        this.gameObject = g;
        g.transform.SetParent(TerrainObject.Instance.gameObject.transform);
        gameObjectCreated = true;
    }

    public void DestroyGameObject()
    {
        if (!gameObjectCreated) return;
        this.gameObject.GetComponent<Destroyer>().Destroy();
        this.gameObject = null;
        gameObjectCreated = false;
    }

    public void Iter(Action<int, int, int> callback)
    {
        for (int x = 0; x < Landscape.CHUNKSIZE; x++)
        {
            for (int y = 0; y < Landscape.CHUNKSIZE; y++)
            {
                for (int z = 0; z < Landscape.CHUNKSIZE; z++)
                {
                    callback(x, y, z);
                }
            }
        }
    }
}
