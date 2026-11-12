using Unity.Netcode;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
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

    public Vector3 ObterPosicaoRespawn()
    {
        // Se o ponto foi atribuído no Inspector
        if (pontoDeRespawn != null)
        {
            Debug.Log($"[RespawnManager] Ponto encontrado no Transform: {pontoDeRespawn.position}");
            return pontoDeRespawn.position;
        }

        // Tenta achar um objeto chamado "PontoRespawn" na cena automaticamente
        GameObject pontoObj = GameObject.Find("PontoRespawn");
        if (pontoObj != null)
        {
            Debug.Log($"[RespawnManager] Ponto encontrado via GameObject.Find: {pontoObj.transform.position}");
            return pontoObj.transform.position;
        }

        // Fallback padrão se nada for encontrado
        Debug.LogWarning("[RespawnManager] Nenhum ponto de respawn atribuído! Usando (0, 1, 0).");
        return new Vector3(0f, 1f, 0f);
    }
}