using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GerenciadorHUD : MonoBehaviour
{
    [Header("Textos Fixos (Sempre visíveis)")]
    [SerializeField] private TMP_Text textoVida;
    [SerializeField] private TMP_Text textoMana;
    [SerializeField] private TMP_Text textoXP;

    [Header("Textos que abrem/fecham no TAB ou I")]
    [SerializeField] private GameObject painelInventario;
    [SerializeField] private TMP_Text textoAtributos;
    [SerializeField] private TMP_Text textoOuro;
    [SerializeField] private TMP_Text textoItensInventario;

    private NetworkHealth saudeLocal;
    private PlayerStats statsLocal;
    private PlayerInventory inventarioLocal;
    private bool inventarioAberto = true;

    // Só reescreve um texto quando o valor mudou (evita criar lixo de memória todo frame)
    private bool forcarAtualizacao = true;
    private bool itensSujos = true;
    private readonly StringBuilder sb = new StringBuilder(128);

    private int ultVida, ultVidaMax, ultMana, ultManaMax, ultNivel, ultXp, ultXpProx;
    private int ultFor, ultAgi, ultInt, ultVit, ultOuro;

    private void Update()
    {
        if (ApertouTeclaInventario())
        {
            AlternarVisibilidadeInventario();
        }

        if (saudeLocal == null || statsLocal == null || inventarioLocal == null)
        {
            TentarEncontrarPlayerLocal();
            return;
        }

        AtualizarUI();
    }

    private void OnDestroy()
    {
        Desvincular();
    }

    private bool ApertouTeclaInventario()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado != null && (teclado.iKey.wasPressedThisFrame || teclado.tabKey.wasPressedThisFrame))
        {
            return true;
        }
#endif

        // Só usa o sistema antigo se ele estiver ativo no projeto.
        // (Sem este #if, projetos só com o Input System novo dão erro a cada frame.)
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            return true;
        }
#endif

        return false;
    }

    private void AlternarVisibilidadeInventario()
    {
        inventarioAberto = !inventarioAberto;

        if (inventarioAberto) forcarAtualizacao = true; // redesenha tudo ao reabrir

        // Se você arrastou um painel pai (e ele não é o próprio objeto deste script), liga/desliga o painel
        if (painelInventario != null && painelInventario != this.gameObject)
        {
            painelInventario.SetActive(inventarioAberto);
        }

        // Garante que os textos sumam/apareçam mesmo sem painel configurado
        if (textoAtributos != null) textoAtributos.gameObject.SetActive(inventarioAberto);
        if (textoOuro != null) textoOuro.gameObject.SetActive(inventarioAberto);
        if (textoItensInventario != null) textoItensInventario.gameObject.SetActive(inventarioAberto);
    }

    private void TentarEncontrarPlayerLocal()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null) return;

        // Só vincula quando o player tem os três componentes
        if (!localPlayer.TryGetComponent(out NetworkHealth saude)) return;
        if (!localPlayer.TryGetComponent(out PlayerStats stats)) return;
        if (!localPlayer.TryGetComponent(out PlayerInventory inventario)) return;

        Desvincular();

        saudeLocal = saude;
        statsLocal = stats;
        inventarioLocal = inventario;
        inventarioLocal.OnInventarioAtualizado += MarcarItensSujos;

        ResetarCache();
    }

    private void Desvincular()
    {
        if (inventarioLocal != null)
        {
            inventarioLocal.OnInventarioAtualizado -= MarcarItensSujos;
        }
    }

    private void MarcarItensSujos()
    {
        itensSujos = true;
    }

    private void ResetarCache()
    {
        forcarAtualizacao = true;
        itensSujos = true;
        ultVida = ultVidaMax = ultMana = ultManaMax = ultNivel = ultXp = ultXpProx = -1;
        ultFor = ultAgi = ultInt = ultVit = ultOuro = -1;
    }

    private void AtualizarUI()
    {
        // --- Sempre visíveis ---
        int vida = saudeLocal.vidaAtual.Value;
        int vidaMax = saudeLocal.vidaMaxima.Value;
        if (textoVida != null && (vida != ultVida || vidaMax != ultVidaMax))
        {
            ultVida = vida;
            ultVidaMax = vidaMax;
            textoVida.text = $"Vida: {vida} / {vidaMax}";
        }

        int mana = statsLocal.manaAtual.Value;
        int manaMax = statsLocal.manaMaxima.Value;
        if (textoMana != null && (mana != ultMana || manaMax != ultManaMax))
        {
            ultMana = mana;
            ultManaMax = manaMax;
            textoMana.text = $"Mana: {mana} / {manaMax}";
        }

        int nivel = statsLocal.nivel.Value;
        int xp = statsLocal.xpAtual.Value;
        int xpProx = statsLocal.xpProximoNivel.Value;
        if (textoXP != null && (nivel != ultNivel || xp != ultXp || xpProx != ultXpProx))
        {
            ultNivel = nivel;
            ultXp = xp;
            ultXpProx = xpProx;
            textoXP.text = $"Nível: {nivel} | XP: {xp} / {xpProx}";
        }

        if (!inventarioAberto) return;

        // --- Só quando o painel está aberto ---
        int forca = statsLocal.forca.Value;
        int agilidade = statsLocal.agilidade.Value;
        int inteligencia = statsLocal.inteligencia.Value;
        int vitalidade = statsLocal.vitalidade.Value;
        if (textoAtributos != null && (forcarAtualizacao || forca != ultFor || agilidade != ultAgi || inteligencia != ultInt || vitalidade != ultVit))
        {
            ultFor = forca;
            ultAgi = agilidade;
            ultInt = inteligencia;
            ultVit = vitalidade;
            textoAtributos.text = $"FOR: {forca} | AGI: {agilidade} | INT: {inteligencia} | VIT: {vitalidade}";
        }

        int ouro = inventarioLocal.ObterOuro();
        if (textoOuro != null && (forcarAtualizacao || ouro != ultOuro))
        {
            ultOuro = ouro;
            textoOuro.text = $"Ouro: {ouro}";
        }

        if (textoItensInventario != null && (forcarAtualizacao || itensSujos))
        {
            var itens = inventarioLocal.ObterItens();
            if (itens.Count == 0)
            {
                textoItensInventario.text = "Mochila Vazia";
            }
            else
            {
                sb.Clear();
                sb.Append("Itens:\n");
                for (int i = 0; i < itens.Count; i++)
                {
                    sb.Append("- ").Append(itens[i]).Append('\n');
                }
                textoItensInventario.text = sb.ToString();
            }
        }

        forcarAtualizacao = false;
        itensSujos = false;
    }
}
