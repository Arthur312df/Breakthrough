using Unity.Netcode;
using UnityEngine;

public class GerenciadorSpawn : MonoBehaviour
{
    [SerializeField] private GameObject prefabPlayer;
    [SerializeField] private Transform pontoRespawn;

    public static GerenciadorSpawn Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Se já estiver rodando como servidor/host (ex: entrou na cena pelo menu), gerencia o spawn imediatamente
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnarPlayerRede;
            
            // Caso o host já tenha se conectado antes da cena carregar
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnarPlayerRede(clientId);
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnarPlayerRede;
        }
    }

    public void SpawnarPlayerRede(ulong clientId)
    {
        // Verifica se já não existe um player associado a este cliente para evitar duplicidade
        foreach (var playerObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (playerObj.IsPlayerObject && playerontalOwner(playerObj, clientId))
            {
                return;
            }
        }

        Vector3 posicao = pontoRespawn != null ? pontoRespawn.position : Vector3.zero;
        GameObject instanciaPlayer = Instantiate(prefabPlayer, posicao, Quaternion.identity);
        
        // Faz o spawn oficial na rede atrelando ao ID do cliente
        instanciaPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        Debug.Log($"[Spawner] Player instanciado com sucesso para o Client ID: {clientId}");
    }

    private bool playerontalOwner(NetworkObject netObj, ulong clientId)
    {
        return netObj.OwnerClientId == clientId;
    }
}