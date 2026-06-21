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
                    CapsuleCollider col = unit.GetComponent<CapsuleCollider>();
                    float radius = col != null ? col.radius : 0.4f;
                    float checkDist = radius * 2.8f;
                    
                    Vector3 steerDir = desiredDir;
                    Vector3 avoidanceSteer;
                    if (IsBlockedByTeammates(unit, desiredDir, checkDist, out avoidanceSteer))
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

    private bool IsBlockedByTeammates(Unit unit, Vector3 desiredDir, float checkDist, out Vector3 avoidanceSteer)
    {
        avoidanceSteer = Vector3.zero;
        if (GameManager.Instance == null) return false;

        List<Unit> teammates = unit.isPlayer ? GameManager.Instance.playerUnits : GameManager.Instance.enemyUnits;
        float closestDist = float.MaxValue;
        Unit closestBlocker = null;

        foreach (var teammate in teammates)
        {
            if (teammate == unit || teammate.state == UnitState.Dead) continue;

            Vector3 toTeammate = teammate.transform.position - unit.transform.position;
            toTeammate.y = 0;
            float dist = toTeammate.magnitude;

            if (dist < checkDist)
            {
                float dot = Vector3.Dot(desiredDir, toTeammate.normalized);
                if (dot > 0.4f) // within ~66 degrees in front
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
            // Perpendicular vector
            Vector3 perpendicular = new Vector3(-desiredDir.z, 0, desiredDir.x);
            
            // Distribute agents left/right using their unique ID
            float sign = (unit.GetHashCode() % 2 == 0) ? 1.0f : -1.0f;
            
            // Steer stronger when closer
            float steerWeight = Mathf.Lerp(1.8f, 0.6f, closestDist / checkDist);
            avoidanceSteer = perpendicular * sign * steerWeight;
            
            return true;
        }

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
