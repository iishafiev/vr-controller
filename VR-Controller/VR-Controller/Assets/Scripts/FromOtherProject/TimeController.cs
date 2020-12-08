using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/** Класс для управления воспроизведением видеофайлов с пересылкой сообщений
 */
public class TimeController : MonoBehaviour
{
    // Слайдеры управления прогрессом видео и громкостью
    public Slider tracking;
    public Slider sound;
    // Кнопки воспроизведения, паузы, остановки и сохранения названия видеофайла
    public Button playBtn;
    public Button pauseBtn;
    public Button stopBtn;
    public Button saveBtn;
    // Список названий видеофайлов
    public Dropdown library;
    // Текстовое поле прогресса воспроизведения видео
    public Text videoProgressText;
    // Воле ввода названия видео для добавления в список
    public InputField nameField;
    // Сетевой компонент
    private NetManager netManager;
    // Состояние плеера
    public int state = 0;//состояние плейера 0 - stopped, 1 - playing, 2 - paused

    // Инициализация сетевого компонента при запуске скрипта
    void Start()
    {
        GameObject NetworkObject = GameObject.Find("NetworkObject");
        netManager = NetworkObject.GetComponent<NetManager>();
    }
    // Корутина для изменения значения слайдера прогресса видео
    IEnumerator counter()
    {
        while (state == 1)
        {
            tracking.value += netManager.frameRate;
            yield return new WaitForSeconds(1f);
        }
    }

    void Update()
    {
        // Динамическое изменение текстового поля с прогрессом воспроизведения видео
        videoProgressText.text = ((int)(tracking.value/netManager.frameRate / 60)).ToString() + ":" + ((int)((tracking.value / netManager.frameRate) % 60)).ToString() + " / " +
            ((int)(tracking.maxValue / netManager.frameRate / 60)).ToString() + ":" + ((int)((tracking.maxValue / netManager.frameRate) % 60)).ToString();
    }
    // Действия по нажатию кнопки воспроизведения
    public void OnPlay()
    {
        if (state != Globals.psPlaying) {
            state = Globals.psPlaying;
            //Отправка команды по сети           
            SetPlayerState ps = new SetPlayerState();
            ps.state = Globals.psPlaying;
            ps.videoFile = library.options[Int32.Parse(library.value.ToString())].text; 
            ps.frame = (long)tracking.value;
            ps.volume = sound.value;
            netManager.sendToAll(ps);
            if(library.options[Int32.Parse(library.value.ToString())].text != "")
                StartCoroutine(counter());
        }
        
    }
    // Действия по нажатию кнопки паузы
    public void OnPause()
    {
        if (state == Globals.psPlaying)
        {
            state = Globals.psPaused;
            //Отправка команды по сети
            SetPlayerState ps = new SetPlayerState();
            ps.state = Globals.psPaused;
            ps.videoFile = library.options[Int32.Parse(library.value.ToString())].text;
            ps.frame = (long)tracking.value;
            ps.volume = sound.value;
            netManager.sendToAll(ps);
            StopCoroutine(counter());
        }
    }
    // Действия по нажатию кнопки остановки
    public void OnStop()
    {
        state = Globals.psStopped;
        //Отправка команды по сети
        SetPlayerState ps = new SetPlayerState();
        ps.state = Globals.psStopped;
        ps.videoFile = library.options[Int32.Parse(library.value.ToString())].text;
        ps.frame = 0;
        ps.volume = 1f;
        netManager.sendToAll(ps);
        StopCoroutine(counter());
        tracking.value = 0;
        sound.value = 100;
        
    }
    // Действия по нажатию кнопки добавления нового видео в список
    public void OnSave()
   {
        // Проверка на наличие видео с таким названием в списке
        bool newVidBool = true;
        Dropdown.OptionData newVid = new Dropdown.OptionData(nameField.text);
        foreach (var opt in library.options)
            if (opt.text == nameField.text)
                newVidBool = false;

        if(newVidBool && (nameField.text != ""))
            library.options.Add(newVid);

        OnStop();
    }
}
