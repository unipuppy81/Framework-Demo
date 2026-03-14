namespace MultiplayerFramework.Runtime.Netcode.Messages
{
    public struct InputMessage
    {
        public int Tick;
        public int PlayerId;
        public float MoveX;
        public float MoveY;
        public bool DashPressed;
        public bool AttackPressed;
    }
}