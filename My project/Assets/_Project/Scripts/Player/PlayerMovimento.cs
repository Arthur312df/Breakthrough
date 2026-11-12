using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovimento : NetworkBehaviour
{
    [SerializeField] private float velocidade = 6f;

    private CharacterController controle;
    private float velocidadeVertical;
    private NetworkHealth networkHealth;

    private void Awake()
    {
        controle = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TryGetComponent(out networkHealth);

        Debug.Log($"[Player] Nasceu: {name} | Dono: {OwnerClientId} | Sou o dono: {IsOwner}");

        // Cada jogador nasce em um lugar diferente para não ficarem um dentro do outro
        if (IsOwner)
        {
            controle.enabled = false;
            transform.position = new Vector3(OwnerClientId * 2f, 1f, 0f);
            controle.enabled = true;
        }
    }

    private void Update()
    {
        // Só o dono deste personagem controla ele
        if (!IsOwner) return;

        // Se houver o componente NetworkHealth e ele estiver morto, impede de andar imediatamente
        if (networkHealth != null && networkHealth.estaMorto.Value)
        {
            return; // Para a execução do movimento aqui antes de processar qualquer tecla
        }

        Vector2 entrada = LerEntrada();
        Vector3 direcao = new Vector3(entrada.x, 0f, entrada.y);

        // Gravidade simples
        if (controle.isGrounded)
            velocidadeVertical = -1f;
        else
            velocidadeVertical += Physics.gravity.y * Time.deltaTime;

        Vector3 movimento = direcao * velocidade;
        movimento.y = velocidadeVertical;

        controle.Move(movimento * Time.deltaTime);

        // Vira o personagem para a direção que está andando
        if (direcao.sqrMagnitude > 0.01f)
            transform.forward = direcao;
    }

    private Vector2 LerEntrada()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (teclado.wKey.isPressed) y += 1f;
        if (teclado.sKey.isPressed) y -= 1f;
        if (teclado.dKey.isPressed) x += 1f;
        if (teclado.aKey.isPressed) x -= 1f;

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }
}