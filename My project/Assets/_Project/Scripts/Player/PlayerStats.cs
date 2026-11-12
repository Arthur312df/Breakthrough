using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Header("Valores Base (Nível 1)")]
    [SerializeField] private int vidaBase = 100;
    [SerializeField] private int manaBase = 50;

    // --- NÍVEL E EXPERIÊNCIA ---
    public NetworkVariable<int> nivel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> xpAtual = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> xpProximoNivel = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- MANA ---
    public NetworkVariable<int> manaMaxima = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> manaAtual = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- ATRIBUTOS (Começam em 10 -> 11 -> 12...) ---
    public NetworkVariable<int> forca = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> agilidade = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> inteligencia = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> vitalidade = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static event Action<PlayerStats> OnPlayerStatsLocalSpawnou;
    public event Action OnStatsAtualizados;

    private NetworkHealth networkHealth;

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            RecalcularAtributosDerivados(true);
        }

        if (IsOwner)
        {
            nivel.OnValueChanged += (_, _) => OnStatsAtualizados?.Invoke();
            xpAtual.OnValueChanged += (_, _) => OnStatsAtualizados?.Invoke();
            manaAtual.OnValueChanged += (_, _) => OnStatsAtualizados?.Invoke();

            OnPlayerStatsLocalSpawnou?.Invoke(this);
        }
    }

    public void GanharXP(int quantidade)
    {
        if (!IsServer) return;

        xpAtual.Value += quantidade;
        Debug.Log($"[Stats] Player {OwnerClientId} ganhou {quantidade} XP ({xpAtual.Value}/{xpProximoNivel.Value})");

        while (xpAtual.Value >= xpProximoNivel.Value)
        {
            SubirDeNivel();
        }
    }

    private void SubirDeNivel()
    {
        xpAtual.Value -= xpProximoNivel.Value;
        nivel.Value++;
        xpProximoNivel.Value = Mathf.RoundToInt(xpProximoNivel.Value * 1.25f);

        forca.Value += 1;
        agilidade.Value += 1;
        inteligencia.Value += 1;
        vitalidade.Value += 1;

        RecalcularAtributosDerivados(true);

        Debug.Log($"[Level Up!] Player {OwnerClientId} subiu para o Nível {nivel.Value}! Status agora: {forca.Value}");
    }

    public void RecalcularAtributosDerivados(bool restaurarVidaEMana)
    {
        if (!IsServer) return;

        int novaVidaMax = vidaBase + ((vitalidade.Value - 10) * 15);
        int novaManaMax = manaBase + ((inteligencia.Value - 10) * 10);

        manaMaxima.Value = novaManaMax;
        if (restaurarVidaEMana)
        {
            manaAtual.Value = novaManaMax;
        }

        if (networkHealth != null)
        {
            networkHealth.AtualizarVidaMaxima(novaVidaMax, restaurarVidaEMana);
        }
    }

    public bool GastarMana(int custo)
    {
        if (!IsServer || manaAtual.Value < custo) return false;
        manaAtual.Value -= custo;
        return true;
    }
}