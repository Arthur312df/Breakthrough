using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MobSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject mobPrefab;
    [SerializeField] private int quantidadeMaxima = 3;
    [SerializeField] private float raio = 6f;
    [SerializeField] private float tempoRespawn = 5f; // Tempo em segundos para o mob renascer

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Spawner] Não existe NetworkManager na cena.");
            return;
        }

        // Só dispara quando o servidor (ou Host) liga
        NetworkManager.Singleton.OnServerStarted += AoIniciarServidor;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= AoIniciarServidor;
    }
    private void AoIniciarServidor()
    {
        if (mobPrefab == null)
        {
            Debug.LogError("[Spawner] O prefab do Mob não foi atribuído.");
            return;
        }

        for (int i = 0; i < quantidadeMaxima; i++)
        {
            CriarMob();
        }
    }

    private void CriarMob()
    {
        float alturaBase = 1.0f; // Ajuste conforme a base do seu mob

        Vector3 posicao = new Vector3(
            Random.Range(-raio, raio),
            alturaBase, 
            Random.Range(3f, raio + 3f));

        NetworkObject mobInstance = Instantiate(mobPrefab, posicao, Quaternion.identity);
        mobInstance.Spawn(); // cria o inimigo para todos os jogadores

        // Se o mob tiver o componente NetworkHealth, inscrevemos no evento de morte
        if (mobInstance.TryGetComponent<NetworkHealth>(out NetworkHealth health))
        {
            health.OnMobMorreu += HandleMobMorreu;
        }

        Debug.Log($"[Spawner] Mob criado em {posicao}");
    }

    private void HandleMobMorreu(NetworkHealth mobMorto)
    {
        // Remove a assinatura para evitar vazamento de memória
        mobMorto.OnMobMorreu -= HandleMobMorreu;

        // Inicia a contagem para nascer um novo mob no lugar
        StartCoroutine(EsperaERenascer());
    }

    private IEnumerator EsperaERenascer()
    {
        yield return new WaitForSeconds(tempoRespawn);
        
        // Garante que o servidor ainda está ativo antes de spawnar
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            CriarMob();
        }
    }
}