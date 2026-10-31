using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public GameObject prefab;   // префаб героя для игры

    [Header("Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 5f;
    public int damage = 10;
}