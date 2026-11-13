using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ItemCoativel : NetworkBehaviour
{
    [SerializeField] private int quantidadeOuro = 10;

    [Header("Coleta")]
    [Tooltip("Distância máxima (medida no servidor) entre o player e o item para a coleta valer")]
    [SerializeField] private float distanciaMaximaColeta = 4f;

    [Header("Queda até o Chão")]
    [SerializeField] private float alturaPousoNoChao = 0.3f;
    [SerializeField] private float velocidadeQueda = 8f;

    private bool coletadoNoServidor;
    private float proximoPedido;
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
        float menorDistancia = float.MaxValue;

        // O Raycast devolve os resultados em ordem qualquer: escolhe o chão MAIS PRÓXIMO
        for (int i = 0; i < hits; i++)
        {
            Collider col = bufferChao[i].collider;
            if (col.transform.IsChildOf(transform)) continue;
            if (col.GetComponentInParent<NetworkHealth>() != null) continue;
            if (col.GetComponentInParent<ItemCoativel>() != null) continue;
            if (col.GetComponentInParent<ProjetilCombate>() != null) continue;

            if (bufferChao[i].distance < menorDistancia)
            {
                menorDistancia = bufferChao[i].distance;
                alturaChaoY = bufferChao[i].point.y;
                achouChao = true;
            }
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

    // Roda em cada computador, mas só o DONO do player encostando pede a coleta.
    // O cliente só AVISA que encostou; quem decide é o servidor.
    private void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned || Time.time < proximoPedido) return;

        PlayerInventory inventario = other.GetComponentInParent<PlayerInventory>();
        if (inventario == null || !inventario.IsOwner) return;

        proximoPedido = Time.time + 0.5f; // evita spam de pedidos
        SolicitarColetaServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SolicitarColetaServerRpc(RpcParams rpcParams = default)
    {
        if (coletadoNoServidor || !IsSpawned) return;

        ulong remetente = rpcParams.Receive.SenderClientId;
        NetworkManager rede = NetworkManager.Singleton;

        // Quem pediu precisa ter um player de verdade
        if (!rede.ConnectedClients.TryGetValue(remetente, out NetworkClient cliente) || cliente.PlayerObject == null) return;

        NetworkObject player = cliente.PlayerObject;
        if (!player.TryGetComponent(out PlayerInventory inventario)) return;

        // Morto não coleta
        if (player.TryGetComponent(out NetworkHealth vida) && vida.estaMorto.Value) return;

        // Tem que estar perto do item (posição conhecida pelo servidor)
        float distanciaSqr = (player.transform.position - transform.position).sqrMagnitude;
        if (distanciaSqr > distanciaMaximaColeta * distanciaMaximaColeta) return;

        coletadoNoServidor = true;

        // A quantidade vem DESTE item, nunca do cliente
        inventario.AdicionarOuro(quantidadeOuro);
        Debug.Log($"[Loot] Player ID {remetente} coletou {quantidadeOuro} de ouro!");

        NetworkObject.Despawn(true);
    }
}
