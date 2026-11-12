using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSegue : MonoBehaviour
{
    [Header("Configuração Top-Down Isométrica")]
    [SerializeField] private float anguloInclinacao = 56f;
    [SerializeField] private float campoDeVisaoFOV = 40f;
    [SerializeField] private float distanciaDoAlvo = 19f; // Equivalente à distância de (0, 16, -10), mas respeitando os 56° exatos

    private Transform alvo;
    private Camera cam;
    private Vector3 offsetCalculado;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        AplicarConfiguracaoIsometrica();

        // Escuta o evento de quando o Player local nasce na rede (Zero buscas pesadas no LateUpdate!)
        NetworkHealth.OnPlayerLocalSpawnou += DefinirAlvoLocal;
    }

    private void Start()
    {
        // Fallback: Caso o Player já tenha nascido antes da câmera inicializar
        if (alvo == null && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient)
        {
            var localPlayer = Unity.Netcode.NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                alvo = localPlayer.transform;
            }
        }
    }

    private void OnDestroy()
    {
        NetworkHealth.OnPlayerLocalSpawnou -= DefinirAlvoLocal;
    }

    private void DefinirAlvoLocal(NetworkHealth playerHealth)
    {
        alvo = playerHealth.transform;
    }

    private void AplicarConfiguracaoIsometrica()
    {
        if (cam != null)
        {
            cam.fieldOfView = campoDeVisaoFOV;
        }

        // Trava a inclinação exata em 56 graus olhando de cima para baixo
        transform.rotation = Quaternion.Euler(anguloInclinacao, 0f, 0f);

        // Calcula o offset exato na linha de visão de 56 graus para manter o player centralizado
        offsetCalculado = -transform.forward * distanciaDoAlvo;
    }

    private void LateUpdate()
    {
        if (alvo == null) return;

        // Movimentação firme e direta mantendo os 56 graus cravados
        transform.position = alvo.position + offsetCalculado;
    }

#if UNITY_EDITOR
    // Atualiza a visão em tempo real se você ajustar valores no Inspector com o jogo rodando
    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        AplicarConfiguracaoIsometrica();
    }
#endif
}