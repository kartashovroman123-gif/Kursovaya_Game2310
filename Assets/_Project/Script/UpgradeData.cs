using System.Collections.Generic;
using UnityEngine;

// Усиление (бафф), полученное во время забега. Действует до конца забега.
// Личные усиления героя лежат в его CharacterData (поле Hero Upgrades),
// общие — в отдельном пуле (подключим, когда будет комната улучшений).
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Game/Upgrade")]
public class UpgradeData : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    public List<StatModifier> modifiers = new List<StatModifier>();
}
