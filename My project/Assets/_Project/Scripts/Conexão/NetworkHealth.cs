using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private int vidaMaxima = 100;
    
    [Header("Drops (Apenas para Mobs)")]
    [SerializeField] private bool ehMob = false;

    public NetworkVariable<int> vidaAtual = new NetworkVariable<int>(
        100, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> estaMorto = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public System.Action<NetworkHealth> OnMobMorreu;

    private string Identificacao => TryGetComponent<NetworkObject>(out var netObj) 
        ? (CompareTag("Player") || netObj.IsPlayerObject ? $"Player ID {netObj.OwnerClientId}" : $"Mob ID {netObj.NetworkObjectId}") 
        : gameObject.name;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            vidaAtual.Value = vidaMaxima;
            estaMorto.Value = false;
        }

        // Quando o jogador local nasce, garante que o painel de morte comece fechado
        if (IsOwner && !ehMob)
        {
            ControlarPainelMorte(false);
        }
    }

    public void TomarDano(int quantidade)
    {
        if (!IsServer) return;
        if (estaMorto.Value) return;

        vidaAtual.Value -= quantidade;
        vidaAtual.Value = Mathf.Clamp(vidaAtual.Value, 0, vidaMaxima);

        Debug.Log($"[Health] {Identificacao} tomou {quantidade} de dano. Vida: {vidaAtual.Value}");

        if (vidaAtual.Value <= 0)
        {
            Morrer();
        }
    }

    private void Morrer()
    {
        if (ehMob)
        {
            if (TryGetComponent<MobLoot>(out MobLoot mobLoot))
            {
                mobLoot.DroparItens(transform.position);
            }

            OnMobMorreu?.Invoke(this);
            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            Debug.Log($"[Health] {Identificacao} morreu!");
            estaMorto.Value = true;
            NotificarMorteClientRpc();
        }
    }

    [ClientRpc]
    private void NotificarMorteClientRpc()
    {
        if (IsOwner)
        {
            ControlarPainelMorte(true);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SolicitarRespawnServerRpc()
    {
        if (!IsServer) return;

        // 1. Pega a posição do PontoRespawn
        Vector3 posicaoRespawn = Vector3.zero;
        if (RespawnManager.Instance != null)
        {
            posicaoRespawn = RespawnManager.Instance.ObterPosicaoRespawn();
        }
        else
        {
            GameObject pontoObj = GameObject.Find("PontoRespawn");
            posicaoRespawn = pontoObj != null ? pontoObj.transform.position : new Vector3(0f, 1f, 0f);
        }

        // 2. Restaura vida e estado
        vidaAtual.Value = vidaMaxima;
        estaMorto.Value = false;

        // 3. Teleporta no Servidor e avisa o Cliente Dono (corrige o Player 1 continuar no lugar de morte!)
        ExecutarTeleporteFisico(posicaoRespawn);
        TeleportarClienteClientRpc(posicaoRespawn);

        Debug.Log($"[Health] Player {gameObject.name} renasceu em {posicaoRespawn}.");
    }

    [ClientRpc]
    private void TeleportarClienteClientRpc(Vector3 novaPosicao)
    {
        if (IsOwner)
        {
            // Se for o Player 1 (Cliente), aplica o teleporte localmente também
            if (!IsServer)
            {
                ExecutarTeleporteFisico(novaPosicao);
            }

            // Fecha a tela de morte
            ControlarPainelMorte(false);
        }
    }

    private void ExecutarTeleporteFisico(Vector3 novaPosicao)
    {
        CharacterController cc = GetComponent<CharacterController>();
        NetworkTransform netTransform = GetComponent<NetworkTransform>();

        if (cc != null) cc.enabled = false;

        transform.position = novaPosicao;

        if (netTransform != null && netTransform.CanCommitToTransform)
        {
            netTransform.Teleport(novaPosicao, transform.rotation, transform.localScale);
        }

        if (cc != null) cc.enabled = true;
    }

    // Controla o painel diretamente na cena sem depender de outro script no Prefab
    private void ControlarPainelMorte(bool mostrar)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform painel = canvas.transform.Find("PainelMorte");
            if (painel != null)
            {
                painel.gameObject.SetActive(mostrar);

                // Se estiver abrindo o painel, já garante que o botão funcione
                if (mostrar)
                {
                    UnityEngine.UI.Button btn = painel.GetComponentInChildren<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SolicitarRespawnServerRpc());
                    }
                }
            }
        }
    }
}