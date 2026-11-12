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
        playerLocalHealth = health;
        playerLocalHealth.OnEstadoMorteLocalMudou += AtualizarTelaMorte;

        if (botaoRespawn != null)
        {
            botaoRespawn.onClick.RemoveAllListeners();
            botaoRespawn.onClick.AddListener(() => playerLocalHealth.SolicitarRespawnServerRpc());
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