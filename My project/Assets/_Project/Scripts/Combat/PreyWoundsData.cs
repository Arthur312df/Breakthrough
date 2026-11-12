using UnityEngine;

[CreateAssetMenu(fileName = "NovoPreyWoundsConfig", menuName = "Combate/Prey & Wounds Data")]
public class PreyWoundsData : ScriptableObject
{
    [Header("Configuração de Prey (Marca da Presa)")]
    [Tooltip("Duração da marca de Presa no alvo em segundos")]
    public float duracaoPrey = 8f;
    [Tooltip("Multiplicador de dano extra quando o alvo está marcado como Prey (ex: 1.25 = +25% de dano)")]
    public float multiplicadorDanoPrey = 1.25f;

    [Header("Configuração de Wounds (Ferimentos)")]
    [Tooltip("Quantidade máxima de ferimentos acumuláveis no alvo")]
    public int maxStacksWounds = 5;
    [Tooltip("Tempo até os ferimentos cicatrizarem se o alvo parar de apanhar")]
    public float duracaoWounds = 6f;
    [Tooltip("Bônus de dano fixo por cada stack de Wound ativo no alvo")]
    public int danoBonusPorWound = 5;

    [Header("Ruptura / Estouro de Wounds")]
    [Tooltip("Dano base causado por cada stack quando uma Skill consome/estoura os Wounds")]
    public int danoRupturaPorStack = 35;
    [Tooltip("Se verdadeiro, atingir o máximo de stacks estoura os ferimentos automaticamente")]
    public bool estourarAoAtingirMaximo = false;
}