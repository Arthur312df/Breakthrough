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

    private Task<bool> loginEmAndamento;
    private bool ocupado; // evita duplo clique em Criar/Entrar

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Já começa o login em segundo plano; Criar/Entrar esperam por ele
        _ = GarantirLogin();
    }

    // Todo mundo que precisa da nuvem espera por esta tarefa.
    // Se a tentativa anterior falhou, tenta de novo.
    public Task<bool> GarantirLogin()
    {
        if (loginEmAndamento == null || (loginEmAndamento.IsCompleted && !loginEmAndamento.Result))
        {
            loginEmAndamento = FazerLoginAsync();
        }
        return loginEmAndamento;
    }

    private async Task<bool> FazerLoginAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Relay] Autenticado na nuvem com ID anônimo: {AuthenticationService.Instance.PlayerId}");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Falha no login da nuvem: {e.Message}");
            return false;
        }
    }

    private UnityTransport ObterTransport()
    {
        NetworkManager netManager = NetworkManager.Singleton;
        if (netManager == null)
        {
            Debug.LogError("[Relay] NetworkManager não foi encontrado na cena!");
            return null;
        }

        if (!netManager.TryGetComponent(out UnityTransport transport))
        {
            Debug.LogError("[Relay] O NetworkManager não tem o componente Unity Transport!");
            return null;
        }

        return transport;
    }

    public async Task<string> CriarSalaRelay(int maximoJogadores = 4)
    {
        if (ocupado) return null;
        ocupado = true;

        try
        {
            if (!await GarantirLogin()) return null;

            UnityTransport transport = ObterTransport();
            if (transport == null) return null;

            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[Relay] A rede já está ligada. Desconecte antes de criar outra sala.");
                return null;
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maximoJogadores - 1);
            string codigoSala = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[Relay] O NetworkManager não conseguiu iniciar o Host.");
                return null;
            }

            Debug.Log($"[Relay] Sala criada com sucesso! Código de Acesso: {codigoSala}");
            return codigoSala;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Erro ao criar sala: {e.Message}");
            return null;
        }
        finally
        {
            ocupado = false;
        }
    }

    public async Task<bool> EntrarSalaRelay(string codigoSala)
    {
        if (ocupado) return false;
        ocupado = true;

        try
        {
            if (!await GarantirLogin()) return false;

            UnityTransport transport = ObterTransport();
            if (transport == null) return false;

            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[Relay] A rede já está ligada. Desconecte antes de entrar em outra sala.");
                return false;
            }

            codigoSala = codigoSala.Trim().ToUpper();
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoSala);

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("[Relay] O NetworkManager não conseguiu iniciar o Client.");
                return false;
            }

            Debug.Log($"[Relay] Conectando à sala com o código: {codigoSala}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Erro ao entrar na sala {codigoSala}: {e.Message}");
            return false;
        }
        finally
        {
            ocupado = false;
        }
    }
}
