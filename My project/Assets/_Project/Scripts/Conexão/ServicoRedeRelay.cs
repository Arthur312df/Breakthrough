using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ServicoRedeRelay : MonoBehaviour
{
    public static ServicoRedeRelay Instance { get; private set; }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Relay] Autenticado na nuvem com ID anônimo: {AuthenticationService.Instance.PlayerId}");
        }
    }

    public async Task<string> CriarSalaRelay(int maximoJogadores = 4)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maximoJogadores - 1);
            string codigoSala = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Garante que o NetworkManager existe na cena ou cria um fallback seguro
            var netManager = NetworkManager.Singleton;
            if (netManager == null)
            {
                Debug.LogError("[Relay] NetworkManager não foi encontrado na cena!");
                return null;
            }

            var transport = netManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = netManager.gameObject.AddComponent<UnityTransport>();
            }
            
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            netManager.StartHost();
            Debug.Log($"[Relay] Sala criada com sucesso! Código de Acesso: {codigoSala}");
            
            return codigoSala;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Erro ao criar sala: {e.Message}");
            return null;
        }
    }

    public async Task<bool> EntrarSalaRelay(string codigoSala)
    {
        try
        {
            codigoSala = codigoSala.ToUpper();
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoSala);

            var netManager = NetworkManager.Singleton;
            if (netManager == null)
            {
                Debug.LogError("[Relay] NetworkManager não foi encontrado na cena!");
                return false;
            }

            var transport = netManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = netManager.gameObject.AddComponent<UnityTransport>();
            }

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            netManager.StartClient();
            Debug.Log($"[Relay] Conectado à sala com o código: {codigoSala}");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Erro ao entrar na sala {codigoSala}: {e.Message}");
            return false;
        }
    }
}