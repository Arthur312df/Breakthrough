using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkills : NetworkBehaviour
{
    [Header("As 5 Skills do Player (Slots 0 a 4)")]
    [SerializeField] private SkillData[] skillsEquipadas = new SkillData[5];

    private PlayerStats playerStats;
    private NetworkHealth networkHealth;
    private CharacterController characterController;
    private Camera camPrincipal;

    // Cooldowns individuais para os 5 slots (Local e Servidor)
    private readonly float[] proximoUsoCliente = new float[5];
    private readonly float[] proximoUsoServidor = new float[5];

    // Buffer sem alocação de memória RAM
    private readonly Collider[] bufferColisoes = new Collider[20];

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        networkHealth = GetComponent<NetworkHealth>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (networkHealth != null && networkHealth.estaMorto.Value) return;
        if (Keyboard.current == null) return;

        // Atalhos para as 5 Skills: Teclas 1, 2, 3, 4, 5 (ou Q, E, R, F, Botão Direito)
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) TentarUsarSkill(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame) TentarUsarSkill(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame) TentarUsarSkill(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame) TentarUsarSkill(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)) TentarUsarSkill(4);
    }

    private void TentarUsarSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= skillsEquipadas.Length) return;

        SkillData skill = skillsEquipadas[slotIndex];
        if (skill == null) return;

        // 1. Verifica Cooldown no Cliente
        if (Time.time < proximoUsoCliente[slotIndex])
        {
            float restante = proximoUsoCliente[slotIndex] - Time.time;
            Debug.Log($"[Skill] {skill.nomeSkill} em recarga ({restante:F1}s)");
            return;
        }

        // 2. Verifica Mana localmente antes de mandar pacote pra rede
        if (playerStats != null && playerStats.manaAtual.Value < skill.custoMana)
        {
            Debug.Log($"[Skill] Mana insuficiente para {skill.nomeSkill}!");
            return;
        }

        Vector3 direcaoMira = ObterDirecaoDoMouse();
        if (direcaoMira != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direcaoMira);
        }

        proximoUsoCliente[slotIndex] = Time.time + skill.cooldown;

        // Se for Dash, move no cliente dono imediatamente para resposta instantânea
        if (skill.tipoExecucao == TipoExecucaoSkill.DashPosicionamento && characterController != null)
        {
            characterController.Move(direcaoMira * skill.distanciaDash);
        }

        ExecutarSkillServerRpc(slotIndex, direcaoMira);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ExecutarSkillServerRpc(int slotIndex, Vector3 direcaoMira)
    {
        if (networkHealth != null && networkHealth.estaMorto.Value) return;
        if (slotIndex < 0 || slotIndex >= skillsEquipadas.Length) return;

        SkillData skill = skillsEquipadas[slotIndex];
        if (skill == null) return;

        // Valida Cooldown e Mana no Servidor
        if (Time.time < proximoUsoServidor[slotIndex]) return;
        if (playerStats != null && !playerStats.GastarMana(skill.custoMana)) return;

        proximoUsoServidor[slotIndex] = Time.time + skill.cooldown;

        direcaoMira.y = 0f;
        if (direcaoMira.sqrMagnitude < 0.01f) direcaoMira = transform.forward;
        direcaoMira.Normalize();

        int danoCalculado = CalcularDanoSkill(skill);

        switch (skill.tipoExecucao)
        {
            case TipoExecucaoSkill.GolpeConeMelee:
                ExecutarGolpeEmArea(skill, direcaoMira, danoCalculado, true);
                break;

            case TipoExecucaoSkill.AreaAoRedor:
                ExecutarGolpeEmArea(skill, direcaoMira, danoCalculado, false);
                break;

            case TipoExecucaoSkill.DisparoProjetil:
                DispararProjetilSkill(skill, direcaoMira, danoCalculado);
                break;

            case TipoExecucaoSkill.DashPosicionamento:
                if (IsServer && !IsOwner && characterController != null)
                {
                    characterController.Move(direcaoMira * skill.distanciaDash);
                }
                Debug.Log($"[Skill] Player {OwnerClientId} usou Dash: {skill.nomeSkill}");
                break;
        }
    }

    private int CalcularDanoSkill(SkillData skill)
    {
        if (playerStats == null) return skill.danoBase;

        int valorAtributo = 10;
        switch (skill.atributoEscala)
        {
            case AtributoEscala.Forca: valorAtributo = playerStats.forca.Value; break;
            case AtributoEscala.Agilidade: valorAtributo = playerStats.agilidade.Value; break;
            case AtributoEscala.Inteligencia: valorAtributo = playerStats.inteligencia.Value; break;
        }

        return skill.danoBase + ((valorAtributo - 10) * skill.bonusPorPontoAtributo);
    }

    private void ExecutarGolpeEmArea(SkillData skill, Vector3 direcaoMira, int danoSkill, bool usarConeFrontal)
    {
        int qtd = Physics.OverlapSphereNonAlloc(transform.position, skill.alcanceOuRaio, bufferColisoes);
        float cosLimite = Mathf.Cos((skill.anguloCone * 0.5f) * Mathf.Deg2Rad);

        for (int i = 0; i < qtd; i++)
        {
            Collider hit = bufferColisoes[i];
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<NetworkHealth>(out var alvoHealth))
            {
                if (alvoHealth.estaMorto.Value || !alvoHealth.EhMob) continue;

                if (usarConeFrontal)
                {
                    Vector3 proAlvo = hit.transform.position - transform.position;
                    proAlvo.y = 0f;
                    if (proAlvo.sqrMagnitude > 0.5f && Vector3.Dot(direcaoMira, proAlvo.normalized) < cosLimite)
                        continue;
                }

                AplicarDanoEEfeitosNoAlvo(alvoHealth, skill, danoSkill);
            }
        }
    }

    public static void AplicarDanoEEfeitosNoAlvo(NetworkHealth alvoHealth, SkillData skill, int danoBruto)
    {
        int danoFinal = danoBruto;

        if (alvoHealth.TryGetComponent<StatusPreyWounds>(out var statusPreyWounds))
        {
            // 1. Calcula o dano amplificado pelos stacks atuais de Prey & Wounds
            danoFinal = statusPreyWounds.CalcularDanoModificado(danoBruto);

            // 2. Aplica novas marcas de Prey ou stacks de Wounds
            if (skill != null)
            {
                if (skill.aplicaPrey) statusPreyWounds.AplicarPrey();
                if (skill.woundsParaAplicar > 0) statusPreyWounds.AdicionarWounds(skill.woundsParaAplicar);
            }
        }

        alvoHealth.TomarDano(danoFinal);

        // 3. Se for uma skill finalizadora/executora, explode os Wounds restantes!
        if (skill != null && skill.consomeWoundsParaExplodir && statusPreyWounds != null && !alvoHealth.estaMorto.Value)
        {
            statusPreyWounds.ConsumirTodosWounds();
        }
    }

    private void DispararProjetilSkill(SkillData skill, Vector3 direcao, int danoSkill)
    {
        Vector3 posOrigem = transform.position + direcao * 0.6f;
        InstanciarProjetilSkillLocal(skill, posOrigem, direcao, danoSkill, true);
        DispararProjetilSkillClientRpc( System.Array.IndexOf(skillsEquipadas, skill), posOrigem, direcao, danoSkill);
    }

    [ClientRpc]
    private void DispararProjetilSkillClientRpc(int slotIndex, Vector3 posOrigem, Vector3 direcao, int danoSkill)
    {
        if (IsServer) return;
        if (slotIndex < 0 || slotIndex >= skillsEquipadas.Length) return;
        InstanciarProjetilSkillLocal(skillsEquipadas[slotIndex], posOrigem, direcao, danoSkill, false);
    }

    private void InstanciarProjetilSkillLocal(SkillData skill, Vector3 posOrigem, Vector3 direcao, int danoSkill, bool ehServidor)
    {
        if (skill == null || skill.prefabProjetil == null) return;

        GameObject obj = Instantiate(skill.prefabProjetil, posOrigem, Quaternion.LookRotation(direcao));
        if (obj.TryGetComponent<ProjetilCombate>(out var proj))
        {
            proj.Inicializar(direcao, skill.velocidadeProjetil, danoSkill, true, ehServidor, OwnerClientId, skill);
        }
    }

    private Vector3 ObterDirecaoDoMouse()
    {
        if (camPrincipal == null) camPrincipal = Camera.main;
        if (camPrincipal == null || Mouse.current == null) return transform.forward;

        Ray raio = camPrincipal.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane planoChao = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (planoChao.Raycast(raio, out float dist))
        {
            Vector3 dir = raio.GetPoint(dist) - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
        }
        return transform.forward;
    }
}