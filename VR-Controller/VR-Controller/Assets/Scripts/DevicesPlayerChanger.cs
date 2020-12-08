using UnityEngine;
using UnityEngine.EventSystems;

/** Класс для перехода между основным окном приложения и плеером
 */
public class DevicesPlayerChanger : MonoBehaviour, ISelectHandler
{
    // Окно плеера
    public GameObject panel;

    // Активация окна плеера по нажатию кнопки
    public void OnSelect(BaseEventData eventData)
    {
        panel.SetActive(!panel.active);
    }
}
