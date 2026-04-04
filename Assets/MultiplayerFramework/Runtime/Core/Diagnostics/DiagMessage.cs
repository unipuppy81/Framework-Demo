using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerFramework.Runtime.Core.Diagnostics
{
    [System.Serializable]
    public class PingMessage
    {
        public int Sequence;
        public float SentTime;
    }

    [System.Serializable]
    public class PongMessage
    {
        public int Sequence;
        public float SentTime;
    }

}