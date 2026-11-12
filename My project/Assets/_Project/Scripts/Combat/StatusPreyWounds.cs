using Unity.Netcode;
using UnityEngine;

public class StatusPreyWounds : NetworkBehaviour
{
    [SerializeField] private PreyWoundsData config;

    public NetworkVariable<bool> estaMarcadoPrey = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> stacksWounds = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float timerExpirarPrey;
    private float timerExpirarWounds;
    private NetworkHealth meuHealth;

    private void Awake()
    {
        meuHealth = GetComponent<NetworkHealth>();
    }

    private void Update()
    {
        if (!IsServer) return;
        if (meuHealth != null && meuHealth.estaMorto.Value) return;

        // Contagem regressiva da marca Prey
        if (estaMarcadoPrey.Value)
        {
            timerExpirarPrey -= Time.deltaTime;
            if (timerExpirarPrey <= 0f)
            {
                estaMarcadoPrey.Value = false;
                Debug.Log($"[Prey & Wounds] Marca Prey expirou em {gameObject.name}.");
            }
        }

        // Contagem regressiva dos Wounds
        if (stacksWounds.Value > 0)
        {
            timerExpirarWounds -= Time.deltaTime;
            if (timerExpirarWounds <= 0f)
            {
                stacksWounds.Value = 0;
                Debug.Log($"[Prey & Wounds] Ferimentos cicatrizaram em {gameObject.name}.");
            }
        }
    }

    public void AplicarPrey()
    {
        if (!IsServer || config == null) return;
        if (meuHealth != null && meuHealth.estaMorto.Value) return;

        estaMarcadoPrey.Value = true;
        timerExpirarPrey = config.duracaoPrey;
        Debug.Log($"[Prey & Wounds] {gameObject.name} foi marcado como PREY!");
    }

    public void AdicionarWounds(int quantidade)
    {
        if (!IsServer || config == null || quantidade <= 0) return;
        if (meuHealth != null && meuHealth.estaMorto.Value) return;

        stacksWounds.Value = Mathf.Clamp(stacksWounds.Value + quantidade, 0, config.maxStacksWounds);
        timerExpirarWounds = config.duracaoWounds;

        Debug.Log($"[Prey & Wounds] {gameObject.name} está com {stacksWounds.Value}/{config.maxStacksWounds} Wounds!");

        if (config.estourarAoAtingirMaximo && stacksWounds.Value >= config.maxStacksWounds)
        {
            ConsumirTodosWounds();
        }
    }

    public int ConsumirTodosWounds()
    {
        if (!IsServer || config == null || stacksWounds.Value <= 0) return 0;
        if (meuHealth != null && meuHealth.estaMorto.Value) return 0;

        int stacksAtuais = stacksWounds.Value;
        int danoRuptura = stacksAtuais * config.danoRupturaPorStack;

        if (estaMarcadoPrey.Value)
        {
            danoRuptura = Mathf.RoundToInt(danoRuptura * config.multiplicadorDanoPrey);
        }

        stacksWounds.Value = 0;
        timerExpirarWounds = 0f;

        if (meuHealth != null)
        {
            meuHealth.TomarDano(danoRuptura);
        }

        Debug.Log($"[RUPTURA!] {stacksAtuais} Wounds estourados em {gameObject.name} causando {danoRuptura} de dano extra!");
        return danoRuptura;
    }

    public int CalcularDanoModificado(int danoBaseAtaque)
    {
        if (config == null) return danoBaseAtaque;

        float danoFinal = danoBaseAtaque + (stacksWounds.Value * config.danoBonusPorWound);

        if (estaMarcadoPrey.Value)
        {
            danoFinal *= config.multiplicadorDanoPrey;
        }

        return Mathf.RoundToInt(danoFinal);
    }
}