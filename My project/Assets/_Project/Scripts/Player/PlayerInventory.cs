using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public NetworkVariable<int> quantidadeOuro = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Itens: só o servidor escreve e só o DONO recebe a lista (nada de mandar para todo mundo)
    private NetworkList<FixedString128Bytes> itens;

    // Cópia em texto para a UI ler sem criar lixo a cada frame
    private readonly List<string> itensCache = new List<string>();

    public event Action<int> OnOuroAlterado;
    public event Action OnInventarioAtualizado;

    private void Awake()
    {
        // NetworkList precisa ser criada no Awake
        itens = new NetworkList<FixedString128Bytes>(
            null,
            NetworkVariableReadPermission.Owner,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        quantidadeOuro.OnValueChanged += AoMudarOuro;
        itens.OnListChanged += AoMudarItens;

        ReconstruirCache();
    }

    public override void OnNetworkDespawn()
    {
        quantidadeOuro.OnValueChanged -= AoMudarOuro;
        itens.OnListChanged -= AoMudarItens;
    }

    private void AoMudarOuro(int valorAntigo, int valorNovo)
    {
        OnOuroAlterado?.Invoke(valorNovo);
        OnInventarioAtualizado?.Invoke();
    }

    private void AoMudarItens(NetworkListEvent<FixedString128Bytes> evento)
    {
        ReconstruirCache();
        OnInventarioAtualizado?.Invoke();
    }

    private void ReconstruirCache()
    {
        itensCache.Clear();
        for (int i = 0; i < itens.Count; i++)
        {
            itensCache.Add(itens[i].ToString());
        }
    }

    // --- SOMENTE SERVIDOR ---
    // Antes eram RPCs abertos (qualquer cliente podia pedir qualquer quantidade de ouro / item).
    // Agora só código que roda no servidor consegue chamar.

    public void AdicionarOuro(int quantidade)
    {
        if (!IsServer || quantidade <= 0) return;

        quantidadeOuro.Value += quantidade;
        Debug.Log($"[Inventário] Ouro atualizado na rede: {quantidadeOuro.Value}");
    }

    public void AdicionarItem(string nomeItem)
    {
        if (!IsServer || string.IsNullOrEmpty(nomeItem)) return;

        if (nomeItem.Length > 40) nomeItem = nomeItem.Substring(0, 40);
        itens.Add(new FixedString128Bytes(nomeItem));
        Debug.Log($"[Inventário Servidor] Item adicionado: {nomeItem}");
    }

    public int ObterOuro() => quantidadeOuro.Value;
    public IReadOnlyList<string> ObterItens() => itensCache;
}
