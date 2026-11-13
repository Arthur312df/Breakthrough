using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovimento : NetworkBehaviour
{
    [SerializeField] private float velocidade = 6f;

    private CharacterController controle;
    private NetworkTransform networkTransform;
    private NetworkHealth networkHealth;
    private float velocidadeVertical;

    private void Awake()
    {
        // Cache de todos os componentes uma única vez
        controle = GetComponent<CharacterController>();
        networkTransform = GetComponent<NetworkTransform>();
        networkHealth = GetComponent<NetworkHealth>();
    }

    // A posição inicial agora é decidida só pelo GerenciadorSpawn.
    // (Antes, este script teleportava todo mundo para (clientId * 2, 1, 0) e ignorava o ponto de spawn.)

    private void Update()
    {
        if (!IsOwner) return;

        // Se estiver morto, bloqueia o movimento imediatamente
        if (networkHealth != null && networkHealth.estaMorto.Value) return;

        Vector2 entrada = LerEntrada();
        Vector3 direcao = new Vector3(entrada.x, 0f, entrada.y);

        if (controle.isGrounded)
            velocidadeVertical = -1f;
        else
            velocidadeVertical += Physics.gravity.y * Time.deltaTime;

        Vector3 movimento = direcao * velocidade;
        movimento.y = velocidadeVertical;

        controle.Move(movimento * Time.deltaTime);

        if (direcao.sqrMagnitude > 0.01f)
            transform.forward = direcao;
    }

    // Método centralizado para teletransportar sem conflito com CharacterController ou NetworkTransform
    public void Teleportar(Vector3 novaPosicao)
    {
        if (controle != null) controle.enabled = false;

        velocidadeVertical = 0f;
        transform.position = novaPosicao;

        if (networkTransform != null && networkTransform.CanCommitToTransform)
        {
            networkTransform.Teleport(novaPosicao, transform.rotation, transform.localScale);
        }

        if (controle != null) controle.enabled = true;
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
