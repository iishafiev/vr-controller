using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/** Класс для управления звуком и прогрессом видео
 */
public class VideoSlider : MonoBehaviour,  IPointerDownHandler, IDragHandler
{ 
    //Слайдеры прогресса видео и громкости
    public Slider videoTracking;
    public Slider soundTracking;
    
    //Список видео для воспроизведения
    public Dropdown library;

    //Контроллер воспроизведения и сетевой компонент 
    private TimeController tc;
    private NetManager netManager;

    void Start()
    {
        // Инициализация сетевого компонента
        GameObject NetworkObject = GameObject.Find("NetworkObject");
        netManager = NetworkObject.GetComponent<NetManager>();
    }

    // При изменении значения слайдера перепрыгнуть к соответствующему кадру видео
    public void OnDrag(PointerEventData eventData)
    {
        ScipToFrame();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ScipToFrame();
    }

    // Переход к нужному кадру видео
    private void ScipToFrame()
    {
        // Остановить воспроизведение
        tc.OnPause(); 
        // Передать плееру информацию
        SetPlayerState ps = new SetPlayerState();
        ps.state = Globals.psPaused;
        ps.videoFile = library.options[Int32.Parse(library.value.ToString())].text;
        ps.frame = (long)videoTracking.value;
        ps.volume = soundTracking.value;  
        netManager.sendToAll(ps);
    }
}
