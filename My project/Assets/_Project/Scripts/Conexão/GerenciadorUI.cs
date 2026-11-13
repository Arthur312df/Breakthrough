using UnityEngine;
using UnityEngine.UI;

public class GerenciadorUI : MonoBehaviour
{
    [Header("Referências da Cena (Arraste no Inspector)")]
    [SerializeField] private GameObject painelMorte;
    [SerializeField] private Button botaoRespawn;

    private NetworkHealth playerLocalHealth;

    private void Awake()
    {
        // Garante que o painel de morte comece fechado ao abrir a cena
        if (painelMorte != null)
        {
            painelMorte.SetActive(false);
        }

        NetworkHealth.OnPlayerLocalSpawnou += RegistrarPlayerLocal;
    }

    private void Start()
    {
        // Se o player já nasceu antes da UI existir, pega ele agora
        if (NetworkHealth.PlayerLocal != null)
        {
            RegistrarPlayerLocal(NetworkHealth.PlayerLocal);
        }
    }

    private void OnDestroy()
    {
        NetworkHealth.OnPlayerLocalSpawnou -= RegistrarPlayerLocal;

        if (playerLocalHealth != null)
        {
            playerLocalHealth.OnEstadoMorteLocalMudou -= AtualizarTelaMorte;
        }
    }

    private void RegistrarPlayerLocal(NetworkHealth health)
    {
        // Evita assinar duas vezes o mesmo evento
        if (playerLocalHealth != null)
        {
            playerLocalHealth.OnEstadoMorteLocalMudou -= AtualizarTelaMorte;
        }

        playerLocalHealth = health;
        playerLocalHealth.OnEstadoMorteLocalMudou += AtualizarTelaMorte;
        AtualizarTelaMorte(playerLocalHealth.estaMorto.Value);

        if (botaoRespawn != null)
        {
            botaoRespawn.onClick.RemoveAllListeners();
            botaoRespawn.onClick.AddListener(() =>
            {
                if (playerLocalHealth != null) playerLocalHealth.SolicitarRespawnServerRpc();
            });
        }
    }

    private void AtualizarTelaMorte(bool estaMorto)
    {
        if (painelMorte != null)
        {
            painelMorte.SetActive(estaMorto);
        }
    }
}
