using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private int vidaMaximaInicial = 100;
    
    [Header("Configuração de Entidade")]
    [SerializeField] private bool ehMob = false;
    public bool EhMob => ehMob;
    [SerializeField] private int xpRecompensa = 40;

    public NetworkVariable<int> vidaMaxima = new NetworkVariable<int>(
        100, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

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

    public Action<NetworkHealth> OnMobMorreu;
    public static event Action<NetworkHealth> OnPlayerLocalSpawnou;
    public event Action<bool> OnEstadoMorteLocalMudou;

    private PlayerMovimento playerMovimento;
    private MobLoot mobLoot;

    private void Awake()
    {
        playerMovimento = GetComponent<PlayerMovimento>();
        mobLoot = GetComponent<MobLoot>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            vidaMaxima.Value = vidaMaximaInicial;
            vidaAtual.Value = vidaMaximaInicial;
            estaMorto.Value = false;
        }

        if (IsOwner && !ehMob)
        {
            estaMorto.OnValueChanged += AoMudarEstadoMorte;
            OnPlayerLocalSpawnou?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && !ehMob)
        {
            estaMorto.OnValueChanged -= AoMudarEstadoMorte;
        }
    }

    private void AoMudarEstadoMorte(bool estavaMorto, bool mortoAgora)
    {
        OnEstadoMorteLocalMudou?.Invoke(mortoAgora);
    }

    public void AtualizarVidaMaxima(int novaVidaMaxima, bool curarTotalmente)
    {
        if (!IsServer) return;

        vidaMaxima.Value = novaVidaMaxima;
        if (curarTotalmente)
        {
            vidaAtual.Value = vidaMaxima.Value;
        }
    }

    public void TomarDano(int quantidade)
    {
        if (!IsServer || estaMorto.Value) return;

        vidaAtual.Value = Mathf.Clamp(vidaAtual.Value - quantidade, 0, vidaMaxima.Value);

        if (vidaAtual.Value <= 0)
        {
            Morrer();
        }
    }

    private void Morrer()
    {
        // Trava dupla para evitar chamadas duplicadas no mesmo frame
        if (estaMorto.Value) return;
        estaMorto.Value = true;

        if (ehMob)
        {
            EntregarXpParaPlayers();

            if (mobLoot != null)
            {
                mobLoot.DroparItens(transform.position);
            }

            OnMobMorreu?.Invoke(this);
            NetworkObject.Despawn(true);
        }
    }

    private void EntregarXpParaPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerStats>(out var stats))
            {
                stats.GanharXP(xpRecompensa);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SolicitarRespawnServerRpc()
    {
        if (!IsServer) return;

        Vector3 posicaoRespawn = RespawnManager.Instance != null 
            ? RespawnManager.Instance.ObterPosicaoRespawn() 
            : new Vector3(0f, 1f, 0f);

        vidaAtual.Value = vidaMaxima.Value;
        estaMorto.Value = false;

        if (playerMovimento != null) playerMovimento.Teleportar(posicaoRespawn);
        TeleportarClienteClientRpc(posicaoRespawn);
    }

    [ClientRpc]
    private void TeleportarClienteClientRpc(Vector3 novaPosicao)
    {
        if (IsOwner && !IsServer && playerMovimento != null)
        {
            playerMovimento.Teleportar(novaPosicao);
        }
    }
}