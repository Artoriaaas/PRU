using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║             BATTLE SCENE BOOTSTRAPPER                           ║
/// ║                                                                  ║
/// ║  HOW TO USE:                                                     ║
/// ║  1. Open SampleScene (or any scene)                              ║
/// ║  2. Create an empty GameObject, name it "SceneManager"           ║
/// ║  3. Attach THIS script to it                                     ║
/// ║  4. Press PLAY — the full battle scene builds automatically      ║
/// ║                                                                  ║
/// ║  OR use the menu:  Tools → Battle Scene → Build Scene Now        ║
/// ╚══════════════════════════════════════════════════════════════════╝
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [Header("─── Enable/Disable Sections ───────────────────────────")]
    public bool buildTerrain = true;
    public bool buildForest  = true;
    public bool buildCastle  = true;
    public bool buildGrid    = true;
    public bool setupCamera  = true;
    public bool setupLighting = true;

    [Header("─── Forest ─────────────────────────────────────────────")]
    [Range(20, 300)] public int treeCount = 120;
    public float forestRadius      = 32f;
    public float forestInnerClear  = 11f;

    [Header("─── Castle ─────────────────────────────────────────────")]
    public Vector3 castlePosition  = new Vector3(0f, 0f, 37f);
    [Range(0.5f, 2f)] public float castleScale = 0.85f;

    [Header("─── Grid ───────────────────────────────────────────────")]
    [Range(4, 10)] public int gridColumns = 6;
    [Range(4, 10)] public int gridRows    = 6;
    [Range(0.8f, 3f)] public float cellSize = 1.6f;

    [Header("─── Camera ─────────────────────────────────────────────")]
    public Vector3 cameraPosition = new Vector3(9.513777f, 19f, 12.63755f);
    public Vector3 cameraRotation = new Vector3(51f, -87.369f, 0.011f);
    [Range(30f, 90f)] public float cameraFOV = 60f;

    // ── Runtime build ──────────────────────────────────────────────
    void Start()
    {
        BuildScene();
    }

    public void BuildScene()
    {
        var builder = new BattleSceneBuilder(this);
        builder.Build();
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  BATTLE SCENE BUILDER  (pure C#, no MonoBehaviour dependency)
// ═══════════════════════════════════════════════════════════════════════
public class BattleSceneBuilder
{
    private SceneBootstrapper _cfg;

    // Shared materials
    private Material _matGround, _matDirt, _matLeaf, _matTrunk;
    private Material _matStone, _matRoof, _matFlag, _matWater;
    private Material _matTileA, _matTileB, _matTileHL;

    public BattleSceneBuilder(SceneBootstrapper cfg) { _cfg = cfg; }

    // ──────────────────────────────────────────────────────────────
    public void Clear()
    {
        string[] roots = { "BattleTerrain", "BattleForest", "InnerCastle", 
                           "OuterCitadelWall", "BattleGrid", "BattleCamera", "BattleLight" };
        foreach (var r in roots)
        {
            var go = GameObject.Find(r);
            if (go) Object.DestroyImmediate(go);
        }
    }

    public void Build()
    {
        CreateMaterials();
        if (_cfg.buildTerrain)  BuildTerrain();
        if (_cfg.buildForest)   BuildForest();
        if (_cfg.buildCastle)   BuildCastle(_cfg.castlePosition, _cfg.castleScale);
        if (_cfg.buildCastle)   BuildOuterWall(_cfg.castlePosition, _cfg.castleScale);
        if (_cfg.buildGrid)     BuildGrid();
        if (_cfg.setupCamera)   SetupCamera();
        if (_cfg.setupLighting) SetupLighting();
    }

    // ══════════════════════════════════════════════════════════════
    //  MATERIALS
    // ══════════════════════════════════════════════════════════════
    // Extra materials for outer wall
    private Material _matOuterWall;   // dark aged stone
    private Material _matGateYellow;  // Vietnamese pavilion ochre/gold
    private Material _matGateRoof;    // dark glazed tile roof
    private Material _matGateDark;    // gate arch shadow/interior
    private Material _matRampStone;   // approach ramp

    void CreateMaterials()
    {
        _matGround = Solid(new Color(0.29f, 0.57f, 0.22f));
        _matDirt   = Solid(new Color(0.70f, 0.60f, 0.43f));
        _matLeaf   = Solid(new Color(0.18f, 0.52f, 0.13f));
        _matTrunk  = Solid(new Color(0.38f, 0.26f, 0.11f));
        _matStone  = Solid(new Color(0.74f, 0.69f, 0.62f));
        _matRoof   = Solid(new Color(0.26f, 0.22f, 0.52f));
        _matFlag   = Solid(new Color(0.88f, 0.12f, 0.10f));
        _matWater  = Solid(new Color(0.20f, 0.55f, 0.85f));

        _matTileA  = Solid(new Color(0.76f, 0.70f, 0.56f));
        _matTileB  = Solid(new Color(0.69f, 0.63f, 0.49f));
        _matTileHL = Transparent(new Color(0.28f, 0.92f, 0.38f, 0.50f));

        // Outer citadel wall colours (Thang Long inspired)
        _matOuterWall  = Solid(new Color(0.42f, 0.38f, 0.32f));  // dark weathered stone
        _matGateYellow = Solid(new Color(0.88f, 0.72f, 0.18f));  // Vietnamese ochre/gold
        _matGateRoof   = Solid(new Color(0.15f, 0.20f, 0.18f));  // dark glazed tile
        _matGateDark   = Solid(new Color(0.18f, 0.15f, 0.12f));  // arch interior shadow
        _matRampStone  = Solid(new Color(0.60f, 0.56f, 0.48f));  // approach courtyard
    }

    // ══════════════════════════════════════════════════════════════
    //  TERRAIN
    // ══════════════════════════════════════════════════════════════
    void BuildTerrain()
    {
        // Main green ground plane
        Cube("Ground", new Vector3(0f, -0.25f, 8f),
             new Vector3(90f, 0.5f, 90f), _matGround);

        // Sandy battle area
        Cube("ArenaFloor", new Vector3(0f, -0.05f, 0f),
             new Vector3(14f, 0.10f, 14f), _matDirt);

        // Slightly raised hill behind the arena (makes castle feel elevated)
        Cube("HillBack", new Vector3(0f, 0.4f, 28f),
             new Vector3(30f, 0.8f, 18f), _matGround);
        Cube("HillMid",  new Vector3(0f, 0.15f, 18f),
             new Vector3(22f, 0.3f, 10f), _matGround);
    }

    // ══════════════════════════════════════════════════════════════
    //  FOREST
    // ══════════════════════════════════════════════════════════════
    void BuildForest()
    {
        var forestParent = new GameObject("Forest");

        // ── Compute grid exclusion bounds (with safety margin) ────
        // Grid is centered at (0,0,0), size = columns*cellSize × rows*cellSize
        float halfGridX = (_cfg.gridColumns * _cfg.cellSize) * 0.5f + 2.0f;  // +2 margin
        float halfGridZ = (_cfg.gridRows    * _cfg.cellSize) * 0.5f + 2.0f;

        for (int i = 0; i < _cfg.treeCount; i++)
        {
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(_cfg.forestInnerClear, _cfg.forestRadius);

            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius + 10f;

            // ── Skip if inside the battle grid ─────────────────────
            if (Mathf.Abs(x) < halfGridX && Mathf.Abs(z) < halfGridZ) continue;

            // Keep area too far in front clear
            if (z < -14f) continue;
            // Keep approach corridor to castle clear
            if (z > 22f && z < 33f && Mathf.Abs(x) < 14f) continue;

            float s = Random.Range(0.55f, 1.5f);
            PlaceTree(new Vector3(x, 0f, z), s, forestParent.transform);
        }

        // Dense backdrop trees far behind castle
        for (int i = 0; i < 60; i++)
        {
            float x = Random.Range(-35f, 35f);
            float z = Random.Range(42f, 55f);
            float s = Random.Range(1.0f, 2.2f);
            PlaceTree(new Vector3(x, 0f, z), s, forestParent.transform);
        }
    }

    void PlaceTree(Vector3 pos, float scale, Transform parent)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;
        tree.transform.rotation = Quaternion.Euler(
            Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));

        // Trunk
        var trunk = Cube("Trunk", Vector3.zero,
            new Vector3(0.28f * scale, 1.1f * scale, 0.28f * scale), _matTrunk);
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0f, 0.55f * scale, 0f);

        // 3-layer foliage ball
        float[] fY = { 1.2f, 1.75f, 2.25f };
        float[] fS = { 1.05f, 0.88f, 0.60f };
        for (int i = 0; i < 3; i++)
        {
            var leaf = Sphere("Leaf", Vector3.zero,
                Vector3.one * fS[i] * scale, _matLeaf);
            leaf.transform.SetParent(tree.transform);
            leaf.transform.localPosition = new Vector3(0f, fY[i] * scale, 0f);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  CASTLE
    // ══════════════════════════════════════════════════════════════
    void BuildCastle(Vector3 origin, float sc)
    {
        var root = new GameObject("Castle");
        root.transform.position   = origin;
        root.transform.localScale = Vector3.one * sc;

        // ── Base platform ──────────────────────────────────────────
        C(root, "Platform",  new Vector3(0, -0.5f, 0),  new Vector3(15, 1, 10),  _matStone);

        // ── Curtain Walls ──────────────────────────────────────────
        C(root, "WallFront", new Vector3(0, 2.5f, -4),  new Vector3(9, 5, 0.6f), _matStone);
        C(root, "WallBack",  new Vector3(0, 2.5f, 4),   new Vector3(9, 5, 0.6f), _matStone);
        C(root, "WallLeft",  new Vector3(-4.5f, 2.5f, 0), new Vector3(0.6f, 5, 8), _matStone);
        C(root, "WallRight", new Vector3(4.5f, 2.5f, 0),  new Vector3(0.6f, 5, 8), _matStone);

        // Wall battlements (front wall)
        for (int i = -3; i <= 3; i += 2)
        {
            C(root, "WM_F" + i, new Vector3(i, 5.5f, -4),  new Vector3(0.7f, 0.9f, 0.7f), _matStone);
            C(root, "WM_B" + i, new Vector3(i, 5.5f, 4),   new Vector3(0.7f, 0.9f, 0.7f), _matStone);
        }
        for (int i = -3; i <= 3; i += 2)
        {
            C(root, "WM_L" + i, new Vector3(-4.5f, 5.5f, i), new Vector3(0.7f, 0.9f, 0.7f), _matStone);
            C(root, "WM_R" + i, new Vector3(4.5f, 5.5f, i),  new Vector3(0.7f, 0.9f, 0.7f), _matStone);
        }

        // ── Gate (front opening) ───────────────────────────────────
        C(root, "GateL", new Vector3(-1.3f, 1.8f, -4), new Vector3(1.1f, 3.6f, 0.7f), _matStone);
        C(root, "GateR", new Vector3( 1.3f, 1.8f, -4), new Vector3(1.1f, 3.6f, 0.7f), _matStone);
        C(root, "GateT", new Vector3(0,     3.8f, -4), new Vector3(3.4f, 0.8f, 0.7f), _matStone);

        // Gate archway shadow (dark stone above arch)
        C(root, "GateShadow", new Vector3(0, 2.0f, -4.0f), new Vector3(2.5f, 3.2f, 0.1f),
            Solid(new Color(0.25f, 0.22f, 0.18f)));

        // ── Main Keep ─────────────────────────────────────────────
        C(root, "Keep",      new Vector3(0, 5.5f, 1),    new Vector3(7.5f, 8, 5.5f), _matStone);
        C(root, "KeepRoof",  new Vector3(0, 9.8f, 1),    new Vector3(8f, 0.8f, 6f),  _matRoof);

        // Keep battlements
        for (int i = -3; i <= 3; i += 2)
        {
            C(root, "KM_" + i, new Vector3(i, 10.4f, 1), new Vector3(0.75f, 1.0f, 5.6f), _matStone);
        }

        // ── Four Corner Towers ─────────────────────────────────────
        float[,] tc = { {-5, -4}, {5, -4}, {-5, 4}, {5, 4} };
        for (int i = 0; i < 4; i++)
        {
            float tx = tc[i, 0], tz = tc[i, 1];
            // Body
            C(root, "T" + i,      new Vector3(tx, 5.5f, tz),   new Vector3(2.6f, 12, 2.6f), _matStone);
            // Top cap
            C(root, "TCap" + i,   new Vector3(tx, 11.8f, tz),  new Vector3(3.0f, 0.6f, 3.0f), _matStone);
            // Pyramid roof (tall narrow box)
            C(root, "TRoof" + i,  new Vector3(tx, 13.2f, tz),  new Vector3(2.8f, 2.8f, 2.8f), _matRoof);
            // Flag pole
            C(root, "TPole" + i,  new Vector3(tx, 15.0f, tz),  new Vector3(0.13f, 3f, 0.13f), _matStone);
            // Flag banner
            C(root, "TFlag" + i,  new Vector3(tx + 0.65f, 16.2f, tz), new Vector3(1.1f, 0.65f, 0.12f), _matFlag);
            // Tower battlements
            for (int m = -1; m <= 1; m += 2)
            {
                C(root, "TM" + i + "_" + m,
                  new Vector3(tx + m * 0.85f, 12.0f, tz),
                  new Vector3(0.55f, 0.75f, 2.6f), _matStone);
                C(root, "TMZ" + i + "_" + m,
                  new Vector3(tx, 12.0f, tz + m * 0.85f),
                  new Vector3(2.6f, 0.75f, 0.55f), _matStone);
            }
        }

        // ── Small moat/water strip in front ───────────────────────
        C(root, "Moat", new Vector3(0, -0.65f, -6.5f), new Vector3(18, 0.4f, 2.5f), _matWater);
    }

    // ══════════════════════════════════════════════════════════════
    //  OUTER CITADEL WALL  (Thang Long style — fixed stacking)
    // ══════════════════════════════════════════════════════════════
    void BuildOuterWall(Vector3 castleOrigin, float sc)
    {
        var root = new GameObject("OuterCitadelWall");
        root.transform.position = castleOrigin + new Vector3(0f, 0f, -4f);

        float wallW = 34f, wallD = 26f, wallH = 5.5f, wallThick = 2.2f;
        float halfW = wallW * 0.5f, halfD = wallD * 0.5f;
        float wallCY = wallH * 0.5f;
        float merH = 1.0f, merW = 0.9f;
        float gateW = 5.5f, sideW = (wallW - gateW) * 0.5f;

        // ── Footing ──────────────────────────────────────────────
        OW(root, "FootFront", new Vector3(0f,     0.4f, -halfD), new Vector3(wallW+1f, 0.8f, wallThick+1.5f), _matOuterWall);
        OW(root, "FootBack",  new Vector3(0f,     0.4f,  halfD), new Vector3(wallW+1f, 0.8f, wallThick+1.5f), _matOuterWall);
        OW(root, "FootL",     new Vector3(-halfW, 0.4f, 0f),     new Vector3(wallThick+1.5f, 0.8f, wallD),    _matOuterWall);
        OW(root, "FootR",     new Vector3( halfW, 0.4f, 0f),     new Vector3(wallThick+1.5f, 0.8f, wallD),    _matOuterWall);

        // ── Front wall (split left/right of gate opening) ────────
        OW(root, "FrontL",
            new Vector3(-(gateW*0.5f+sideW*0.5f), wallCY, -halfD),
            new Vector3(sideW, wallH, wallThick), _matOuterWall);
        OW(root, "FrontR",
            new Vector3( gateW*0.5f+sideW*0.5f,  wallCY, -halfD),
            new Vector3(sideW, wallH, wallThick), _matOuterWall);
        // Gate: dark interior arch + solid lintel on top
        OW(root, "GateArch",
            new Vector3(0f, wallH * 0.40f, -halfD),
            new Vector3(gateW-0.5f, wallH*0.80f, wallThick+0.3f), _matGateDark);
        OW(root, "GateLint",
            new Vector3(0f, wallH - 0.5f, -halfD),
            new Vector3(gateW+0.4f, 1.0f, wallThick+0.1f), _matOuterWall);
        // Front battlements
        for (int i = 0; i < 12; i++)
        {
            float mx = -halfW + i * (wallW / 11f);
            if (Mathf.Abs(mx) < gateW * 0.55f) continue;
            OW(root, "FM"+i, new Vector3(mx, wallH+merH*0.5f, -halfD),
               new Vector3(merW, merH, wallThick*0.8f), _matOuterWall);
        }

        // ── Back wall ────────────────────────────────────────────
        OW(root, "BackWall", new Vector3(0f, wallCY, halfD), new Vector3(wallW, wallH, wallThick), _matOuterWall);
        for (int i = 0; i < 10; i++)
        {
            float mx = -halfW + i * (wallW / 9f);
            OW(root, "BM"+i, new Vector3(mx, wallH+merH*0.5f, halfD),
               new Vector3(merW, merH, wallThick*0.8f), _matOuterWall);
        }

        // ── Side walls ───────────────────────────────────────────
        OW(root, "LeftWall",  new Vector3(-halfW, wallCY, 0f), new Vector3(wallThick, wallH, wallD), _matOuterWall);
        OW(root, "RightWall", new Vector3( halfW, wallCY, 0f), new Vector3(wallThick, wallH, wallD), _matOuterWall);
        for (int i = 0; i < 8; i++)
        {
            float mz = -halfD + i * (wallD / 7f);
            OW(root, "LM"+i, new Vector3(-halfW, wallH+merH*0.5f, mz), new Vector3(wallThick*0.8f, merH, merW), _matOuterWall);
            OW(root, "RM"+i, new Vector3( halfW, wallH+merH*0.5f, mz), new Vector3(wallThick*0.8f, merH, merW), _matOuterWall);
        }

        // ── Corner bastions ──────────────────────────────────────
        float bastH = wallH + 1.5f;
        float[] bx = { -halfW, halfW, -halfW,  halfW };
        float[] bz = { -halfD, -halfD,  halfD,  halfD };
        for (int i = 0; i < 4; i++)
        {
            OW(root, "Bast"+i,    new Vector3(bx[i], bastH*0.5f,  bz[i]), new Vector3(5.5f, bastH, 5.5f), _matOuterWall);
            OW(root, "BastCap"+i, new Vector3(bx[i], bastH+0.3f,  bz[i]), new Vector3(6.0f, 0.6f,  6.0f), _matOuterWall);
            for (int m = -1; m <= 1; m += 2)
            {
                OW(root, "BCX"+i+m, new Vector3(bx[i]+m*2.2f, bastH+0.9f, bz[i]),     new Vector3(0.8f, 1.0f, 5.5f), _matOuterWall);
                OW(root, "BCZ"+i+m, new Vector3(bx[i], bastH+0.9f,         bz[i]+m*2.2f), new Vector3(5.5f, 1.0f, 0.8f), _matOuterWall);
            }
        }

        // ── Courtyard in front of gate ───────────────────────────
        OW(root, "Courtyard", new Vector3(0f, 0.05f, -halfD-6f), new Vector3(14f, 0.1f, 10f), _matRampStone);

        // ══════════════════════════════════════════════════════════
        //  GATE PAVILION — botY accumulator ensures zero gaps
        // ══════════════════════════════════════════════════════════
        float px = 0f, pz = -halfD;
        float botY = wallH;           // start at top of wall

        // Base slab ───────────────────────────────────────────────
        float basH = 1.2f;
        OW(root, "PavBase",
            new Vector3(px, botY + basH*0.5f, pz),
            new Vector3(gateW+5f, basH, wallThick+3.5f), _matOuterWall);
        float colBaseY = botY + basH;  // columns start here
        botY += basH;                  // ← botY now = 6.7

        // Tier 1 body ─────────────────────────────────────────────
        float t1H = 3.0f;
        OW(root, "PavTier1",
            new Vector3(px, botY + t1H*0.5f, pz),
            new Vector3(gateW+3.5f, t1H, wallThick+2.5f), _matGateYellow);
        botY += t1H;                   // ← botY = 9.7
        botY = PlaceRoof(root, px, botY, pz, gateW+5.5f, wallThick+4.5f, 1.4f, "R1");

        // Tier 2 body ─────────────────────────────────────────────
        float t2H = 2.0f;
        OW(root, "PavTier2",
            new Vector3(px, botY + t2H*0.5f, pz),
            new Vector3(gateW+1.5f, t2H, wallThick+1.2f), _matGateYellow);
        botY += t2H;
        botY = PlaceRoof(root, px, botY, pz, gateW+3.5f, wallThick+3.0f, 1.1f, "R2");

        // Tier 3 body ─────────────────────────────────────────────
        float t3H = 1.4f;
        OW(root, "PavTier3",
            new Vector3(px, botY + t3H*0.5f, pz),
            new Vector3(gateW-1.0f, t3H, wallThick), _matGateYellow);
        botY += t3H;
        botY = PlaceRoof(root, px, botY, pz, gateW+1.5f, wallThick+1.8f, 0.8f, "R3");

        // Flag pole + banner ──────────────────────────────────────
        OW(root, "FlagPole",
            new Vector3(px, botY + 2.0f, pz),
            new Vector3(0.18f, 4.0f, 0.18f), _matOuterWall);
        OW(root, "FlagBanner",
            new Vector3(px + 1.0f, botY + 3.8f, pz),
            new Vector3(2.0f, 1.0f, 0.12f), _matFlag);

        // Decorative columns flanking gate ────────────────────────
        float[] colXs = { -(gateW*0.5f+0.8f), gateW*0.5f+0.8f };
        foreach (float cx in colXs)
            OW(root, "Col"+cx,
                new Vector3(cx, colBaseY + t1H*0.5f, pz),
                new Vector3(0.45f, t1H, 0.45f), _matGateYellow);
    }

    /// <summary>
    /// Places a Vietnamese curved-roof layer bottom-up.
    /// Eave slab sits at botY, ridge stacks on top, corner tips at eave corners.
    /// Returns new botY = top of ridge (ready for next piece to stack on).
    /// </summary>
    float PlaceRoof(GameObject parent, float x, float botY, float z,
                    float width, float depth, float height, string tag)
    {
        float eaveH  = height * 0.38f;
        float ridgeH = height * 0.52f;
        float tipH   = height * 0.28f;
        float tipW   = height * 0.36f;

        // Eave slab
        OW(parent, tag+"_Eave",
            new Vector3(x, botY + eaveH*0.5f, z),
            new Vector3(width, eaveH, depth), _matGateRoof);
        float topEave = botY + eaveH;

        // Ridge sits flush on top of eave
        OW(parent, tag+"_Ridge",
            new Vector3(x, topEave + ridgeH*0.5f, z),
            new Vector3(width*0.48f, ridgeH, depth*0.48f), _matGateRoof);

        // Corner upturn tips — at eave bottom level, protruding outward
        float tx = width * 0.45f, tz = depth * 0.45f;
        float tipCY = botY + tipH*0.5f;
        OW(parent, tag+"_TFL", new Vector3(x-tx, tipCY, z-tz), new Vector3(tipW, tipH, tipW), _matGateRoof);
        OW(parent, tag+"_TFR", new Vector3(x+tx, tipCY, z-tz), new Vector3(tipW, tipH, tipW), _matGateRoof);
        OW(parent, tag+"_TBL", new Vector3(x-tx, tipCY, z+tz), new Vector3(tipW, tipH, tipW), _matGateRoof);
        OW(parent, tag+"_TBR", new Vector3(x+tx, tipCY, z+tz), new Vector3(tipW, tipH, tipW), _matGateRoof);

        return topEave + ridgeH;   // return top of roof for next piece
    }

    /// <summary>Shorthand: child cube at LOCAL position under parent.</summary>
    void OW(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = Cube(name, Vector3.zero, size, mat);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
    }


    // ══════════════════════════════════════════════════════════════
    //  BATTLE GRID
    // ══════════════════════════════════════════════════════════════
    void BuildGrid()
    {
        var gridRoot = new GameObject("BattleGrid");

        int cols = _cfg.gridColumns;
        int rows = _cfg.gridRows;
        float cs = _cfg.cellSize;

        float totalW = cols * cs;
        float totalD = rows * cs;
        Vector3 origin = new Vector3(-totalW * 0.5f, 0.01f, -totalD * 0.5f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = origin.x + c * cs + cs * 0.5f;
                float z = origin.z + r * cs + cs * 0.5f;
                Vector3 pos = new Vector3(x, 0.01f, z);

                bool even = (r + c) % 2 == 0;
                var tile = Cube($"Tile_{r}_{c}", pos,
                    new Vector3(cs * 0.97f, 0.07f, cs * 0.97f),
                    even ? _matTileA : _matTileB);
                tile.transform.SetParent(gridRoot.transform);

                // Green "+" highlight on each tile
                PlacePlus(pos + Vector3.up * 0.05f, cs, tile.transform);
            }
        }

        // Grid outer border
        float edge = 0.22f;
        float hy   = 0.22f;
        float by   = 0.11f;
        // Front / Back
        var bF = Cube("GridBorder_F", new Vector3(0, by, origin.z - edge * 0.5f),
            new Vector3(totalW + edge * 2f, hy, edge), _matDirt);
        var bB = Cube("GridBorder_B", new Vector3(0, by, -origin.z + edge * 0.5f),
            new Vector3(totalW + edge * 2f, hy, edge), _matDirt);
        // Left / Right
        var bL = Cube("GridBorder_L", new Vector3(origin.x - edge * 0.5f, by, 0),
            new Vector3(edge, hy, totalD), _matDirt);
        var bR = Cube("GridBorder_R", new Vector3(-origin.x + edge * 0.5f, by, 0),
            new Vector3(edge, hy, totalD), _matDirt);

        bF.transform.SetParent(gridRoot.transform);
        bB.transform.SetParent(gridRoot.transform);
        bL.transform.SetParent(gridRoot.transform);
        bR.transform.SetParent(gridRoot.transform);
    }

    void PlacePlus(Vector3 center, float cs, Transform parent)
    {
        // Horizontal bar
        var h = Cube("PH", center, new Vector3(cs * 0.6f, 0.01f, cs * 0.18f), _matTileHL);
        h.transform.SetParent(parent);
        // Vertical bar
        var v = Cube("PV", center, new Vector3(cs * 0.18f, 0.01f, cs * 0.6f), _matTileHL);
        v.transform.SetParent(parent);
    }

    // ══════════════════════════════════════════════════════════════
    //  CAMERA — diagonal side-angle (landscape PC, soldier-friendly)
    // ══════════════════════════════════════════════════════════════
    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Remove stale LockedCamera if rebuilding
        var old = cam.GetComponent<LockedCamera>();
        if (old != null) Object.DestroyImmediate(old);

        var lc = cam.gameObject.AddComponent<LockedCamera>();
        lc.position     = _cfg.cameraPosition;
        lc.rotation     = _cfg.cameraRotation;
        lc.fieldOfView  = _cfg.cameraFOV;
        
        lc.skyColor     = new Color(0.44f, 0.67f, 0.94f, 1f);
        lc.clearMode    = CameraClearFlags.SolidColor;
        lc.lockPosition = true;
        lc.lockRotation = true;
        lc.Apply();
    }

    // ══════════════════════════════════════════════════════════════
    //  LIGHTING
    // ══════════════════════════════════════════════════════════════
    void SetupLighting()
    {
        Light sun = null;
        foreach (var l in Object.FindObjectsOfType<Light>())
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun    = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
        sun.color              = new Color(1.0f, 0.95f, 0.82f);
        sun.intensity          = 1.7f;
        sun.shadows            = LightShadows.Soft;

        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.38f, 0.52f, 0.68f);
        RenderSettings.ambientIntensity = 0.85f;

        RenderSettings.fog              = true;
        RenderSettings.fogColor         = new Color(0.65f, 0.80f, 0.95f);
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance   = 75f;
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════
    GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col) Object.Destroy(col);
        return go;
    }

    // Shorthand: castle-child cube (local position)
    void C(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = Cube(name, Vector3.zero, size, mat);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
    }

    GameObject Sphere(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col) Object.Destroy(col);
        return go;
    }

    Material Solid(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.shader.name == "Hidden/InternalErrorShader")
            m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Smoothness", 0.05f);
        return m;
    }

    Material Transparent(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bool urp = m.shader.name != "Hidden/InternalErrorShader";

        if (!urp)
        {
            m = new Material(Shader.Find("Standard"));
            m.SetFloat("_Mode", 3);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
        }
        else
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
        m.color = c;
        return m;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EDITOR MENU (Unity Editor only)
// ═══════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(SceneBootstrapper))]
public class SceneBootstrapperEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);

        if (GUILayout.Button("▶  Build Scene Now (Editor)", GUILayout.Height(36)))
        {
            var bs = (SceneBootstrapper)target;
            var builder = new BattleSceneBuilder(bs);
            builder.Clear(); // Automatically clean up old stuff first
            builder.Build();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        GUILayout.Space(4);

        if (GUILayout.Button("✖  Clear Generated Scene", GUILayout.Height(24)))
        {
            var bs = (SceneBootstrapper)target;
            var builder = new BattleSceneBuilder(bs);
            builder.Clear();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
#endif
