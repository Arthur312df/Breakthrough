using Unity.Netcode;
using UnityEngine;

public class GerenciadorSpawn : MonoBehaviour
{
    [SerializeField] private GameObject prefabPlayer;
    [SerializeField] private Transform pontoRespawn;
    [SerializeField] private float espacoEntrePlayers = 1.5f;

    public static GerenciadorSpawn Instance { get; private set; }

    private bool inscrito;

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
        NetworkManager rede = NetworkManager.Singleton;
        if (rede == null || !rede.IsServer) return;

        rede.OnClientConnectedCallback += SpawnarPlayerRede;
        inscrito = true;

        // Quem já estava conectado antes da cena carregar (ex: o próprio Host)
        foreach (ulong clientId in rede.ConnectedClientsIds)
        {
            SpawnarPlayerRede(clientId);
        }
    }

    private void OnDestroy()
    {
        if (inscrito && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnarPlayerRede;
        }

        if (Instance == this) Instance = null;
    }

    public void SpawnarPlayerRede(ulong clientId)
    {
        NetworkManager rede = NetworkManager.Singleton;

        if (prefabPlayer == null)
        {
            Debug.LogError("[Spawner] O prefab do Player não foi atribuído no Inspector!");
            return;
        }

        // Se este cliente já tem um player, não cria outro
        if (rede.ConnectedClients.TryGetValue(clientId, out NetworkClient cliente) && cliente.PlayerObject != null)
        {
            return;
        }

        // Sem ponto de spawn configurado: nasce em (0, 1, 0), um pouco acima do chão
        Vector3 posicaoBase = pontoRespawn != null ? pontoRespawn.position : new Vector3(0f, 1f, 0f);

        // Cada jogador nasce um pouco ao lado do outro para não ficarem empilhados
        Vector3 deslocamento = new Vector3((int)(clientId % 4) * espacoEntrePlayers, 0f, 0f);

        GameObject instanciaPlayer = Instantiate(prefabPlayer, posicaoBase + deslocamento, Quaternion.identity);
        instanciaPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        Debug.Log($"[Spawner] Player instanciado para o Client ID: {clientId}");
    }
}
