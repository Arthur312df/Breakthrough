using Unity.Netcode;
using UnityEngine;

public class PlayerAtaque : NetworkBehaviour
{
    [SerializeField] private float alcanceAtaque = 3f;
    [SerializeField] private int danoAtaque = 25;

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[Player {OwnerClientId}] Tecla de ataque pressionada!");
            AtacarServerRpc();
        }
        // Impede de atacar se estiver morto
        if (TryGetComponent<NetworkHealth>(out var health) && health.estaMorto.Value)
        {
        return;
        }
    }

    [ServerRpc]
    private void AtacarServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientIdAtacante = rpcParams.Receive.SenderClientId;
        Debug.Log($"[Servidor] Processando ataque do Player ID: {clientIdAtacante}");

        Collider[] hits = Physics.OverlapSphere(transform.position, alcanceAtaque);

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<NetworkHealth>(out NetworkHealth health))
            {
                // Evita que o player bata nele mesmo
                if (health.gameObject == gameObject) continue;

                health.TomarDano(danoAtaque);
                Debug.Log($"[Servidor] Player ID {clientIdAtacante} acertou {hit.name}!");
                break; 
            }
        }
    }
}