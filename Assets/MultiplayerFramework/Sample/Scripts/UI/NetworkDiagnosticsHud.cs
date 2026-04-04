using MultiplayerFramework.Runtime.Core.Diagnostics;
using TMPro;
using UnityEngine;

namespace MultiplayerFramework.Samples.Scripts.UI
{
    public class NetworkDiagnosticsHud : MonoBehaviour
    {
        public Host _host;
        public Client _client;

        [SerializeField] private TextMeshProUGUI _tickText;
        [SerializeField] private TextMeshProUGUI _rttText;
        [SerializeField] private TextMeshProUGUI _packetText;
        [SerializeField] private TextMeshProUGUI _spawnText;
        [SerializeField] private TextMeshProUGUI _messageText;

        private RuntimeDiagnosticsCollector _diagnostics;

        private void Update()
        {
            RuntimeDiagnosticsCollector diagnostics = null;

            if (_client != null)
                diagnostics = _client.Diagnostics;
            else if (_host != null)
                diagnostics = _host.Diagnostics;

            if (diagnostics == null)
                return;

            _tickText.text = $"Tick: {diagnostics.CurrentTick}";
            _rttText.text = $"RTT: {diagnostics.RttMs:F1} ms";
            _packetText.text = $"Packets Send / Received : {diagnostics.SentPacketCountPerSecond} / {diagnostics.ReceivedPacketCountPerSecond}";
            _spawnText.text = $"Spawn Count: {diagnostics.SpawnCount}";
            _messageText.text = $"Diag: {diagnostics.LastMessage}";
        }

        public void Bind(RuntimeDiagnosticsCollector diagnostics)
        {
            _diagnostics = diagnostics;
        }
    }
}
