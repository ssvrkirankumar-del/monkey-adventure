using UnityEngine;

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Lightweight component attached to level markers and spawn points
    /// for editor visualization, gizmo rendering, and runtime discovery.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/AI Level Builder/Level Marker")]
    [DisallowMultipleComponent]
    public class LevelMarker : MonoBehaviour
    {
        [Header("Marker Metadata")]
        [SerializeField] private LevelMarkerType markerType = LevelMarkerType.EnvironmentObject;
        [SerializeField] private string markerLabel = "";
        [SerializeField] private int markerIndex = 0;
        [SerializeField] private string sectionName = "";

        [Header("Gizmo Settings")]
        [SerializeField] private float gizmoRadius = 0.5f;
        [SerializeField] private bool showGizmo = true;

        public LevelMarkerType MarkerType
        {
            get => markerType;
            set => markerType = value;
        }

        public string MarkerLabel
        {
            get => markerLabel;
            set => markerLabel = value;
        }

        public int MarkerIndex
        {
            get => markerIndex;
            set => markerIndex = value;
        }

        public string SectionName
        {
            get => sectionName;
            set => sectionName = value;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = GetColorForType(markerType);
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            Gizmos.DrawRay(transform.position, transform.forward * (gizmoRadius * 1.5f));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GetColorForType(markerType);
            Gizmos.DrawSphere(transform.position, gizmoRadius * 0.7f);
        }

        private Color GetColorForType(LevelMarkerType type)
        {
            switch (type)
            {
                case LevelMarkerType.Start:
                    return new Color(0.2f, 1f, 0.2f, 0.9f);
                case LevelMarkerType.Finish:
                    return new Color(1f, 0.85f, 0f, 0.9f);
                case LevelMarkerType.Checkpoint:
                    return new Color(0f, 0.8f, 1f, 0.9f);
                case LevelMarkerType.EnemySpawn:
                    return new Color(1f, 0.2f, 0.2f, 0.9f);
                case LevelMarkerType.CollectibleSpawn:
                    return new Color(1f, 0.95f, 0.1f, 0.9f);
                case LevelMarkerType.ObstacleSpawn:
                    return new Color(1f, 0.5f, 0f, 0.9f);
                case LevelMarkerType.EnvironmentObject:
                    return new Color(0.3f, 0.8f, 0.4f, 0.9f);
                default:
                    return Color.white;
            }
        }
    }
}
