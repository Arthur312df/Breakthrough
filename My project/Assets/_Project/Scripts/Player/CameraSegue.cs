using Unity.Netcode;
using UnityEngine;

public class CameraSegue : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float velocidadeInterpolacao = 5f;
    [SerializeField] private float intervaloBusca = 0.25f;

    private Transform alvoPlayer;
    private float proximaBusca;

    private void LateUpdate()
    {
        // Ainda não achou o player local: procura algumas vezes por segundo (não todo frame)
        if (alvoPlayer == null)
        {
            if (Time.time >= proximaBusca)
            {
                proximaBusca = Time.time + intervaloBusca;
                alvoPlayer = ProcurarPlayerLocal();

                // Ao achar, já posiciona a câmera sem "voar" até lá
                if (alvoPlayer != null)
                {
                    transform.position = alvoPlayer.position + offset;
                }
            }
            return;
        }

        // Suavização que não muda com o FPS
        Vector3 posicaoDesejada = alvoPlayer.position + offset;
        float fator = 1f - Mathf.Exp(-velocidadeInterpolacao * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, fator);
    }

    private Transform ProcurarPlayerLocal()
    {
        NetworkManager rede = NetworkManager.Singleton;
        if (rede == null || !rede.IsListening || rede.SpawnManager == null) return null;

        // O Netcode já sabe qual é o player deste computador
        NetworkObject jogador = rede.SpawnManager.GetLocalPlayerObject();
        return jogador != null ? jogador.transform : null;
    }
}
