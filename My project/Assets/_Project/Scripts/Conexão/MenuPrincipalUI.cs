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

    public async void AoClicarCriarSala()
    {
        if (textoStatusRede != null) textoStatusRede.text = "Criando sala na nuvem...";
        
        string codigoSala = await ServicoRedeRelay.Instance.CriarSalaRelay();

        if (!string.IsNullOrEmpty(codigoSala))
        {
            if (textoStatusRede != null) textoStatusRede.text = "Sala criada com sucesso!";
            if (textoCodigoGerado != null) textoCodigoGerado.text = $"Código: {codigoSala}";
            
            // Aguarda 1.5s para o player ver o código e avança para a escolha do herói
            Invoke(nameof(AvancarParaSelecaoPersonagem), 1.5f);
        }
        else
        {
            if (textoStatusRede != null) textoStatusRede.text = "Falha ao criar sala!";
        }
    }

    public async void AoClicarEntrarSala()
    {
        string codigoDigitado = inputCodigoSala != null ? inputCodigoSala.text.Trim() : "";

        if (string.IsNullOrEmpty(codigoDigitado))
        {
            if (textoStatusRede != null) textoStatusRede.text = "Digite o código da sala!";
            return;
        }

        if (textoStatusRede != null) textoStatusRede.text = $"Conectando à sala {codigoDigitado}...";

        bool sucesso = await ServicoRedeRelay.Instance.EntrarSalaRelay(codigoDigitado);

        if (sucesso)
        {
            if (textoStatusRede != null) textoStatusRede.text = "Conectado!";
            AvancarParaSelecaoPersonagem();
        }
        else
        {
            if (textoStatusRede != null) textoStatusRede.text = "Código inválido ou sala cheia!";
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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[Rede] Host carregando a cena {nomeCenaDoJogo} para todos...");
            NetworkManager.Singleton.SceneManager.LoadScene(nomeCenaDoJogo, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(nomeCenaDoJogo);
        }
    }
}