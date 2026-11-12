using Unity.Netcode;
using UnityEngine;

public class MobCombat : NetworkBehaviour
{
    [Header("Comportamento do Mob")]
    [SerializeField] private EstiloCombate estiloCombate = EstiloCombate.Melee;
    [SerializeField] private float raioDeteccao = 15f;
    [SerializeField] private float velocidadeMovimento = 3.5f;

    [Header("Atributos de Ataque")]
    [SerializeField] private float alcanceAtaque = 1.5f;
    [SerializeField] private int danoMob = 10;
    [SerializeField] private float intervaloAtaque = 1.5f;

    [Header("Configurações Exclusivas Ranged")]
    [SerializeField] private GameObject prefabProjetilMob;
    [SerializeField] private float velocidadeProjetil = 10f;
    [SerializeField] private float distanciaRecuoKiting = 4.0f;

    private NetworkHealth meuHealth;
    private float timerAtaque;

    // Buffers pré-alocados (Zero Garbage Collection)
    private readonly Collider[] bufferAlvos = new Collider[15];
    private readonly RaycastHit[] bufferChao = new RaycastHit[10];

    private void Awake()
    {
        meuHealth = GetComponent<NetworkHealth>();
    }

    public override void OnNetworkSpawn()
    {
        // Segurança caso o Inspector tenha ficado com 0 nos novos campos
        if (raioDeteccao <= 0.1f) raioDeteccao = 15f;
        if (velocidadeMovimento <= 0.1f) velocidadeMovimento = 3.5f;

        if (estiloCombate == EstiloCombate.Ranged && alcanceAtaque <= distanciaRecuoKiting)
        {
            alcanceAtaque = distanciaRecuoKiting + 3f;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (meuHealth != null && meuHealth.estaMorto.Value) return;

        timerAtaque += Time.deltaTime;

        NetworkHealth alvoPlayer = EncontrarPlayerVivoMaisProximo(out float distanciaAteAlvo);
        if (alvoPlayer == null) return;

        Vector3 direcaoProAlvo = (alvoPlayer.transform.position - transform.position);
        direcaoProAlvo.y = 0f;
        Vector3 dirNormalizada = direcaoProAlvo.sqrMagnitude > 0.001f ? direcaoProAlvo.normalized : transform.forward;

        // Gira para olhar para o Player
        if (dirNormalizada != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirNormalizada);
        }

        // Movimentação tática (Melee vs Ranged)
        if (estiloCombate == EstiloCombate.Melee)
        {
            if (distanciaAteAlvo > alcanceAtaque)
            {
                MoverMob(dirNormalizada);
            }
        }
        else // Ranged
        {
            if (distanciaAteAlvo < distanciaRecuoKiting)
            {
                MoverMob(-dirNormalizada);
            }
            else if (distanciaAteAlvo > alcanceAtaque - 0.5f)
            {
                MoverMob(dirNormalizada);
            }
        }

        // Executa o ataque se estiver no alcance
        if (distanciaAteAlvo <= alcanceAtaque && timerAtaque >= intervaloAtaque)
        {
            timerAtaque = 0f;
            if (estiloCombate == EstiloCombate.Melee)
            {
                AtacarPlayerMelee(alvoPlayer);
            }
            else
            {
                AtacarPlayerRanged(dirNormalizada);
            }
        }
    }

    private void MoverMob(Vector3 direcao)
    {
        Vector3 deslocamento = direcao * (velocidadeMovimento * Time.deltaTime);
        Vector3 posicaoFutura = transform.position + deslocamento;

        // Verifica se existe chão na posição futura para não sair flutuando para fora da plataforma
        Vector3 origemRaio = new Vector3(posicaoFutura.x, transform.position.y + 5.0f, posicaoFutura.z);
        int qtdHits = Physics.RaycastNonAlloc(origemRaio, Vector3.down, bufferChao, 20.0f, ~0, QueryTriggerInteraction.Ignore);

        bool achouChao = false;
        float alturaChaoY = transform.position.y;

        for (int i = 0; i < qtdHits; i++)
        {
            Collider col = bufferChao[i].collider;
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<PlayerMovimento>() != null) continue;
            if (col.GetComponent<MobCombat>() != null) continue;
            if (col.GetComponent<ProjetilCombate>() != null) continue;

            achouChao = true;
            alturaChaoY = bufferChao[i].point.y;
            break;
        }

        if (achouChao)
        {
            float metadeAlturaMob = transform.localScale.y * 0.5f;
            posicaoFutura.y = alturaChaoY + metadeAlturaMob;
            transform.position = posicaoFutura;
        }
    }

    private NetworkHealth EncontrarPlayerVivoMaisProximo(out float menorDistancia)
    {
        menorDistancia = float.MaxValue;
        NetworkHealth melhorAlvo = null;

        // Usa OverlapSphereNonAlloc procurando PlayerMovimento (mesma base do seu script original, mas otimizado)
        int qtd = Physics.OverlapSphereNonAlloc(transform.position, raioDeteccao, bufferAlvos);

        for (int i = 0; i < qtd; i++)
        {
            Collider hit = bufferAlvos[i];

            if (hit.TryGetComponent<PlayerMovimento>(out _) && hit.TryGetComponent<NetworkHealth>(out NetworkHealth playerHealth))
            {
                if (playerHealth.estaMorto.Value) continue;

                Vector3 diff = hit.transform.position - transform.position;
                diff.y = 0f;
                float dist = diff.magnitude;

                if (dist < menorDistancia)
                {
                    menorDistancia = dist;
                    melhorAlvo = playerHealth;
                }
            }
        }

        return melhorAlvo;
    }

    private void AtacarPlayerMelee(NetworkHealth alvoPlayer)
    {
        alvoPlayer.TomarDano(danoMob);
        Debug.Log($"[Servidor Melee] {gameObject.name} bateu no Player ID: {alvoPlayer.OwnerClientId}!");
    }

    private void AtacarPlayerRanged(Vector3 direcaoDisparo)
    {
        Vector3 posOrigem = transform.position + direcaoDisparo * 0.6f;

        InstanciarProjetilMobLocal(posOrigem, direcaoDisparo, true);
        DispararProjetilMobClientRpc(posOrigem, direcaoDisparo);
    }

    [ClientRpc]
    private void DispararProjetilMobClientRpc(Vector3 posOrigem, Vector3 direcaoDisparo)
    {
        if (IsServer) return;
        InstanciarProjetilMobLocal(posOrigem, direcaoDisparo, false);
    }

    private void InstanciarProjetilMobLocal(Vector3 posOrigem, Vector3 direcaoDisparo, bool validarNoServidor)
    {
        if (prefabProjetilMob == null)
        {
            Debug.LogWarning($"[MobCombat] {gameObject.name} está como Ranged, mas sem Prefab de Projétil!");
            return;
        }

        GameObject projObj = Instantiate(prefabProjetilMob, posOrigem, Quaternion.LookRotation(direcaoDisparo));
        if (projObj.TryGetComponent<ProjetilCombate>(out var projetil))
        {
            projetil.Inicializar(direcaoDisparo, velocidadeProjetil, danoMob, false, validarNoServidor);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeteccao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceAtaque);

        if (estiloCombate == EstiloCombate.Ranged)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, distanciaRecuoKiting);
        }
    }
#endif
}