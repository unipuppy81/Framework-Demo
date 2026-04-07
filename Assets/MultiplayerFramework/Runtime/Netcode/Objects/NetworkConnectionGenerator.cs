namespace MultiplayerFramework.Runtime.NetCode.Objects
{
    public sealed class NetworkConnectionGenerator
    {
        private int _nextId = 1; 

        public int Create()
        {
            return _nextId++;
        }

        public void Reset(int startId = 1)
        {
            _nextId = startId;
        }
    }
}
  