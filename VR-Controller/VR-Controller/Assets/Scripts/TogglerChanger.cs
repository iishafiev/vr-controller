using UnityEngine;
using UnityEngine.UI;

/** Класс для изменения активности подключенного устройства и изменения данных о количестве активных и подключенных устройств
 */
public class TogglerChanger : MonoBehaviour
{
    // Индикатор активности подключенного устройства
    public Toggle toggle;

    // Текстовое поле для информации об активных и подключенных устройствах
    private Text numOfDevicesText;
    
    // Инициализация текстового поля при запуске скрипта
    void Start()
    {
        numOfDevicesText = GameObject.Find("NumOfDevices").GetComponent<Text>();
    }

    // Изменение текстового поля и состояния подключенного устройства в зависимости от значения индикатора
    public void OnToggleChange()
    {
        // Название текущего устройства и поиск его в списке
        int key = int.Parse(gameObject.name);
        NetManager.clientsDict[key] = toggle.isOn;
        // Изменение текстового поля
        if(toggle.isOn)
            NetManager.activeDevsNum++;
        else
            NetManager.activeDevsNum--;
        numOfDevicesText.text = "Активно " + NetManager.activeDevsNum.ToString() + " из " + (NetManager.clientsDict.Count).ToString() + " устройств";
    }
}
