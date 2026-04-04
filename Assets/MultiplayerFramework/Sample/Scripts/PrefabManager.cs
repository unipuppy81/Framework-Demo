using MultiplayerFramework.Runtime.NetCode.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;


namespace MultiplayerFramework.Samples
{
    public class PrefabManager : MonoBehaviour
    {
        public GameObject HostPrefab;
        public Transform HostSpawnPosition;
        public GameObject HostGameObject;
        
        
        public GameObject ClientPrefab;
        public Transform ClientSpawnPosition;
        public GameObject ClientGameObject;


        public GameObject CreateHost()
        {
            HostGameObject = Instantiate(HostPrefab, HostSpawnPosition.position, Quaternion.identity);
            return HostGameObject;
        }

        public GameObject CreateClient()
        {
            ClientGameObject = Instantiate(ClientPrefab, ClientSpawnPosition.position, Quaternion.identity);
            return ClientGameObject;
        }

        public NetworkObject GetHostNO()
        {
            return HostGameObject.GetComponent<NetworkObject>();
        }

        public NetworkObject GetClientNO()
        {
            return ClientGameObject.GetComponent<NetworkObject>();
        }
    }

}
