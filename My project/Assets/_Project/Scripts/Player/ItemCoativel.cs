using Unity.Netcode;
using UnityEngine;

public class ItemCoativel : MonoBehaviour
{
    [SerializeField] private int quantidadeOuro = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (netObj.IsOwner && other.TryGetComponent<PlayerInventory>(out var inventario))
            {
                inventario.AdicionarOuroServerRpc(quantidadeOuro);
                Debug.Log($"[Loot] Player ID {netObj.OwnerClientId} coletou {quantidadeOuro} de ouro!");
                
                // Destrói o item no mundo (apenas o servidor comanda a destruição de objetos de rede)
                if (NetworkManager.Singleton.IsServer)
                {
                    GetComponent<NetworkObject>().Despawn();
                }
            }
        }
    }
}