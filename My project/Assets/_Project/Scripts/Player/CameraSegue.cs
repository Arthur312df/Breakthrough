using Unity.Netcode;
using UnityEngine;

public class CameraSegue : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float velocidadeInterpolacao = 5f;

    private Transform alvoPlayer;

    private void LateUpdate()
    {
        // Se ainda não achou o player local, continua procurando
        if (alvoPlayer == null)
        {
            LocalizarPlayerLocal();
            return;
        }

        // Segue o player local suavemente
        Vector3 posicaoDesejada = alvoPlayer.position + offset;
        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, velocidadeInterpolacao * Time.deltaTime);
    }

    private void LocalizarPlayerLocal()
    {
        // Varre todos os NetworkObjects ativos na cena para achar o SEU player
        foreach (var networkObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            // O segredo está aqui: IsOwner garante que é o player daSTA tela específica (seja na Build ou no Editor)
            if (networkObj.IsOwner && networkObj.CompareTag("Player"))
            {
                alvoPlayer = networkObj.transform;
                Debug.Log($"[Camera] Alvo local encontrado com sucesso para o Client ID: {networkObj.OwnerClientId}");
                break;
            }
        }
    }
}