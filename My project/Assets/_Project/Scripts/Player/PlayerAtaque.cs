using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EstiloCombate
{
    Melee,
    Ranged
}

public class PlayerAtaque : NetworkBehaviour
{
    [Header("Estilo de Combate")]
    [SerializeField] private EstiloCombate estiloCombate = EstiloCombate.Melee;

    [Header("Configurações Gerais do Ataque Básico")]
    [SerializeField] private int danoBase = 25;
    [SerializeField] private float tempoEntreAtaques = 0.4f;

    [Header("Configurações Melee")]
    [SerializeField] private float alcanceMelee = 3f;
    [Tooltip("Ângulo do cone frontal de ataque (ex: 120 graus na frente do player)")]
    [SerializeField] private float anguloConeMelee = 120f;

    [Header("Configurações Ranged")]
    [SerializeField] private GameObject prefabProjetil;
    [SerializeField] private float velocidadeProjetil = 16f;
    [SerializeField] private float alturaDisparo = 1.0f;

    // Cache de componentes
    private NetworkHealth networkHealth;
    private PlayerStats playerStats;
    private Camera camPrincipal;

    // Buffer pré-alocado na memória (Zero Garbage Collection)
    private readonly Collider[] bufferColisoes = new Collider[15];
    private float proximoTempoAtaque;

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (networkHealth != null && networkHealth.estaMorto.Value) return;
        if (Time.time < proximoTempoAtaque) return;

        if (PressionouBotaoAtaque())
        {
            proximoTempoAtaque = Time.time + tempoEntreAtaques;
            Vector3 direcaoMira = ObterDirecaoDoMouse();
            
            // Gira o player visualmente para onde atacou
            if (direcaoMira != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direcaoMira);
            }

            AtacarServerRpc(direcaoMira);
        }
    }

    private bool PressionouBotaoAtaque()
    {
        bool espacoPressionado = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool cliqueEsquerdo = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        return espacoPressionado || cliqueEsquerdo;
    }

    private Vector3 ObterDirecaoDoMouse()
    {
        if (camPrincipal == null) camPrincipal = Camera.main;
        if (camPrincipal == null || Mouse.current == null) return transform.forward;

        Ray raio = camPrincipal.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane planoChao = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (planoChao.Raycast(raio, out float distancia))
        {
            Vector3 pontoAlvo = raio.GetPoint(distancia);
            Vector3 direcao = pontoAlvo - transform.position;
            direcao.y = 0f;

            if (direcao.sqrMagnitude > 0.01f)
            {
                return direcao.normalized;
            }
        }

        return transform.forward;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void AtacarServerRpc(Vector3 direcaoAtaque)
    {
        if (networkHealth != null && networkHealth.estaMorto.Value) return;

        direcaoAtaque.y = 0f;
        if (direcaoAtaque.sqrMagnitude < 0.01f) direcaoAtaque = transform.forward;
        direcaoAtaque.Normalize();

        int danoFinal = CalcularDanoFinal();

        if (estiloCombate == EstiloCombate.Melee)
        {
            ExecutarAtaqueMelee(direcaoAtaque, danoFinal);
        }
        else
        {
            ExecutarAtaqueRanged(direcaoAtaque, danoFinal);
        }
    }

    private int CalcularDanoFinal()
    {
        if (playerStats == null) return danoBase;

        // Melee escala mais com Força; Ranged escala com Agilidade
        int atributoPrincipal = (estiloCombate == EstiloCombate.Melee) 
            ? playerStats.forca.Value 
            : playerStats.agilidade.Value;

        int bonusAtributo = (atributoPrincipal - 10) * 3;
        return danoBase + bonusAtributo;
    }

    private void ExecutarAtaqueMelee(Vector3 direcaoAtaque, int danoFinal)
    {
        int quantidadeAtingida = Physics.OverlapSphereNonAlloc(transform.position, alcanceMelee, bufferColisoes);
        float cossenoLimiteCone = Mathf.Cos((anguloConeMelee * 0.5f) * Mathf.Deg2Rad);

        NetworkHealth melhorAlvo = null;
        float menorDistanciaSqr = float.MaxValue;

        for (int i = 0; i < quantidadeAtingida; i++)
        {
            Collider hit = bufferColisoes[i];
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<NetworkHealth>(out NetworkHealth alvoHealth))
            {
                // Ignora alvos mortos ou outros jogadores (evita Friendly Fire)
                if (alvoHealth.estaMorto.Value || !alvoHealth.EhMob) continue;

                Vector3 proAlvo = hit.transform.position - transform.position;
                proAlvo.y = 0f;
                float distSqr = proAlvo.sqrMagnitude;

                // Se estiver fora do cone frontal de ataque (e não estiver colado no corpo do player), ignora
                if (distSqr > 0.5f)
                {
                    float dot = Vector3.Dot(direcaoAtaque, proAlvo.normalized);
                    if (dot < cossenoLimiteCone) continue;
                }

                // Prioriza o inimigo mais próximo dentro do arco do golpe
                if (distSqr < menorDistanciaSqr)
                {
                    menorDistanciaSqr = distSqr;
                    melhorAlvo = alvoHealth;
                }
            }
        }

        if (melhorAlvo != null)
        {
            melhorAlvo.TomarDano(danoFinal);
            Debug.Log($"[Combate Melee] Player {OwnerClientId} causou {danoFinal} de dano em {melhorAlvo.name}!");
        }
    }

    private void ExecutarAtaqueRanged(Vector3 direcaoAtaque, int danoFinal)
    {
        Vector3 posOrigem = transform.position + Vector3.up * alturaDisparo + direcaoAtaque * 0.6f;

        // Cria o projétil no servidor (que valida o acerto) e avisa os clientes para desenharem o projétil
        InstanciarProjetilLocal(posOrigem, direcaoAtaque, danoFinal, true);
        DispararProjetilVisualClientRpc(posOrigem, direcaoAtaque, danoFinal);
    }

    [ClientRpc]
    private void DispararProjetilVisualClientRpc(Vector3 posOrigem, Vector3 direcaoAtaque, int danoFinal)
    {
        // O Host já instanciou no ExecutarAtaqueRanged, então só os outros clientes criam a cópia visual aqui
        if (IsServer) return;
        InstanciarProjetilLocal(posOrigem, direcaoAtaque, danoFinal, false);
    }

    private void InstanciarProjetilLocal(Vector3 posOrigem, Vector3 direcaoAtaque, int danoFinal, bool validarNoServidor)
    {
        if (prefabProjetil == null)
        {
            Debug.LogWarning("[PlayerAtaque] Prefab do Projétil não atribuído no Inspector para ataque Ranged!");
            return;
        }

        GameObject projObj = Instantiate(prefabProjetil, posOrigem, Quaternion.LookRotation(direcaoAtaque));
        if (projObj.TryGetComponent<ProjetilCombate>(out var projetil))
        {
            projetil.Inicializar(direcaoAtaque, velocidadeProjetil, danoFinal, true, validarNoServidor, OwnerClientId);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = estiloCombate == EstiloCombate.Melee ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, alcanceMelee);
    }
#endif
}