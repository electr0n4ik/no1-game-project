using System.Collections.Generic;
using Rubilovo.Logic;
using UnityEngine;

[DefaultExecutionOrder(-60)]
public class LevelUpController : MonoBehaviour
{
    public static LevelUpController Instance { get; private set; }

    [SerializeField] private WeaponLoadout weapons;
    [SerializeField] private PassiveEffects effects;
    [SerializeField] private PlayerGrowth growth;

    private readonly System.Random rng = new();
    private readonly Queue<int> silentCards = new();
    private bool offering;

    public event System.Action<List<UpgradeCard>> OnCardsOffered;
    public event System.Action OnLoadoutChangedSilently;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (weapons == null) weapons = GetComponent<WeaponLoadout>();
        if (effects == null) effects = GetComponent<PassiveEffects>();
        if (growth == null) growth = GetComponent<PlayerGrowth>();
        weapons.Equip(WeaponId.Blades);
        growth.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (growth != null) growth.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int _)
    {
        List<UpgradeCard> cards = UpgradeDeck.OfferThree(weapons.State, rng, SaveSystem.UnlockedWeaponsMask());
        if (cards.Count == 0) return;
        offering = true;
        Time.timeScale = 0f;
        OnCardsOffered?.Invoke(cards);
    }

    public void Choose(UpgradeCard card)
    {
        Apply(card);
        if (offering)
        {
            offering = false;
            if (GameManager.Instance.State == GameState.Playing) Time.timeScale = 1f;
        }
        ProcessSilentQueue();
    }

    public void OpenSmallChest() => silentCards.Enqueue(1);

    public void OpenBigChest()
    {
        if (!TryEvolve()) silentCards.Enqueue(2);
    }

    private bool TryEvolve()
    {
        for (int w = 0; w < 6; w++)
        {
            var id = (WeaponId)w;
            if (!weapons.IsEquipped(id)) continue;
            if (weapons.State.WeaponLevels[w] != WeaponsCatalog.MaxLevel) continue;
            if (!Evolutions.TryFind(id, out Evolutions.Recipe recipe)) continue;
            if (effects.GetLevel(recipe.RequiredPassive) < GameBalance.Evo_ReqPassiveLvl) continue;
            weapons.Evolve(id);
            OnLoadoutChangedSilently?.Invoke();
            return true;
        }
        return false;
    }

    private void Update()
    {
        ProcessSilentQueue();
    }

    private void ProcessSilentQueue()
    {
        while (silentCards.Count > 0)
        {
            int take = Mathf.Min(silentCards.Dequeue(), 3);
            List<UpgradeCard> cards = UpgradeDeck.OfferThree(weapons.State, rng, SaveSystem.UnlockedWeaponsMask());
            for (int i = 0; i < take && i < cards.Count; i++)
                Apply(cards[i]);
            OnLoadoutChangedSilently?.Invoke();
        }
    }

    private void Apply(UpgradeCard card)
    {
        switch (card.Type)
        {
            case CardType.NewWeapon:
                weapons.Equip(card.Weapon);
                break;
            case CardType.UpgradeWeapon:
                weapons.LevelUp(card.Weapon);
                break;
            case CardType.Passive:
                if (weapons.State.PassiveLevels[(int)card.Passive] == 0)
                    weapons.State.PassiveCount++;
                weapons.State.PassiveLevels[(int)card.Passive]++;
                effects.Raise(card.Passive);
                if (card.Passive == PassiveId.Vitality)
                    GetComponent<PlayerController>()?.RefreshMaxHp();
                break;
        }
    }
}
