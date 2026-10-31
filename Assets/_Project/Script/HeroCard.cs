using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroCard : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] Button button;

    public void Setup(CharacterData data, Action<CharacterData> onClick)
    {
        icon.sprite = data.icon;
        nameText.text = data.displayName;
        button.onClick.AddListener(() => onClick(data));
    }
}