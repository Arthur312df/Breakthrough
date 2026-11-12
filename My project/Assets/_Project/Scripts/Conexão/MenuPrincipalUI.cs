using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Caso use TextMeshPro Dropdown

public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Painéis da UI")]
    [SerializeField] private GameObject painelTitulo;
    [SerializeField] private GameObject painelSelecao;

    [Header("Configuração da Cena do Jogo")]
    [SerializeField] private string nomeCenaJogo = "Cena_Teste";

    private void Start()
    {
        // Garante que começa na tela de Start
        if (painelTitulo != null) painelTitulo.SetActive(true);
        if (painelSelecao != null) painelSelecao.SetActive(false);
    }

    // --- TELA 1: START E SERVIDOR ---

    // Vincule no OnValueChanged de um Dropdown no cantinho da tela (0 = Host, 1 = Client)
    public void EscolherModoServidor(int indice)
    {
        DadosSessao.IniciarComoHost = (indice == 0);
        Debug.Log($"[Menu] Modo selecionado: {(DadosSessao.IniciarComoHost ? "Host (Criar)" : "Client (Entrar)")}");
    }

    // Alternativa caso prefira dois botões ou Toggle no cantinho em vez de Dropdown
    public void DefinirComoHost(bool ehHost)
    {
        DadosSessao.IniciarComoHost = ehHost;
    }

    // Vincule no botão "Start" da primeira tela
    public void ClicarBotaoStart()
    {
        painelTitulo.SetActive(false);
        painelSelecao.SetActive(true);
    }

    // --- TELA 2: SELEÇÃO DE PERSONAGEM ---

    // Vincule nos botões de cada personagem (passe 0, 1, 2 no Inspector do botão)
    public void SelecionarPersonagem(int idPersonagem)
    {
        DadosSessao.IdPersonagemEscolhido = idPersonagem;
        Debug.Log($"[Menu] Personagem ID {idPersonagem} selecionado.");
    }

    public void VoltarParaTitulo()
    {
        painelSelecao.SetActive(false);
        painelTitulo.SetActive(true);
    }

    // Vincule no botão "Iniciar" da tela de seleção de personagem
    public void ClicarBotaoIniciarJogo()
    {
        SceneManager.LoadScene(nomeCenaJogo);
    }
}