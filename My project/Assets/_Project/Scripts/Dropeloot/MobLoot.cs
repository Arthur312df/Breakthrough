using Unity.Netcode;
using UnityEngine;

public class MobLoot : NetworkBehaviour
{
    [Header("Prefabs de Drop (Devem ter NetworkObject)")]
    [SerializeField] private GameObject prefabOuro;
    [SerializeField] private GameObject prefabEspada;

    [Header("Chances de Drop (0 a 100%)")]
    [Range(0f, 100f)] [SerializeField] private float chanceDropEspada = 30f; // 30% de chance

    public void DroparItens(Vector3 posicaoMorte)
    {
        // O drop de loot deve ser executado exclusivamente pelo Servidor
        if (!IsServer) return;

        Vector3 posicaoDrop = posicaoMorte + Vector3.up * 0.5f;

        // Ouro sempre dropa (ou ajuste conforme sua regra)
        if (prefabOuro != null)
        {
            GameObject ouro = Instantiate(prefabOuro, posicaoDrop, Quaternion.identity);
            if (ouro.TryGetComponent<NetworkObject>(out NetworkObject netOuro))
            {
                netOuro.Spawn();
            }
        }

        // Espada dropa baseada na porcentagem de chance
        if (prefabEspada != null && Random.Range(0f, 100f) <= chanceDropEspada)
        {
            Vector3 posicaoEspada = posicaoDrop + new Vector3(0.5f, 0f, 0f);
            GameObject espada = Instantiate(prefabEspada, posicaoEspada, Quaternion.identity);
            if (espada.TryGetComponent<NetworkObject>(out NetworkObject netEspada))
            {
                netEspada.Spawn();
            }
            Debug.Log("[Loot] O mob dropou uma espada!");
        }
    }
}