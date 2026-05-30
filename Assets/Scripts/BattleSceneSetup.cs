using UnityEngine;

/// <summary>
/// Procedurally builds the full battle scene:
/// - Terrain (green ground)
/// - Forest (trees in background)
/// - Castle in the far distance
/// - Battle grid in the center
/// - Sky atmosphere
/// - Lighting setup
/// 
/// Attach to an empty GameObject and press Play — the entire scene is generated at runtime.
/// </summary>
[RequireComponent(typeof(BattleGridController))]
public class BattleSceneSetup : MonoBehaviour
{
    [Header("Scene Scale")]
    public float terrainSize = 80f;

    [Header("Forest")]
    [Range(40, 200)]
    public int treeCount = 120;
    public float forestRadius = 30f;
    public float forestInnerClear = 10f;

    [Header("Castle")]
    public Vector3 castlePosition = new Vector3(0f, 0f, 36f);

    [Header("Colors")]
    public Color groundColor      = new Color(0.30f, 0.58f, 0.22f);
    public Color dirtColor        = new Color(0.68f, 0.58f, 0.42f);
    public Color treeLeafColor    = new Color(0.22f, 0.52f, 0.15f);
    public Color treeTrunkColor   = new Color(0.40f, 0.27f, 0.12f);
    public Color castleStoneColor = new Color(0.72f, 0.68f, 0.62f);
    public Color castleRoofColor  = new Color(0.30f, 0.25f, 0.55f);

    // ── Cached materials ──────────────────────────────────────────────────
    private Material _matGround, _matDirt, _matLeaf, _matTrunk;
    private Material _matStone, _matRoof, _matFlag;

    void Awake()
    {
        CreateMaterials();
        BuildTerrain();
        BuildForest();
        BuildCastle(castlePosition);
        SetupCamera();
        SetupLighting();
    }

    // ─────────────────────────────────────────────────────────────────────
    // MATERIALS
    // ─────────────────────────────────────────────────────────────────────
    void CreateMaterials()
    {
        _matGround = MakeMat(groundColor);
        _matDirt   = MakeMat(dirtColor);
        _matLeaf   = MakeMat(treeLeafColor);
        _matTrunk  = MakeMat(treeTrunkColor);
        _matStone  = MakeMat(castleStoneColor);
        _matRoof   = MakeMat(castleRoofColor);
        _matFlag   = MakeMat(new Color(0.85f, 0.12f, 0.12f));
    }

