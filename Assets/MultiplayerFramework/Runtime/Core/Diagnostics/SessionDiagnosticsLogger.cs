using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerFramework.Runtime.Core.Diagnostics
{
    /// <summary>
    /// 세션/Transport 관련 이벤트를 순서대로 보관하고  Console 출력
    /// 
    /// - connect / join / leave / spawn / disconnect 순서를 남긴다.
    /// - 문제 재현 시 어느 단계에서 꼬였는지 추적 가능하게 한다.
    /// </summary>
    public class SessionDiagnosticsLogger
    {
        private readonly List<string> _logs = new();
        public IReadOnlyList<string> Logs => _logs;

        public void Log(string message)
        {
            _logs.Add(message);
            Debug.Log(message);
        }

        public void Clear()
        {
            _logs.Clear();
        }
    }

}


