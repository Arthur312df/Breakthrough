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

    [SerializeField] private float tempoDeVida = 4f;

    public void Inicializar(Vector3 dir, float vel, int danoCausado, bool origemPlayer, bool rodarLogicaNoServidor, ulong atiradorId = 0)
    {
        direcao = dir.normalized;
        direcao.y = 0f; // Trajetória plana para o Top-Down Isométrico
        velocidade = vel;
        dano = danoCausado;
        disparadoPorPlayer = origemPlayer;
        ehServidor = rodarLogicaNoServidor;
        idAtirador = atiradorId;

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
        // Ignora colisão com quem atirou
        if (disparadoPorPlayer && other.CompareTag("Player")) return;
        if (!disparadoPorPlayer && other.GetComponent<MobCombat>() != null) return;

        // Ignora moedas/itens no chão
        if (other.GetComponent<ItemCoativel>() != null) return;

        // Apenas o servidor valida e aplica o dano real na rede
        if (ehServidor)
        {
            if (other.TryGetComponent<NetworkHealth>(out var alvoHealth))
            {
                if (alvoHealth.estaMorto.Value) return;

                // Se o player atirou num Mob, ou se um Mob atirou num Player
                bool alvoEhMob = alvoHealth.EhMob;
                if (disparadoPorPlayer == alvoEhMob)
                {
                    alvoHealth.TomarDano(dano);
                    Debug.Log($"[Combate Ranged] Projétil atingiu {other.name} causando {dano} de dano!");
                }
            }
        }

        // Destrói o projétil visualmente ao bater no alvo ou cenário
        Destroy(gameObject);
    }
}