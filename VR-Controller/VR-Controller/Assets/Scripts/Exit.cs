using UnityEngine;

/** Класс для выхода из приложения
 */

public class Exit : MonoBehaviour
{
    // Выход из приложения по нажатию кнопки
    public void OnMouseUp()
    {
        Application.Quit();
    }
}
