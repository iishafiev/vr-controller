using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/** Класс для управления видеоплеером
 */
public class VideoManager : MonoBehaviour
{
    // Видеоплеер и название воспроизводимого видео
    private VideoPlayer vPlayer;
    public static string videoFile = "";

    // Состояния плеера
    private Queue<PlayerState> playerStates = new Queue<PlayerState>();

    // Сетевой компонент
    private NetManager netManager;

    // Инициализация полей на старте
    void Start()
    {
        vPlayer = GetComponent<VideoPlayer>();
        GameObject NetworkObject = GameObject.Find("NetworkObject");
        netManager = NetworkObject.GetComponent<NetManager>();
        setPlayerState(netManager.initState);
    }

    // Остановка плеера по завершении видео
    void onVideoEnded(VideoPlayer vp)
    {
        //Debug.Log("Debug: onVideoEnded ");
        vp.Stop();
        SceneManager.LoadScene(0);
    }

    // Динамическое отслеживание подключенности плеера и его остановка при отключении
    void Update()
    {
        try
        {
            if (playerStates.Count > 0)
            {
                PlayerState ps = playerStates.Dequeue();
                setPlayerState(ps);
            }

        }
        catch (Exception e)
        {
            //Debug.Log("Debug: exception: " + e.Message);
        }
        if (!netManager.isConnected)
        {
            Stop();
            setTime(0);
            setSound(1);
            videoFile = "";
        }
        //Debug.Log("Debug: vPlayer.time - " + vPlayer.time);


    }
    // При получении состояния плеера
    public void addPlayerState(PlayerState ps)
    {
        playerStates.Enqueue(ps);
    }

    // Воспроизведение видео
    public void Play()
    {
        //Debug.Log("Debug: " + vPlayer.url + " - " + vPlayer.isPrepared);
        if (vPlayer.isPrepared)
        {
            vPlayer.Play();
            //Debug.Log("Debug - UDP message: Play1");
        }

        else
        {
            try
            {
                vPlayer.prepareCompleted += VPlayer_prepareCompleted;
                vPlayer.errorReceived += VideoPlayer_errorReceived;
                vPlayer.Prepare();
            }
            catch (Exception e)
            {
                //Debug.Log("Debug: Exception " + e.Message);
            }
        }
    }
    // Пауза воспроизводимого видео
    public void Pause()
    {
        vPlayer.Pause();
    }

    // Остановка воспроизводимого видео
    public void Stop()
    {
        vPlayer.Stop();
    }

    // Установка текущего кадра видео при изменении значения слайдера прогресса видео
    public void setTime(long frame)
    {
        vPlayer.frame = frame;
    }

    // Установка громкости звука
    public void setSound(float volume)
    {
        vPlayer.SetDirectAudioVolume(0, (volume/100f));
    }

    // Получение состояния плеера
    public int getState()
    {
        int state = Globals.psStopped;

        if (vPlayer.isPlaying) state = Globals.psPlaying;
        if (vPlayer.isPaused) state = Globals.psPaused;

        return state;
    }

    // Выбор видео для воспроизведения
    public void selectVideo(string url)
    {
        vPlayer.source = VideoSource.Url;
        vPlayer.url = url;
    }

    // Подготовка видео к воспроизведению
    private void VPlayer_prepareCompleted(VideoPlayer source)
    {
        vPlayer.prepareCompleted += VPlayer_prepareCompleted;
        //Debug.Log("Debug - UDP message: VPlayer_prepareCompleted");
        vPlayer.Play();

    }

    // Обработка ошибок при подготовке видео
    private void VideoPlayer_errorReceived(VideoPlayer source, string message)
    {
        //Debug.Log("Debug - UDP message: videoPlayer_errorReceived");
        //Debug.Log("Debug - UDP message: " + message);
        vPlayer.errorReceived -= VideoPlayer_errorReceived;//Unregister to avoid memory leaks
    }

    // Получение данных о воспроизводимом видео
    public PlayerState getPlayerState()
    {
        PlayerState ps = new PlayerState();
        ps.frame = vPlayer.frame;
        ps.state = getState();
        ps.videoFile = videoFile;
        ps.frameCount = vPlayer.frameCount;
        ps.frameRate = vPlayer.frameRate;
        ps.volume = vPlayer.GetDirectAudioVolume(0);
        return ps;
    }

    // Установка состояния плеера согласно полученному сообщению от сервера
    public void setPlayerState(PlayerState ps)
    { 
        switch (ps.state)
        {
            case Globals.psStopped:
                videoFile = ps.videoFile;
                setTime(0);
                setSound(ps.volume);
                Stop();
                break;
            case Globals.psPlaying:
                if (!videoFile.Equals(ps.videoFile))
                {
                    vPlayer.Stop(); 
                    videoFile = ps.videoFile;
                    selectVideo("/mnt/sdcard/Movies/"
                        + videoFile);
                    Debug.Log(videoFile);
                }
                
                setTime(ps.frame);
                setSound(ps.volume);
                Play();
                break;
            case Globals.psPaused:
                Pause(); 
                setTime(ps.frame);
                setSound(ps.volume);
                Pause();
                break;
        }
    }
}
