using UnityEngine;

// Всё, с чем герой взаимодействует клавишей (F): дверь на ключ, дверь босса, сундук, позже торговец, алтарь...
// Объекту нужен Collider2D (можно триггер) - по нему PlayerInteractor находит его рядом с героем.
public interface IInteractable
{
    // Можно ли сейчас взаимодействовать (если нет - значок клавиши не показывается)
    bool CanInteract(GameObject player);

    // Что происходит по нажатию
    void Interact(GameObject player);

    // Где показать значок клавиши
    Vector3 PromptPosition { get; }
}
