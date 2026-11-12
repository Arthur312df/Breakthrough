using Unity.Netcode;
using UnityEngine;

public class MobLoot : NetworkBehaviour
{
    [Header("Prefabs de Drop (Devem ter NetworkObject)")]
    [SerializeField] private GameObject prefabOuro;
    [SerializeField] private GameObject prefabEspada;

    [Header("Chances de Drop (0 a 100%)")]
    [Range(0, 100)] [SerializeField] private int chanceDropEspada = 30;

    private readonly RaycastHit[] bufferChao = new RaycastHit[10];

    public void DroparItens(Vector3 posicaoMorte)
    {
        if (!IsServer) return;

        // Descobre onde está o chão embaixo do Mob para não spawnar no alto
        float alturaChao = ObterAlturaDoChao(posicaoMorte);
        Vector3 posicaoDrop = new Vector3(posicaoMorte.x, alturaChao + 1.0f, posicaoMorte.z);

        // Drop de Ouro
        if (prefabOuro != null)
        {
            GameObject ouro = Instantiate(prefabOuro, posicaoDrop, Quaternion.identity);
            if (ouro.TryGetComponent<NetworkObject>(out NetworkObject netOuro))
            {
                netOuro.Spawn(true);
            }
        }

        // Drop de Espada baseado em porcentagem inteira
        int sorteio = Random.Range(0, 100);
        if (prefabEspada != null && sorteio < chanceDropEspada)
        {
            Vector3 posicaoEspada = posicaoDrop + new Vector3(0.6f, 0f, 0f);
            GameObject espada = Instantiate(prefabEspada, posicaoEspada, Quaternion.identity);
            if (espada.TryGetComponent<NetworkObject>(out NetworkObject netEspada))
            {
                netEspada.Spawn(true);
            }
            Debug.Log("[Loot] O mob dropou uma espada!");
        }
    }

    private float ObterAlturaDoChao(Vector3 origem)
    {
        Vector3 topoRaio = origem + Vector3.up * 3f;
        int hits = Physics.RaycastNonAlloc(topoRaio, Vector3.down, bufferChao, 20f, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits; i++)
        {
            Collider col = bufferChao[i].collider;
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<NetworkHealth>() != null) continue;
            if (col.GetComponent<ItemCoativel>() != null) continue;
            if (col.GetComponent<ProjetilCombate>() != null) continue;

            return bufferChao[i].point.y;
        }

        // Fallback: usa a base da escala do Mob caso não ache colisor de chão
        return origem.y - (transform.localScale.y * 0.5f);
    }
}