using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public NetworkVariable<int> quantidadeOuro = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    // Lista local sincronizada para o dono exibir na UI
    private List<string> itensNoInventario = new List<string>();

    public event Action<int> OnOuroAlterado;
    public event Action OnInventarioAtualizado;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            quantidadeOuro.OnValueChanged += (valorAntigo, valorNovo) => {
                OnOuroAlterado?.Invoke(valorNovo);
                OnInventarioAtualizado?.Invoke();
            };
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AdicionarOuroServerRpc(int quantidade)
    {
        quantidadeOuro.Value += quantidade;
        Debug.Log($"[Inventário] Ouro atualizado na rede: {quantidadeOuro.Value}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AdicionarItemServerRpc(string nomeItem)
    {
        itensNoInventario.Add(nomeItem);
        Debug.Log($"[Inventário Servidor] Item adicionado: {nomeItem}");
        SincronizarItemClientRpc(nomeItem);
    }

    [ClientRpc]
    private void SincronizarItemClientRpc(string nomeItem)
    {
        if (IsOwner && !IsServer)
        {
            itensNoInventario.Add(nomeItem);
        }
        
        if (IsOwner)
        {
            OnInventarioAtualizado?.Invoke();
        }
    }

    public int ObterOuro() => quantidadeOuro.Value;
    public IReadOnlyList<string> ObterItens() => itensNoInventario;
}