using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConexaoUI : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[Rede] ConexaoUI iniciou.");

        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += AoConectar;
        NetworkManager.Singleton.OnClientDisconnectCallback += AoDesconectar;

        // Configura o IP e Porta escolhidos lá no menu (se houver UnityTransport)
        if (NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
        {
            transport.SetConnectionData(DadosSessao.EnderecoIP, DadosSessao.Porta);
        }

        // Inicia automaticamente de acordo com o que foi escolhido no Menu!
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (DadosSessao.IniciarComoHost)
            {
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                NetworkManager.Singleton.StartClient();
            }
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
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 220, 100));

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            string modo = NetworkManager.Singleton.IsHost ? "Host" : "Client";
            GUILayout.Label($"Modo: {modo} | Classe ID: {DadosSessao.IdPersonagemEscolhido}");

            if (GUILayout.Button("Desconectar / Menu"))
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Cena_Menu");
            }
        }

        GUILayout.EndArea();
    }
}