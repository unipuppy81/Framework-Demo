using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.NetCode.Objects;

namespace MultiplayerFramework.Runtime.Netcode.StateSync
{
    /// <summary>
    /// NetworkObject 전체를 한 번에 동기화하지 말고, 필요한 부분만 SyncBehaviour로 분리
    /// </summary>
    public interface INetworkSyncBehaviour
    {
        NetworkObject NetworkObject { get; }

        void WriteState(ref StateMessage message);
        void ReadState(in StateMessage message);

        void ResetState();
    }
}