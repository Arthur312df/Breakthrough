using Unity.Netcode;
using UnityEngine;

public class PersistirRede : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}