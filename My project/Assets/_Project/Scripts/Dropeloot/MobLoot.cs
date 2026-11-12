using Unity.Netcode;
using UnityEngine;

public class MobLoot : NetworkBehaviour
{
    [Header("Prefabs de Drop (Devem ter NetworkObject)")]
    [SerializeField] private GameObject prefabOuro;
    [SerializeField] private GameObject prefabEspada;

    [Header("Chances de Drop (0 a 100%)")]
    [Range(0, 100)] [SerializeField] private int chanceDropEspada = 30; // 30% de chance

    public void DroparItens(Vector3 posicaoMorte)
    {
        // O drop de loot deve ser executado exclusivamente pelo Servidor
        if (!IsServer) return;

        Vector3 posicaoDrop = posicaoMorte + Vector3.up * 0.5f;

        // Drop de Ouro (Ex: ID do item Ouro = 1)
        if (prefabOuro != null)
        {
            GameObject ouro = Instantiate(prefabOuro, posicaoDrop, Quaternion.identity);
            if (ouro.TryGetComponent<NetworkObject>(out NetworkObject netOuro))
            {
                netOuro.Spawn(true);
            }
        }

        // Drop de Espada baseado em porcentagem inteira (Ex: ID do item Espada = 2)
        int sorteio = Random.Range(0, 100);
        if (prefabEspada != null && sorteio < chanceDropEspada)
        {
            Vector3 posicaoEspada = posicaoDrop + new Vector3(0.5f, 0f, 0f);
            GameObject espada = Instantiate(prefabEspada, posicaoEspada, Quaternion.identity);
            if (espada.TryGetComponent<NetworkObject>(out NetworkObject netEspada))
            {
                netEspada.Spawn(true);
            }
            Debug.Log("[Loot] O mob dropou uma espada!");
        }
    }
}