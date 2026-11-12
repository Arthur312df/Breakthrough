using System;
using Unity.Netcode;
using UnityEngine;

// Estrutura leve de rede para representar um slot da mochila
public struct SlotInventario : INetworkSerializable, IEquatable<SlotInventario>
{
    public int IdItem;
    public int Quantidade;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref IdItem);
        serializer.SerializeValue(ref Quantidade);
    }

    public bool Equals(SlotInventario other)
    {
        return IdItem == other.IdItem && Quantidade == other.Quantidade;
    }
}

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int capacidadeMaxima = 20;

    // Lista sincronizada automaticamente pelo servidor para os clientes
    public NetworkList<SlotInventario> slots;

    public event Action OnInventarioMudou;

    private void Awake()
    {
        slots = new NetworkList<SlotInventario>(
            null, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            slots.OnListChanged += (_) => OnInventarioMudou?.Invoke();
        }
    }

    // Chamado no Servidor quando o Player coleta um drop do chão
    public bool AdicionarItem(int idItem, int quantidade = 1)
    {
        if (!IsServer) return false;

        // 1. Verifica se já existe esse item na mochila para empilhar (Stack)
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IdItem == idItem)
            {
                SlotInventario slotAtualizado = slots[i];
                slotAtualizado.Quantidade += quantidade;
                slots[i] = slotAtualizado;

                Debug.Log($"[Inventário] Item ID {idItem} empilhado! Nova qtd: {slotAtualizado.Quantidade}");
                return true;
            }
        }

        // 2. Se não existir e houver espaço livre, adiciona em um novo slot
        if (slots.Count >= capacidadeMaxima)
        {
            Debug.LogWarning("[Inventário] Mochila cheia!");
            return false;
        }

        slots.Add(new SlotInventario { IdItem = idItem, Quantidade = quantidade });
        Debug.Log($"[Inventário] Novo Item ID {idItem} adicionado (Qtd: {quantidade}).");
        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoverItemServerRpc(int indiceSlot, int quantidade = 1)
    {
        if (indiceSlot < 0 || indiceSlot >= slots.Count) return;

        SlotInventario slot = slots[indiceSlot];
        slot.Quantidade -= quantidade;

        if (slot.Quantidade <= 0)
        {
            slots.RemoveAt(indiceSlot);
        }
        else
        {
            slots[indiceSlot] = slot;
        }
    }
}