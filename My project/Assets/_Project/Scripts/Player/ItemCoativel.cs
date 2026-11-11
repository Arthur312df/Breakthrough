using Unity.Netcode;
using UnityEngine;

public class ItemCoativel : NetworkBehaviour
{
    [SerializeField] private int valorOuro = 10;

    private void OnTriggerEnter(Collider other)
    {
        // A coleta de itens deve ser processada exclusivamente pelo Servidor
        if (!IsServer) return;

        // Verifica se o objeto que encostou é um Player
        if (other.TryGetComponent<PlayerMovimento>(out _) || other.CompareTag("Player"))
        {
            if (other.TryGetComponent<NetworkObject>(out NetworkObject playerNetObj))
            {
                Debug.Log($"[Loot] Player ID {playerNetObj.OwnerClientId} coletou {valorOuro} de ouro!");
                
                // Destrói o item da rede (despawn)
                GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }
}