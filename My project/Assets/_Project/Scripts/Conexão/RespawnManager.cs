using UnityEngine;

// Não precisa ser NetworkBehaviour: ele só informa uma posição.
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [SerializeField] private Transform pontoDeRespawn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Vector3 ObterPosicaoRespawn()
    {
        // Procura o objeto "PontoRespawn" uma única vez e guarda o resultado
        if (pontoDeRespawn == null)
        {
            GameObject pontoObj = GameObject.Find("PontoRespawn");
            if (pontoObj != null) pontoDeRespawn = pontoObj.transform;
        }

        if (pontoDeRespawn != null)
        {
            return pontoDeRespawn.position;
        }

        Debug.LogWarning("[RespawnManager] Nenhum ponto de respawn atribuído! Usando (0, 1, 0).");
        return new Vector3(0f, 1f, 0f);
    }
}
