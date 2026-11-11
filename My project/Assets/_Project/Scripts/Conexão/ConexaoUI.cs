using Unity.Netcode;
using UnityEngine;

public class ConexaoUI : MonoBehaviour
{
   private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
        Debug.LogError("[Rede] Não existe NetworkManager na cena. Adicione o componente NetworkManager ao objeto.");
        return;
        }

        Debug.Log("[Rede] ConexaoUI iniciou.");

        // Avisa no Console quando alguém conecta ou desconecta
        NetworkManager.Singleton.OnClientConnectedCallback += AoConectar;
        NetworkManager.Singleton.OnClientDisconnectCallback += AoDesconectar;
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= AoConectar;
        NetworkManager.Singleton.OnClientDisconnectCallback -= AoDesconectar;
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
        GUILayout.BeginArea(new Rect(10, 10, 260, 300));

        NetworkManager rede = NetworkManager.Singleton;

        if (rede == null)
        {
            GUILayout.Label("NetworkManager não encontrado na cena!");
            GUILayout.EndArea();
            return;
        }

        if (!rede.IsClient && !rede.IsServer)
        {
            // Ainda não conectou: mostra os botões de entrada
            if (GUILayout.Button("Host (criar sessão)", GUILayout.Height(40)))
                rede.StartHost();

            if (GUILayout.Button("Client (entrar)", GUILayout.Height(40)))
                rede.StartClient();
        }
        else
        {
            // Já conectado: mostra o estado
            string modo = rede.IsHost ? "Host" : (rede.IsServer ? "Servidor" : "Client");
            GUILayout.Label($"Modo: {modo}");

            if (rede.IsServer)
                GUILayout.Label($"Jogadores conectados: {rede.ConnectedClientsIds.Count}");

            if (GUILayout.Button("Desconectar", GUILayout.Height(40)))
                rede.Shutdown();
        }

        GUILayout.EndArea();
    }
}