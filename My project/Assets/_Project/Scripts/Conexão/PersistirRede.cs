using UnityEngine;

// Coloque este script no objeto do NetworkManager.
// Se voltar para o menu e outro NetworkManager existir, a cópia nova é destruída.
public class PersistirRede : MonoBehaviour
{
    private static PersistirRede instancia;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }
}
