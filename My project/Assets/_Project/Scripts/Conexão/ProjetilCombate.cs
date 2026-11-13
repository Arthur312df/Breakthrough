using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProjetilCombate : MonoBehaviour
{
    private Vector3 direcao;
    private float velocidade;
    private int dano;
    private bool disparadoPorPlayer;
    private bool ehServidor;
    private ulong idAtirador;
    private SkillData skillOrigem;
    private bool jaAcertou; // evita dano duplo se encostar em dois alvos no mesmo frame

    [SerializeField] private float tempoDeVida = 4f;

    public void Inicializar(Vector3 dir, float vel, int danoCausado, bool origemPlayer, bool rodarLogicaNoServidor, ulong atiradorId = 0, SkillData skill = null)
    {
        direcao = dir.normalized;
        direcao.y = 0f; // Trajetória plana para o Top-Down Isométrico
        velocidade = vel;
        dano = danoCausado;
        disparadoPorPlayer = origemPlayer;
        ehServidor = rodarLogicaNoServidor;
        idAtirador = atiradorId;
        skillOrigem = skill;

        if (direcao != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direcao);
        }

        Destroy(gameObject, tempoDeVida);
    }

    private void Update()
    {
        transform.position += direcao * (velocidade * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (jaAcertou) return;

        // Ignora colisão com quem atirou
        if (disparadoPorPlayer && other.CompareTag("Player")) return;
        if (!disparadoPorPlayer && other.GetComponentInParent<MobCombat>() != null) return;

        // Ignora moedas/itens no chão
        if (other.GetComponentInParent<ItemCoativel>() != null) return;

        NetworkHealth alvoHealth = other.GetComponentInParent<NetworkHealth>();

        // Não é um alvo com vida: só para em paredes/cenário, não em zonas de gatilho
        if (alvoHealth == null)
        {
            if (other.isTrigger) return;

            jaAcertou = true;
            Destroy(gameObject);
            return;
        }

        // Atravessa cadáveres
        if (alvoHealth.estaMorto.Value) return;

        jaAcertou = true;

        // Apenas o servidor valida e aplica o dano real na rede.
        // Player atira em Mob, ou Mob atira em Player.
        if (ehServidor && disparadoPorPlayer == alvoHealth.EhMob)
        {
            if (disparadoPorPlayer)
            {
                PlayerSkills.AplicarDanoEEfeitosNoAlvo(alvoHealth, skillOrigem, dano);
            }
            else
            {
                alvoHealth.TomarDano(dano);
            }

            Log($"[Combate Ranged] Projétil atingiu {other.name} causando {dano} de dano base!");
        }

        Destroy(gameObject);
    }

    // Só existe no Editor: no jogo final o Debug.Log some e não pesa
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void Log(string mensagem)
    {
        Debug.Log(mensagem);
    }
}
