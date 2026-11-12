using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAtaque : NetworkBehaviour
{
    [Header("Configurações do Ataque Básico")]
    [SerializeField] private float alcanceAtaque = 3f;
    [SerializeField] private int danoBase = 25;
    [SerializeField] private float tempoEntreAtaques = 0.4f;

    // Cache de componentes (Zero buscas no Update)
    private NetworkHealth networkHealth;
    private PlayerStats playerStats;

    // Buffer pré-alocado na memória para evitar Garbage Collection a cada golpe
    private readonly Collider[] bufferColisoes = new Collider[15];
    private float proximoTempoAtaque;

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 1. Checa PRIMEIRO se está morto antes de ler qualquer clique ou tecla!
        if (networkHealth != null && networkHealth.estaMorto.Value) return;

        // 2. Respeita o tempo de recarga (Cooldown) do golpe
        if (Time.time < proximoTempoAtaque) return;

        if (PressinouBotaoAtaque())
        {
            proximoTempoAtaque = Time.time + tempoEntreAtaques;
            AtacarServerRpc();
        }
    }

    private bool PressinouBotaoAtaque()
    {
        bool espacoPressionado = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool cliqueEsquerdo = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        return espacoPressionado || cliqueEsquerdo;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void AtacarServerRpc()
    {
        // Segurança dupla no Servidor: garante que um pacote atrasado não execute se o player já morreu
        if (networkHealth != null && networkHealth.estaMorto.Value) return;

        // Calcula o dano final escalando com a Força do PlayerStats (+3 de dano para cada ponto acima de 10)
        int bonusForca = playerStats != null ? (playerStats.forca.Value - 10) * 3 : 0;
        int danoFinal = danoBase + bonusForca;

        // Usa OverlapSphereNonAlloc para não alocar lixo na memória RAM
        int quantidadeAtingida = Physics.OverlapSphereNonAlloc(transform.position, alcanceAtaque, bufferColisoes);

        for (int i = 0; i < quantidadeAtingida; i++)
        {
            Collider hit = bufferColisoes[i];

            // Ignora o próprio jogador
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<NetworkHealth>(out NetworkHealth alvoHealth))
            {
                // Ignora alvos que já estão mortos
                if (alvoHealth.estaMorto.Value) continue;

                alvoHealth.TomarDano(danoFinal);
                Debug.Log($"[Combate] Player {OwnerClientId} causou {danoFinal} de dano em {hit.name}!");
                break; // Acerta o primeiro alvo válido no alcance
            }
        }
    }

#if UNITY_EDITOR
    // Desenha o círculo do alcance do ataque na aba Scene quando o Player está selecionado
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceAtaque);
    }
#endif
}