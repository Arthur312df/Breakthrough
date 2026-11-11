using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private int vidaMaxima = 100;
    [Header("Drops (Apenas para Mobs)")]
    [SerializeField] private GameObject prefabOuro;
    [SerializeField] private GameObject prefabEspada;
    [SerializeField] private bool ehMob = false;

    public NetworkVariable<int> vidaAtual = new NetworkVariable<int>(
        100, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // Evento para notificar o Spawner que este mob específico morreu
    public System.Action<NetworkHealth> OnMobMorreu;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            vidaAtual.Value = vidaMaxima;
        }
    }

    public void TomarDano(int quantidade)
    {
        if (!IsServer) return;

        vidaAtual.Value -= quantidade;
        vidaAtual.Value = Mathf.Clamp(vidaAtual.Value, 0, vidaMaxima);

        string tipo = ehMob ? "Mob" : "Player";
        Debug.Log($"[Health] {tipo} ID {NetworkObjectId} tomou {quantidade} de dano. Vida: {vidaAtual.Value}");

        if (vidaAtual.Value <= 0)
        {
            Morrer();
        }
    }

private void Morrer()
{
    if (ehMob)
    {
        Debug.Log($"[Health] Mob ID {NetworkObjectId} morreu.");

        // Se o mob tiver o script de loot, executa o drop
        if (TryGetComponent<MobLoot>(out MobLoot mobLoot))
        {
            mobLoot.DroparItens(transform.position);
        }

        // Notifica o Spawner e despacha da rede
        OnMobMorreu?.Invoke(this);
        GetComponent<NetworkObject>().Despawn(true);
    }
    else
    {
        Debug.Log($"[Health] Player ID {GetComponent<NetworkObject>().OwnerClientId} morreu!");
        NotificarMorteClientRpc();
    }
}

    private void DroparLoot()
    {
        Vector3 posicaoDrop = transform.position + Vector3.up * 0.5f;

        // Instancia o Ouro se configurado
        if (prefabOuro != null)
        {
            GameObject ouro = Instantiate(prefabOuro, posicaoDrop, Quaternion.identity);
            ouro.GetComponent<NetworkObject>().Spawn();
        }

        // Instancia a Espada se configurado
        if (prefabEspada != null)
        {
            Vector3 posicaoEspada = posicaoDrop + new Vector3(0.5f, 0f, 0f);
            GameObject espada = Instantiate(prefabEspada, posicaoEspada, Quaternion.identity);
            espada.GetComponent<NetworkObject>().Spawn();
        }
    }

    [ClientRpc]
    private void NotificarMorteClientRpc()
    {
        // Se este cliente for o dono do player que morreu, exibe a UI de Respawn
        if (IsOwner)
        {
            Debug.Log("[UI] Você morreu! Exibir botão de Respawn.");
            // Conecte isso ao seu painel de UI futuramente
        }
    }

    public void Reviver(Vector3 novaPosicao)
    {
        if (!IsServer) return;
        vidaAtual.Value = vidaMaxima;
        transform.position = novaPosicao;
    }
}