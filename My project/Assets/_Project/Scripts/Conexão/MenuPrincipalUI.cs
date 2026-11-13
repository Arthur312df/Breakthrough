using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Painéis da UI")]
    [SerializeField] private GameObject painelRedeRelay;
    [SerializeField] private GameObject painelSelecaoPersonagem;

    [Header("Elementos de Texto")]
    [SerializeField] private TMP_InputField inputCodigoSala;
    [SerializeField] private TMP_Text textoStatusRede;
    [SerializeField] private TMP_Text textoCodigoGerado;

    [Header("Configuração de Cenas")]
    [SerializeField] private string nomeCenaDoJogo = "Cena_Teste";

    private void Start()
    {
        // Garante estado limpo ao abrir o menu: Mostra rede, oculta seleção
        if (painelRedeRelay != null) painelRedeRelay.SetActive(true);
        if (painelSelecaoPersonagem != null) painelSelecaoPersonagem.SetActive(false);
    }

    private void DefinirStatus(string mensagem)
    {
        if (textoStatusRede != null) textoStatusRede.text = mensagem;
    }

    public async void AoClicarCriarSala()
    {
        if (ServicoRedeRelay.Instance == null)
        {
            DefinirStatus("Serviço de rede não encontrado na cena!");
            return;
        }

        DefinirStatus("Criando sala na nuvem...");

        string codigoSala = await ServicoRedeRelay.Instance.CriarSalaRelay();

        if (!string.IsNullOrEmpty(codigoSala))
        {
            DefinirStatus("Sala criada com sucesso!");
            if (textoCodigoGerado != null) textoCodigoGerado.text = $"Código: {codigoSala}";

            // Aguarda 1.5s para o player ver o código e avança para a escolha do herói
            Invoke(nameof(AvancarParaSelecaoPersonagem), 1.5f);
        }
        else
        {
            DefinirStatus("Falha ao criar sala!");
        }
    }

    public async void AoClicarEntrarSala()
    {
        string codigoDigitado = inputCodigoSala != null ? inputCodigoSala.text.Trim() : "";

        if (string.IsNullOrEmpty(codigoDigitado))
        {
            DefinirStatus("Digite o código da sala!");
            return;
        }

        if (ServicoRedeRelay.Instance == null)
        {
            DefinirStatus("Serviço de rede não encontrado na cena!");
            return;
        }

        DefinirStatus($"Conectando à sala {codigoDigitado}...");

        bool sucesso = await ServicoRedeRelay.Instance.EntrarSalaRelay(codigoDigitado);

        if (sucesso)
        {
            DefinirStatus("Conectado!");
            AvancarParaSelecaoPersonagem();
        }
        else
        {
            DefinirStatus("Código inválido ou sala cheia!");
        }
    }

    private void AvancarParaSelecaoPersonagem()
    {
        // Desativa totalmente o painel de rede e ativa o de seleção
        if (painelRedeRelay != null) painelRedeRelay.SetActive(false);
        if (painelSelecaoPersonagem != null) painelSelecaoPersonagem.SetActive(true);
    }

    public void AoClicarIniciarJogoNaCenaTeste()
    {
        NetworkManager rede = NetworkManager.Singleton;

        if (rede == null || !rede.IsListening)
        {
            DefinirStatus("Crie ou entre em uma sala antes de iniciar.");
            return;
        }

        // Clientes NUNCA carregam a cena sozinhos: quem manda é o Host.
        if (!rede.IsServer)
        {
            DefinirStatus("Aguardando o Host iniciar o jogo...");
            return;
        }

        Debug.Log($"[Rede] Host carregando a cena {nomeCenaDoJogo} para todos...");
        rede.SceneManager.LoadScene(nomeCenaDoJogo, LoadSceneMode.Single);
    }
}
