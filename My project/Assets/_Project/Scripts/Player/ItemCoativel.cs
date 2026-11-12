using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ItemCoativel : NetworkBehaviour
{
    [SerializeField] private int quantidadeOuro = 10;

    [Header("Queda até o Chão")]
    [SerializeField] private float alturaPousoNoChao = 0.3f;
    [SerializeField] private float velocidadeQueda = 8f;

    private bool jaColetado = false;
    private readonly RaycastHit[] bufferChao = new RaycastHit[10];

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(CairAteOChaoRoutine());
    }

    private IEnumerator CairAteOChaoRoutine()
    {
        Vector3 origemRaio = transform.position + Vector3.up * 3f;
        int hits = Physics.RaycastNonAlloc(origemRaio, Vector3.down, bufferChao, 20f, ~0, QueryTriggerInteraction.Ignore);

        float alturaChaoY = transform.position.y;
        bool achouChao = false;

        for (int i = 0; i < hits; i++)
        {
            Collider col = bufferChao[i].collider;
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<NetworkHealth>() != null) continue;
            if (col.GetComponent<ItemCoativel>() != null) continue;
            if (col.GetComponent<ProjetilCombate>() != null) continue;

            alturaChaoY = bufferChao[i].point.y;
            achouChao = true;
            break;
        }

        if (achouChao)
        {
            Vector3 posicaoAlvo = new Vector3(transform.position.x, alturaChaoY + alturaPousoNoChao, transform.position.z);

            while (transform.position.y > posicaoAlvo.y + 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, posicaoAlvo, velocidadeQueda * Time.deltaTime);
                yield return null;
            }

            transform.position = posicaoAlvo;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (jaColetado) return;

        // Se o jogador local encostou no item, avisa o servidor para coletar e dar Despawn
        if (other.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsOwner)
        {
            if (other.TryGetComponent<PlayerInventory>(out var inventario))
            {
                jaColetado = true;
                inventario.AdicionarOuroServerRpc(quantidadeOuro);
                Debug.Log($"[Loot] Player ID {netObj.OwnerClientId} coletou {quantidadeOuro} de ouro!");
                
                SolicitarDespawnItemServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SolicitarDespawnItemServerRpc()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}