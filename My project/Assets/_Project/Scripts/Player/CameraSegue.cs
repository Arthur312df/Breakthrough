using Unity.Netcode;
using UnityEngine;

public class CameraSegue : MonoBehaviour
{
    [SerializeField] private Vector3 distancia = new Vector3(0f, 16f, -10f);

    private Transform alvo;
    private float proximoAviso;
    private bool rotacaoDefinida = false;

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

        // Posiciona a câmera de forma firme e direta, sem Lerp
        transform.position = alvo.position + distancia;

        // Define a rotação fixa olhando para o player apenas uma vez quando encontra o alvo
        if (!rotacaoDefinida)
        {
            Vector3 direcao = alvo.position - transform.position;
            transform.rotation = Quaternion.LookRotation(direcao);
            rotacaoDefinida = true;
        }
    }

    private Transform ProcurarAlvo()
    {
        PlayerMovimento[] jogadores = FindObjectsByType<PlayerMovimento>(FindObjectsInactive.Exclude);
        foreach (PlayerMovimento jogador in jogadores)
        {
            if (jogador.IsOwner)
                return jogador.transform;
        }
        return null;
    }
}