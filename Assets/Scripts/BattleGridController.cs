using UnityEngine;

/// <summary>
/// Creates the battle grid — a 6x6 (or configurable) checkerboard of tiles
/// with glowing green "+" markers on each cell, matching the reference image style.
/// Also draws the isometric grid lines using LineRenderer quads.
/// 
/// Works together with BattleSceneSetup.cs (same GameObject).
/// </summary>
public class BattleGridController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Grid Dimensions")]
    [Range(4, 10)] public int columns = 6;
    [Range(4, 10)] public int rows    = 6;
    [Tooltip("World-space size of each cell (width and depth).")]
    public float cellSize = 1.6f;

    [Header("Appearance")]
    public Color tileColorA     = new Color(0.76f, 0.70f, 0.56f, 1f);  // sand light
    public Color tileColorB     = new Color(0.70f, 0.64f, 0.50f, 1f);  // sand dark
    public Color tileHighlight  = new Color(0.30f, 0.90f, 0.40f, 0.55f); // transparent green
    public Color gridLineColor  = new Color(0.85f, 0.85f, 0.85f, 0.8f);

    [Header("Position")]
    [Tooltip("World position of the grid centre.")]
    public Vector3 gridCenter = new Vector3(0f, 0.01f, 0f);

    // ── Private ───────────────────────────────────────────────────────────
    private GameObject _gridRoot;
    private Material   _matA, _matB, _matHighlight;

    void Awake()
    {
        BuildGrid();
    }

    // ─────────────────────────────────────────────────────────────────────
    // BUILD
    // ─────────────────────────────────────────────────────────────────────
    void BuildGrid()
    {
        _gridRoot = new GameObject("BattleGrid");

        // Create materials
        _matA         = MakeMat(tileColorA,    0.05f);
        _matB         = MakeMat(tileColorB,    0.05f);
        _matHighlight = MakeTransparentMat(tileHighlight);

        float totalW = columns * cellSize;
        float totalD = rows    * cellSize;
        Vector3 originOffset = new Vector3(-totalW * 0.5f, 0f, -totalD * 0.5f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = originOffset.x + c * cellSize + cellSize * 0.5f;
                float z = originOffset.z + r * cellSize + cellSize * 0.5f;
                Vector3 pos = gridCenter + new Vector3(x, 0f, z);

                // Alternating tile
                bool even = (r + c) % 2 == 0;
                var tile = CreateTile(r, c, pos, even ? _matA : _matB);
                tile.transform.SetParent(_gridRoot.transform);

                // Highlight overlay (green plus/glow effect on every tile)
                AddHighlight(pos, tile.transform);
            }
        }

        // Grid border / outline
        AddGridBorder(originOffset, totalW, totalD);
    }

    // ─────────────────────────────────────────────────────────────────────
    // TILE
    // ─────────────────────────────────────────────────────────────────────
    GameObject CreateTile(int row, int col, Vector3 worldPos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Tile_{row}_{col}";
        go.transform.position   = worldPos;
        go.transform.localScale = new Vector3(cellSize * 0.98f, 0.08f, cellSize * 0.98f);
        go.GetComponent<Renderer>().material = mat;
        var col2 = go.GetComponent<Collider>();
        if (col2 != null) Destroy(col2);
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HIGHLIGHT (green "+" marker)
    // ─────────────────────────────────────────────────────────────────────
    void AddHighlight(Vector3 tilePos, Transform parent)
    {
        // Horizontal bar of the plus
        var hBar = CreateFlatQuad("HBar",
            tilePos + new Vector3(0f, 0.05f, 0f),
            new Vector3(cellSize * 0.65f, 0.01f, cellSize * 0.20f),
            _matHighlight);
        hBar.transform.SetParent(parent);

        // Vertical bar of the plus
        var vBar = CreateFlatQuad("VBar",
            tilePos + new Vector3(0f, 0.05f, 0f),
            new Vector3(cellSize * 0.20f, 0.01f, cellSize * 0.65f),
            _matHighlight);
        vBar.transform.SetParent(parent);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GRID BORDER (thin raised edges around the whole grid)
    // ─────────────────────────────────────────────────────────────────────
    void AddGridBorder(Vector3 originOffset, float totalW, float totalD)
    {
        var borderMat = MakeMat(new Color(0.55f, 0.50f, 0.38f), 0f);
        float thickness = 0.18f;
        float height    = 0.20f;
        float cy        = gridCenter.y + height * 0.5f;

        // Front
        var front = CreateFlatQuad("Border_F",
            gridCenter + new Vector3(0f, cy, originOffset.z - thickness * 0.5f),
            new Vector3(totalW + thickness * 2f, height, thickness), borderMat);
        front.transform.SetParent(_gridRoot.transform);

        // Back
        var back = CreateFlatQuad("Border_B",
            gridCenter + new Vector3(0f, cy, -originOffset.z + thickness * 0.5f),
            new Vector3(totalW + thickness * 2f, height, thickness), borderMat);
        back.transform.SetParent(_gridRoot.transform);

        // Left
        var left = CreateFlatQuad("Border_L",
            gridCenter + new Vector3(originOffset.x - thickness * 0.5f, cy, 0f),
            new Vector3(thickness, height, totalD), borderMat);
        left.transform.SetParent(_gridRoot.transform);

        // Right
        var right = CreateFlatQuad("Border_R",
            gridCenter + new Vector3(-originOffset.x + thickness * 0.5f, cy, 0f),
            new Vector3(thickness, height, totalD), borderMat);
        right.transform.SetParent(_gridRoot.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────
    GameObject CreateFlatQuad(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }

    Material MakeMat(Color c, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.shader.name == "Hidden/InternalErrorShader")
            m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    Material MakeTransparentMat(Color c)
    {
        // Try URP first
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.shader.name == "Hidden/InternalErrorShader")
        {
            m = new Material(Shader.Find("Standard"));
            m.SetFloat("_Mode", 3);         // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
        else
        {
            // URP transparent surface
            m.SetFloat("_Surface", 1);       // 0=Opaque, 1=Transparent
            m.SetFloat("_Blend", 0);         // Alpha
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
