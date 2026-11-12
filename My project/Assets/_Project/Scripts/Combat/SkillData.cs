using UnityEngine;

public enum TipoExecucaoSkill
{
    GolpeConeMelee,
    AreaAoRedor,
    DisparoProjetil,
    DashPosicionamento
}

public enum AtributoEscala
{
    Forca,
    Agilidade,
    Inteligencia
}

[CreateAssetMenu(fileName = "NovaSkill", menuName = "Combate/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identificação")]
    public string nomeSkill = "Nova Habilidade";
    [TextArea(2, 4)] public string descricao;

    [Header("Custos e Tempos")]
    public int custoMana = 10;
    public float cooldown = 3f;

    [Header("Comportamento e Dano")]
    public TipoExecucaoSkill tipoExecucao = TipoExecucaoSkill.GolpeConeMelee;
    public AtributoEscala atributoEscala = AtributoEscala.Forca;
    public int danoBase = 40;
    public int bonusPorPontoAtributo = 4;
    public float alcanceOuRaio = 4f;
    public float anguloCone = 140f;

    [Header("Se for Projétil ou Dash")]
    public GameObject prefabProjetil;
    public float velocidadeProjetil = 18f;
    public float distanciaDash = 5f;

    [Header("Sinergia com Prey & Wounds")]
    [Tooltip("Marca o alvo atingido como Prey?")]
    public bool aplicaPrey = false;
    [Tooltip("Quantos stacks de Wounds esta skill aplica ao acertar?")]
    public int woundsParaAplicar = 1;
    [Tooltip("Consome todos os Wounds ativos no alvo para causar dano de Ruptura?")]
    public bool consomeWoundsParaExplodir = false;
}