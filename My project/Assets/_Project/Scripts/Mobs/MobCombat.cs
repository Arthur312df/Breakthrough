using Unity.Netcode;
using UnityEngine;

public class MobCombat : NetworkBehaviour
{
    [SerializeField] private float alcanceAtaque = 1.5f;
    [SerializeField] private int danoMob = 10;
    [SerializeField] private float intervaloAtaque = 1.5f;

    private float timerAtaque;

    private void Update()
    {
        if (!IsServer) return;

        timerAtaque += Time.deltaTime;
        if (timerAtaque >= intervaloAtaque)
        {
            TentarAtacarPlayerMaisProximo();
            timerAtaque = 0f;
        }
    }

    private void TentarAtacarPlayerMaisProximo()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alcanceAtaque);
        
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<PlayerMovimento>(out _) && hit.TryGetComponent<NetworkHealth>(out NetworkHealth playerHealth))
            {
                // Pega o NetworkObject do player para ler o OwnerClientId
                if (hit.TryGetComponent<NetworkObject>(out NetworkObject netObj))
                {
                    playerHealth.TomarDano(danoMob);
                    Debug.Log($"[Servidor] {gameObject.name} bateu no Player ID: {netObj.OwnerClientId}!");
                }
                break;
            }
        }
    }
}