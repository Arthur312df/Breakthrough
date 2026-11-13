using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConexaoUI : MonoBehaviour
{
    [SerializeField] private string nomeCenaMenu = "Cena_Menu";

    private void Start()
    {
        NetworkManager rede = NetworkManager.Singleton;
        if (rede == null)
        {
            Debug.LogWarning("[Rede] Nenhum NetworkManager encontrado. Abra o jogo pela Cena_Menu.");
            return;
        }

        rede.OnClientConnectedCallback += AoConectar;
        rede.OnClientDisconnectCallback += AoDesconectar;

        // Se a rede já está ligada (veio do menu com Relay), NÃO mexe no transporte
        // nem inicia de novo.
        if (rede.IsListening) return;

        // Rede ainda desligada (ex: teste direto por IP/localhost)
        if (rede.TryGetComponent(out UnityTransport transport))
        {
            transport.SetConnectionData(DadosSessao.EnderecoIP, DadosSessao.Porta);
        }

        if (DadosSessao.IniciarComoHost)
        {
            rede.StartHost();
        }
        else
        {
            rede.StartClient();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AoConectar;
            NetworkManager.Singleton.OnClientDisconnectCallback -= AoDesconectar;
        }
    }

    private void AoConectar(ulong clientId)
    {
        Debug.Log($"[Rede] Jogador {clientId} conectou.");
    }

    private void AoDesconectar(ulong clientId)
    {
        Debug.Log($"[Rede] Jogador {clientId} desconectou.");
    }

    private void OnGUI()
    {
        NetworkManager rede = NetworkManager.Singleton;
        if (rede == null || !rede.IsListening) return;

        GUILayout.BeginArea(new Rect(10, 10, 240, 100));

        string modo = rede.IsHost ? "Host" : "Client";
        GUILayout.Label($"Modo: {modo} | Classe ID: {DadosSessao.IdPersonagemEscolhido}");

        if (GUILayout.Button("Desconectar / Menu"))
        {
            rede.Shutdown();
            SceneManager.LoadScene(nomeCenaMenu);
        }

        GUILayout.EndArea();
    }
}
