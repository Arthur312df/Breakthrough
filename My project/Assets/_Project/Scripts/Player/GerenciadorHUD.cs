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

    private bool ApertouTeclaInventario()
    {
        bool apertou = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            apertou = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame;
        }
#endif

        // Fallback caso o Input Manager antigo também esteja ativo
        if (!apertou)
        {
            apertou = Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
        }

        return apertou;
    }

    private void AlternarVisibilidadeInventario()
    {
        inventarioAberto = !inventarioAberto;

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
        if (localPlayer != null)
        {
            localPlayer.TryGetComponent(out saudeLocal);
            localPlayer.TryGetComponent(out statsLocal);
            localPlayer.TryGetComponent(out inventarioLocal);
        }
    }

    private void AtualizarUI()
    {
        if (textoVida != null && saudeLocal != null)
        {
            textoVida.text = $"Vida: {saudeLocal.vidaAtual.Value} / {saudeLocal.vidaMaxima.Value}";
        }

        if (textoMana != null && statsLocal != null)
        {
            textoMana.text = $"Mana: {statsLocal.manaAtual.Value} / {statsLocal.manaMaxima.Value}";
        }

        if (textoXP != null && statsLocal != null)
        {
            textoXP.text = $"Nível: {statsLocal.nivel.Value} | XP: {statsLocal.xpAtual.Value} / {statsLocal.xpProximoNivel.Value}";
        }

        if (!inventarioAberto) return;

        if (textoAtributos != null && statsLocal != null)
        {
            textoAtributos.text = $"FOR: {statsLocal.forca.Value} | AGI: {statsLocal.agilidade.Value} | INT: {statsLocal.inteligencia.Value} | VIT: {statsLocal.vitalidade.Value}";
        }

        if (textoOuro != null && inventarioLocal != null)
        {
            textoOuro.text = $"Ouro: {inventarioLocal.ObterOuro()}";
        }

        if (textoItensInventario != null && inventarioLocal != null)
        {
            var itens = inventarioLocal.ObterItens();
            if (itens.Count == 0)
            {
                textoItensInventario.text = "Mochila Vazia";
            }
            else
            {
                StringBuilder sb = new StringBuilder("Itens:\n");
                foreach (var item in itens)
                {
                    sb.AppendLine($"- {item}");
                }
                textoItensInventario.text = sb.ToString();
            }
        }
    }
}