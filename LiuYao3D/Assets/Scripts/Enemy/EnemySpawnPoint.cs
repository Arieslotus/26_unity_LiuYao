/// <summary>
/// 实现功能：标记敌人出生点，并根据统一配置提供环形错位候选点与地面合法性检测。
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("出生点")]
    [SerializeField] private string spawnPointId;
    [SerializeField] private EnemySpawnPointConfigSO config;

    private readonly List<SpawnCandidate> cachedCandidates = new List<SpawnCandidate>();
    private bool candidatesDirty = true;

    public string SpawnPointId => spawnPointId;
    public Vector3 Center => transform.position;
    public QueryTriggerInteraction TriggerInteraction =>
        Config != null && Config.includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

    private EnemySpawnPointConfigSO Config => config;

    public IReadOnlyList<SpawnCandidate> GetCandidates(bool includeCenter)
    {
        RebuildCandidatesIfNeeded();

        if (includeCenter)
            return cachedCandidates;

        List<SpawnCandidate> result = new List<SpawnCandidate>();
        for (int i = 0; i < cachedCandidates.Count; i++)
        {
            if (cachedCandidates[i].RingIndex > 0)
            {
                result.Add(cachedCandidates[i]);
            }
        }

        return result;
    }

    public IReadOnlyList<SpawnCandidate> GetCandidates(bool includeCenter, float radiusMultiplier)
    {
        if (radiusMultiplier <= 1f)
            return GetCandidates(includeCenter);

        List<SpawnCandidate> result = new List<SpawnCandidate>();
        BuildCandidates(result, Mathf.Max(1f, radiusMultiplier));

        if (includeCenter)
            return result;

        for (int i = result.Count - 1; i >= 0; i--)
        {
            if (result[i].RingIndex <= 0)
            {
                result.RemoveAt(i);
            }
        }

        return result;
    }

    public float GetSpawnRadius(float radiusMultiplier = 1f)
    {
        float baseRadius = Config != null ? Mathf.Max(0.1f, Config.spawnRadius) : 3f;
        return baseRadius * Mathf.Max(1f, radiusMultiplier);
    }

    public bool IsCircleOnGround(Vector3 center, float radius)
    {
        if (Config == null || Config.groundMask.value == 0)
            return true;

        if (!HasGround(center))
            return false;

        int segments = Mathf.Max(6, Config.groundProbeSegments);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (!HasGround(center + offset))
                return false;
        }

        return true;
    }

    public bool IsBlockingCollider(Collider collider)
    {
        if (collider == null || !collider.enabled)
            return false;

        if ((Config == null || !Config.includeTriggerColliders) && collider.isTrigger)
            return false;

        int colliderLayerMask = 1 << collider.gameObject.layer;
        if (Config != null && (Config.groundMask.value & colliderLayerMask) != 0)
            return false;

        return true;
    }

    private bool HasGround(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * Config.groundProbeHeight;
        float distance = Config.groundProbeHeight + Config.groundProbeDistance;
        return Physics.Raycast(origin, Vector3.down, distance, Config.groundMask, QueryTriggerInteraction.Ignore);
    }

    private void RebuildCandidatesIfNeeded()
    {
        if (!candidatesDirty)
            return;

        cachedCandidates.Clear();
        BuildCandidates(cachedCandidates, 1f);

        candidatesDirty = false;
    }

    private void BuildCandidates(List<SpawnCandidate> candidates, float radiusMultiplier)
    {
        candidates.Clear();
        candidates.Add(new SpawnCandidate(transform.position, 0));

        float spawnRadius = GetSpawnRadius(radiusMultiplier);
        float pointSpacing = Config != null ? Mathf.Max(0.1f, Config.pointSpacing) : 1f;
        int ringCount = Mathf.FloorToInt(spawnRadius / pointSpacing);

        for (int ring = 1; ring <= ringCount; ring++)
        {
            float radius = ring * pointSpacing;
            int pointCount = Mathf.Max(6 * ring, Mathf.CeilToInt((Mathf.PI * 2f * radius) / pointSpacing));
            float offset = ring * 17f * Mathf.Deg2Rad;

            for (int i = 0; i < pointCount; i++)
            {
                float angle = Mathf.PI * 2f * i / pointCount + offset;
                Vector3 localOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                candidates.Add(new SpawnCandidate(transform.position + localOffset, ring));
            }
        }
    }

    private void OnValidate()
    {
        candidatesDirty = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (Config != null && !Config.drawGizmos)
            return;

        RebuildCandidatesIfNeeded();

        float spawnRadius = Config != null ? Config.spawnRadius : 3f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        for (int i = 0; i < cachedCandidates.Count; i++)
        {
            SpawnCandidate candidate = cachedCandidates[i];
            Gizmos.color = candidate.RingIndex == 0 ? Color.green : Color.yellow;
            Gizmos.DrawSphere(candidate.Position, 0.06f);
        }
    }
}

public readonly struct SpawnCandidate
{
    public readonly Vector3 Position;
    public readonly int RingIndex;

    public SpawnCandidate(Vector3 position, int ringIndex)
    {
        Position = position;
        RingIndex = ringIndex;
    }
}
