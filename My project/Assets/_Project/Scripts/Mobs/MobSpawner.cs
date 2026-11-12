using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    [Header("Configurações do Spawner")]
    [SerializeField] private GameObject mobPrefab;
    [SerializeField] private int quantidadeMaxima = 3;
    [SerializeField] private float raioDeSpawn = 8f;
    [SerializeField] private float tempoDeRespawn = 5f;

    private bool spawnerIniciado = false;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += AoIniciarServidor;

            // Se o servidor já foi iniciado pelo Menu antes deste Start rodar, cria os mobs imediatamente!
            if (NetworkManager.Singleton.IsServer)
            {
                AoIniciarServidor();
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= AoIniciarServidor;
        }
    }

    private void AoIniciarServidor()
    {
        // Trava para garantir que não vai criar mobs duplicados
        if (spawnerIniciado) return;
        spawnerIniciado = true;

        for (int i = 0; i < quantidadeMaxima; i++)
        {
            CriarMob();
        }
    }

    private void CriarMob()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (mobPrefab == null)
        {
            Debug.LogError("[Spawner] Mob Prefab não foi atribuído no Inspector do MobSpawner!");
            return;
        }

        Vector2 pontoAleatorio = Random.insideUnitCircle * raioDeSpawn;
        Vector3 posicaoSpawn = transform.position + new Vector3(pontoAleatorio.x, 1f, pontoAleatorio.y);

        GameObject novoMob = Instantiate(mobPrefab, posicaoSpawn, Quaternion.identity);

        if (novoMob.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn(true);
        }

        if (novoMob.TryGetComponent<NetworkHealth>(out var health))
        {
            health.OnMobMorreu += AoMobMorrer;
        }

        Debug.Log($"[Spawner] Mob criado em {posicaoSpawn}");
    }

    private void AoMobMorrer(NetworkHealth mobMorto)
    {
        mobMorto.OnMobMorreu -= AoMobMorrer;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RespawnarMobCoroutine());
        }
    }

    private IEnumerator RespawnarMobCoroutine()
    {
        yield return new WaitForSeconds(tempoDeRespawn);
        CriarMob();
    }
}