    Material MakeMat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.shader.name == "Hidden/InternalErrorShader")
            m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }

    // ─────────────────────────────────────────────────────────────────────
    // TERRAIN
    // ─────────────────────────────────────────────────────────────────────
    void BuildTerrain()
    {
        // Main flat ground
        var ground = CreateCube("Ground", Vector3.zero,
            new Vector3(terrainSize, 0.4f, terrainSize), _matGround);
        ground.transform.position = new Vector3(0f, -0.2f, 8f);

        // Sandy battle area beneath the grid
        var arena = CreateCube("ArenaFloor", new Vector3(0f, -0.05f, 0f),
            new Vector3(12f, 0.1f, 12f), _matDirt);
    }

    // ─────────────────────────────────────────────────────────────────────
    // FOREST
    // ─────────────────────────────────────────────────────────────────────
    void BuildForest()
    {
        var forestParent = new GameObject("Forest");

        for (int i = 0; i < treeCount; i++)
        {
            // Random angle + radius — keep inner area clear
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(forestInnerClear, forestRadius);

            // Bias trees toward the back (positive Z)
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius + 8f;  // shift whole forest back

            // Avoid placing trees inside the castle zone
            if (z > 28f && Mathf.Abs(x) < 12f) continue;

            float scale = Random.Range(0.6f, 1.6f);
            PlaceTree(new Vector3(x, 0f, z), scale, forestParent.transform);
        }
    }

    void PlaceTree(Vector3 pos, float scale, Transform parent)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent);

        // Trunk
        var trunk = CreateCube("Trunk", Vector3.zero,
            new Vector3(0.3f * scale, 1.2f * scale, 0.3f * scale), _matTrunk);
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0f, 0.6f * scale, 0f);

        // Foliage (3 stacked spheres for a lush look)
        float[] fHeights = { 1.3f, 1.9f, 2.4f };
        float[] fScales  = { 1.1f, 0.9f, 0.65f };
        for (int i = 0; i < 3; i++)
        {
            var leaf = CreateSphere("Leaf" + i,
                new Vector3(0f, fHeights[i] * scale, 0f),
                Vector3.one * fScales[i] * scale, _matLeaf);
            leaf.transform.SetParent(tree.transform);
        }

        tree.transform.position = new Vector3(pos.x, 0f, pos.z);

        // Slight random rotation & tilt for natural look
        tree.transform.rotation = Quaternion.Euler(
            Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
    }

    // ─────────────────────────────────────────────────────────────────────
    // CASTLE
    // ─────────────────────────────────────────────────────────────────────
    void BuildCastle(Vector3 origin)
    {
        var castleRoot = new GameObject("Castle");
        castleRoot.transform.position = origin;

        // ── Main Keep (central tower) ──
        AddCastle(castleRoot, "Keep",
            new Vector3(0f, 3.5f, 0f), new Vector3(7f, 7f, 5f), _matStone);

        // Keep battlements (top row of merlons)
        for (int i = -3; i <= 3; i += 2)
        {
            AddCastle(castleRoot, "Merlon_Keep_" + i,
                new Vector3(i * 0.9f, 7.6f, 0f), new Vector3(0.7f, 0.8f, 5.2f), _matStone);
        }

        // ── Gate house (front arch effect) ──
        AddCastle(castleRoot, "GateLeft",
            new Vector3(-1.5f, 2f, -2.6f), new Vector3(1.2f, 4f, 0.4f), _matStone);
        AddCastle(castleRoot, "GateRight",
            new Vector3( 1.5f, 2f, -2.6f), new Vector3(1.2f, 4f, 0.4f), _matStone);
        AddCastle(castleRoot, "GateTop",
            new Vector3(0f, 4.3f, -2.6f), new Vector3(3.5f, 0.8f, 0.4f), _matStone);

        // ── Corner Towers ──
        float[] tx = { -4.5f, 4.5f, -4.5f, 4.5f };
        float[] tz = { -3f,   -3f,    3f,    3f  };
        for (int i = 0; i < 4; i++)
        {
            // Tower body
            AddCastle(castleRoot, "Tower_" + i,
                new Vector3(tx[i], 5f, tz[i]), new Vector3(2.4f, 10f, 2.4f), _matStone);
            // Conical roof (approximated with a tall thin pyramid-like box)
            AddCastle(castleRoot, "TowerRoof_" + i,
                new Vector3(tx[i], 10.4f, tz[i]), new Vector3(2.6f, 1.5f, 2.6f), _matRoof);
            // Tower flag pole
            AddCastle(castleRoot, "Pole_" + i,
                new Vector3(tx[i], 11.8f, tz[i]), new Vector3(0.12f, 2.5f, 0.12f), _matStone);
            // Flag
            AddCastle(castleRoot, "Flag_" + i,
                new Vector3(tx[i] + 0.55f, 12.8f, tz[i]), new Vector3(1.0f, 0.6f, 0.1f), _matFlag);
            // Battlements on top of each tower
            for (int m = -1; m <= 1; m += 2)
            {
                AddCastle(castleRoot, "TowerMerlon_" + i + "_" + m,
                    new Vector3(tx[i] + m * 0.7f, 10.5f, tz[i]), new Vector3(0.5f, 0.7f, 2.4f), _matStone);
            }
        }

        // ── Curtain Walls ──
        // Front wall
        AddCastle(castleRoot, "WallFront",
            new Vector3(0f, 2f, -3f), new Vector3(7f, 4f, 0.5f), _matStone);
        // Back wall
        AddCastle(castleRoot, "WallBack",
            new Vector3(0f, 2f, 3f), new Vector3(7f, 4f, 0.5f), _matStone);
        // Left wall
        AddCastle(castleRoot, "WallLeft",
            new Vector3(-4.5f, 2f, 0f), new Vector3(0.5f, 4f, 6f), _matStone);
        // Right wall
        AddCastle(castleRoot, "WallRight",
            new Vector3( 4.5f, 2f, 0f), new Vector3(0.5f, 4f, 6f), _matStone);

        // Wall battlements
        for (int i = -3; i <= 3; i += 2)
        {
            AddCastle(castleRoot, "WallMerlon_F_" + i,
                new Vector3(i * 0.9f, 4.6f, -3f), new Vector3(0.6f, 0.7f, 0.5f), _matStone);
            AddCastle(castleRoot, "WallMerlon_B_" + i,
                new Vector3(i * 0.9f, 4.6f, 3f), new Vector3(0.6f, 0.7f, 0.5f), _matStone);
        }

        // ── Base platform ──
        AddCastle(castleRoot, "BasePlatform",
            new Vector3(0f, -0.35f, 0f), new Vector3(12f, 0.7f, 8f), _matStone);

        // Scale castle down a bit so it reads as "far away"
        castleRoot.transform.localScale = Vector3.one * 0.85f;
    }

    void AddCastle(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = CreateCube(name, Vector3.zero, size, mat);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
    }

    // ─────────────────────────────────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────────────────────────────────
    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Isometric-ish top-down angle matching the reference image
        cam.transform.position    = new Vector3(0f, 18f, -14f);
        cam.transform.rotation    = Quaternion.Euler(48f, 0f, 0f);
        cam.fieldOfView           = 55f;
        cam.backgroundColor       = new Color(0.45f, 0.70f, 0.95f);
        cam.clearFlags            = CameraClearFlags.SolidColor;
    }

    // ─────────────────────────────────────────────────────────────────────
    // LIGHTING
    // ─────────────────────────────────────────────────────────────────────
    void SetupLighting()
    {
        // Find or use existing directional light
        var lights = FindObjectsOfType<Light>();
        Light sun = null;
        foreach (var l in lights)
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
        sun.color     = new Color(1.0f, 0.95f, 0.80f);
        sun.intensity = 1.6f;
        sun.shadows   = LightShadows.Soft;

        // Ambient
        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.40f, 0.55f, 0.70f);
        RenderSettings.ambientIntensity = 0.8f;

        // Soft fog for depth
        RenderSettings.fog             = true;
        RenderSettings.fogColor        = new Color(0.68f, 0.82f, 0.96f);
        RenderSettings.fogMode         = FogMode.Linear;
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance   = 70f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────
    GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position   = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        // Remove colliders on purely visual objects to keep scene light
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }

    GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position   = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }
}
