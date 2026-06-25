using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class RVOSimulatorManager : MonoBehaviour
{
    public static RVOSimulatorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<RVOSimulatorManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("RVOSimulatorManager");
                    _instance = go.AddComponent<RVOSimulatorManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    private static RVOSimulatorManager _instance;

    [Header("RVO Settings")]
    public float neighborDist = 12f;
    public int maxNeighbors = 8;
    public float timeHorizon = 1.5f;
    public float timeHorizonObst = 1.5f;
    public float safetyMargin = 1.15f; // multiplier for unit collider radius

    private RVO.Simulator _simulator;
    private Dictionary<Unit, int> _unitToAgent = new Dictionary<Unit, int>();
    private Dictionary<int, Unit> _agentToUnit = new Dictionary<int, Unit>();
    private bool _isSimulationActive = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void InitializeSimulation(List<Unit> players, List<Unit> enemies)
    {
        ClearSimulation();

        _simulator = new RVO.Simulator();
        _simulator.SetTimeStep(Time.fixedDeltaTime);
        _simulator.SetNumWorkers(0); // auto-detect threads

        // Add Environment Obstacles
        GameObject envRoot = GameObject.Find("Environment_Models");
        if (envRoot != null)
        {
            BoxCollider[] rockColliders = envRoot.GetComponentsInChildren<BoxCollider>();
            foreach (var col in rockColliders)
            {
                Vector3 extents = col.size * 0.5f;

                // Create the 4 corners of the bounding box on the XZ plane in local space
                Vector3 p1Local = col.center + new Vector3(-extents.x, 0, -extents.z);
                Vector3 p2Local = col.center + new Vector3(extents.x, 0, -extents.z);
                Vector3 p3Local = col.center + new Vector3(extents.x, 0, extents.z);
                Vector3 p4Local = col.center + new Vector3(-extents.x, 0, extents.z);

                // Transform to world space
                Vector3 p1 = col.transform.TransformPoint(p1Local);
                Vector3 p2 = col.transform.TransformPoint(p2Local);
                Vector3 p3 = col.transform.TransformPoint(p3Local);
                Vector3 p4 = col.transform.TransformPoint(p4Local);

                // RVO obstacles must be counter-clockwise
                IList<float2> obstacleVertices = new List<float2>
                {
                    new float2(p1.x, p1.z),
                    new float2(p2.x, p2.z),
                    new float2(p3.x, p3.z),
                    new float2(p4.x, p4.z)
                };

                _simulator.AddObstacle(obstacleVertices);
            }
            Debug.Log($"[RVO Manager] Added {rockColliders.Length} environment obstacles.");
        }

        foreach (var unit in players)
        {
            AddUnitAgent(unit);
        }
        foreach (var unit in enemies)
        {
            AddUnitAgent(unit);
        }

        _isSimulationActive = true;
        Debug.Log($"[RVO Manager] Simulation initialized with {_unitToAgent.Count} agents.");
    }

    private void AddUnitAgent(Unit unit)
    {
        if (unit == null || unit.state == UnitState.Dead) return;

        CapsuleCollider col = unit.GetComponent<CapsuleCollider>();
        float radius = col != null ? col.radius : 0.4f;

        float2 pos = new float2(unit.transform.position.x, unit.transform.position.z);

        // Setup agent defaults before adding
        _simulator.SetAgentDefaults(
            neighborDist,
            maxNeighbors,
            timeHorizon,
            timeHorizonObst,
            radius * safetyMargin,
            unit.speed,
            float2.zero
        );

        int agentId = _simulator.AddAgent(pos);
        _unitToAgent[unit] = agentId;
        _agentToUnit[agentId] = unit;
    }

    public void RemoveAgent(Unit unit)
    {
        if (unit == null) return;
        if (_unitToAgent.TryGetValue(unit, out int agentId))
        {
            if (_simulator != null)
            {
                _simulator.EnsureCompleted();
                _simulator.RemoveAgent(agentId);
            }
            _unitToAgent.Remove(unit);
            _agentToUnit.Remove(agentId);
            Debug.Log($"[RVO Manager] Agent removed for unit: {unit.name}");
        }
    }

    private void FixedUpdate()
    {
        if (!_isSimulationActive || _simulator == null) return;

        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Battle)
        {
            ClearSimulation();
            return;
        }

        // 1. Sync positions and set preferred velocities
        List<Unit> toRemove = new List<Unit>();
        foreach (var pair in _unitToAgent)
        {
            Unit unit = pair.Key;
            int agentId = pair.Value;

            if (unit == null || unit.state == UnitState.Dead)
            {
                toRemove.Add(unit);
                continue;
            }

            // Sync current position (XZ plane)
            _simulator.SetAgentPosition(agentId, new float2(unit.transform.position.x, unit.transform.position.z));

            // Sync dynamic agent radius (updates RVO avoidance radius in real time)
            CapsuleCollider col = unit.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                _simulator.SetAgentRadius(agentId, col.radius * safetyMargin);
            }

            // Compute preferred velocity
            float2 prefVelocity = float2.zero;
            if (unit.state == UnitState.Moving)
            {
                Vector3 targetPos = unit.GetMoveTargetPosition();
                Vector3 dir = targetPos - unit.transform.position;
                dir.y = 0;
                float dist = dir.magnitude;
                if (dist > 0.05f)
                {
                    Vector3 desiredDir = dir.normalized;
                    
                    // Check if blocked by teammates and apply bypass steering
                    Vector3 steerDir = desiredDir;
                    Vector3 avoidanceSteer;
                    if (IsBlockedByTeammates(unit, desiredDir, out avoidanceSteer))
                    {
                        steerDir = (desiredDir + avoidanceSteer).normalized;
                    }

                    prefVelocity = new float2(steerDir.x * unit.speed, steerDir.z * unit.speed);
                }
            }
            
            _simulator.SetAgentPrefVelocity(agentId, prefVelocity);
        }

        // Remove dead agents
        foreach (var unit in toRemove)
        {
            RemoveAgent(unit);
        }

        if (_unitToAgent.Count == 0) return;

        // 2. Perform simulation step
        _simulator.DoStep();

        // Ensure step completes before accessing results
        _simulator.EnsureCompleted();

        // 3. Apply RVO computed velocities to units
        foreach (var pair in _unitToAgent)
        {
            Unit unit = pair.Key;
            int agentId = pair.Value;

            float2 velocity2D = _simulator.GetAgentVelocity(agentId);
            Vector3 velocity3D = new Vector3(velocity2D.x, 0, velocity2D.y);

            unit.SetRVOVelocity(velocity3D);
        }
    }

    private bool IsBlockedByTeammates(Unit unit, Vector3 desiredDir, out Vector3 avoidanceSteer)
    {
        avoidanceSteer = Vector3.zero;
        if (GameManager.Instance == null) return false;

        CapsuleCollider col = unit.GetComponent<CapsuleCollider>();
        float radius = col != null ? col.radius : 0.4f;

        List<Unit> teammates = unit.isPlayer ? GameManager.Instance.playerUnits : GameManager.Instance.enemyUnits;
        float closestDist = float.MaxValue;
        Unit closestBlocker = null;

        // Use wider cone (smaller dot threshold) if already steering to prevent chattering
        float dotThreshold = unit.isSteeringAroundTeammate ? 0.15f : 0.45f;

        foreach (var teammate in teammates)
        {
            if (teammate == unit || teammate.state == UnitState.Dead) continue;

            Vector3 toTeammate = teammate.transform.position - unit.transform.position;
            toTeammate.y = 0;
            float dist = toTeammate.magnitude;

            CapsuleCollider teamCol = teammate.GetComponent<CapsuleCollider>();
            float teamRadius = teamCol != null ? teamCol.radius : 0.4f;
            float combinedRadius = radius + teamRadius;

            // Hysteresis for check distance: look ahead further if already steering
            float checkDistMultiplier = unit.isSteeringAroundTeammate ? 1.6f : 1.35f;
            float checkDist = combinedRadius * checkDistMultiplier;

            if (dist < checkDist)
            {
                float dot = Vector3.Dot(desiredDir, toTeammate.normalized);
                if (dot > dotThreshold)
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestBlocker = teammate;
                    }
                }
            }
        }

        if (closestBlocker != null)
        {
            unit.isSteeringAroundTeammate = true;

            // Perpendicular vector
            Vector3 perpendicular = new Vector3(-desiredDir.z, 0, desiredDir.x);
            
            // Distribute agents left/right using their unique ID
            float sign = (unit.GetHashCode() % 2 == 0) ? 1.0f : -1.0f;
            
            // Calculate checkDist for the closest blocker to scale steerWeight
            CapsuleCollider closestTeamCol = closestBlocker.GetComponent<CapsuleCollider>();
            float closestTeamRadius = closestTeamCol != null ? closestTeamCol.radius : 0.4f;
            float closestCheckDist = (radius + closestTeamRadius) * (unit.isSteeringAroundTeammate ? 1.6f : 1.35f);

            // Steer stronger when closer
            float steerWeight = Mathf.Lerp(1.8f, 0.6f, closestDist / closestCheckDist);
            avoidanceSteer = perpendicular * sign * steerWeight;
            
            return true;
        }

        unit.isSteeringAroundTeammate = false;
        return false;
    }

    public void ClearSimulation()
    {
        _isSimulationActive = false;
        if (_simulator != null)
        {
            _simulator.EnsureCompleted();
            _simulator.Clear();
            _simulator.Dispose();
            _simulator = null;
        }
        _unitToAgent.Clear();
        _agentToUnit.Clear();
        Debug.Log("[RVO Manager] Simulation cleared.");
    }

    private void OnDestroy()
    {
        ClearSimulation();
    }
}
