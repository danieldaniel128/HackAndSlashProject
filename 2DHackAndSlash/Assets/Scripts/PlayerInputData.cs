using Fusion;

public struct PlayerInputData : INetworkInput
{
    public float MoveX;
    public NetworkBool JumpPressed;
    public NetworkBool JumpHeld;
    public NetworkBool DashPressed;
}