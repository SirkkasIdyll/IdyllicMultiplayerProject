using Godot;
using IdyllicMultiplayerProject.Temperance;

namespace IdyllicMultiplayerProject.Shared.Systems.Movement;

[GlobalClass, PeerSynchronized]
public partial class MovementComponent : Component
{
    [SynchronizedField(SynchronizeOnSpawn = false, ReplicationMode = SceneReplicationConfig.ReplicationMode.Never)]
    public float MovementSpeed = 2f;
}