using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSegue : MonoBehaviour
{
    [Header("Configuração Top-Down Isométrica")]
    [SerializeField] private float anguloInclinacao = 56f;
    [SerializeField] private float campoDeVisaoFOV = 40f;
    [SerializeField] private float distanciaDoAlvo = 19f;

    private Transform alvo;
    private Camera cam;
    private Vector3 offsetCalculado;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        AplicarConfiguracaoIsometrica();
    }

    private void Start()
    {
        LocalizarPlayer();
    }

    private void LocalizarPlayer()
    {
        if (alvo != null) return;

        // Procura diretamente pelo objeto que possui a Tag "Player" na cena
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            alvo = playerObj.transform;
            Debug.Log($"[Camera] Alvo encontrado com sucesso pela Tag: {alvo.name}");
        }
    }

    private void AplicarConfiguracaoIsometrica()
    {
        if (cam != null)
        {
            cam.fieldOfView = campoDeVisaoFOV;
        }

        transform.rotation = Quaternion.Euler(anguloInclinacao, 0f, 0f);
        offsetCalculado = -transform.forward * distanciaDoAlvo;
    }

    private void LateUpdate()
    {
        // Se ainda não encontrou, continua tentando a cada frame até o player nascer na cena
        if (alvo == null)
        {
            LocalizarPlayer();
            return;
        }

        // Segue o player perfeitamente
        transform.position = alvo.position + offsetCalculado;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        AplicarConfiguracaoIsometrica();
    }
#endif
}