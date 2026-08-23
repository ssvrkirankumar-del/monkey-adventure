namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Identifies the semantic role of an AI-generated or scene-placed level marker.
    /// </summary>
    public enum LevelMarkerType
    {
        Start,
        Finish,
        Checkpoint,
        EnemySpawn,
        CollectibleSpawn,
        ObstacleSpawn,
        EnvironmentObject,
        Custom
    }
}
