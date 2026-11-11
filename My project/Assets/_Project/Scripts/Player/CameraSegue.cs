using Unity.Netcode;
using UnityEngine;

public class CameraSegue : MonoBehaviour
{
    [SerializeField] private Vector3 distancia = new Vector3(0f, 12f, -8f);
    [SerializeField] private float suavidade = 8f;

    private Transform alvo;
    private float proximoAviso;

    private void LateUpdate()
    {
        if (alvo == null)
            alvo = ProcurarAlvo();

        if (alvo == null)
        {
            if (Time.time > proximoAviso)
            {
                Debug.LogWarning("[Camera] Nenhum personagem seu encontrado para seguir.");
                proximoAviso = Time.time + 2f;
            }
            return;
        }

        Vector3 destino = alvo.position + distancia;
        transform.position = Vector3.Lerp(transform.position, destino, suavidade * Time.deltaTime);
    }

    private Transform ProcurarAlvo()
    {
        PlayerMovimento[] jogadores = FindObjectsByType<PlayerMovimento>();
        foreach (PlayerMovimento jogador in jogadores)
        {
            if (jogador.IsOwner)
                return jogador.transform;
        }
        return null;
    }
}