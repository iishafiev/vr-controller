using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Класс для обеспечения сопряжения между устройствами и пересылки сообщений
public class NetManager : MonoBehaviour
{
    // Переменные для сопряжения устройств и создания "клиента"
    private byte dataChanel;
    private int hostId;
    private int connId;
    private const int MAX_USER = 200;
    private const int SERVER_PORT = 55555;
    private const int CLIENT_PORT = 55556;
    private const int MAX_MSG_BUFF_SIZE = 1024;
    private byte error;
    // Видеоплеер 
    private VideoManager vMan;
    // Состояния подключенности устройстваа
    public  bool isConnected = false;
    private bool isReadyToConnect = false;
    public SetPlayerState initState;
    // Уровень заряда аккумулятора устройства
    private float battery;

    // Адрес и порт сервера
    public string serverAddress;
    public int serverPort;
    // Название воспроизводимого видеофайла
    private string vidFile;
    
    // Вызывается до первого фрейма сцены
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Init();
        battery = SystemInfo.batteryLevel;
    }

    // Инициализация клиента и начало трансляции
    public void Init()
    {
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();
        dataChanel = cc.AddChannel(QosType.Reliable);
        HostTopology topo = new HostTopology(cc, MAX_USER);

        hostId = NetworkTransport.AddHost(topo, CLIENT_PORT, null);

        NetworkTransport.SetBroadcastCredentials(hostId, Globals.key, Globals.version, Globals.subversion, out error);
    }

    // Сканирование сети на наличие входящих сообщений и их обработка
    public void Update()
    {
        int chanellId;
        byte[] buff = new byte[MAX_MSG_BUFF_SIZE];
        int msgSize = 0;
        int inConId;
        NetworkEventType evnt = NetworkTransport.ReceiveFromHost(hostId, out inConId, out chanellId, buff, MAX_MSG_BUFF_SIZE, out msgSize, out error);
        switch (evnt)
        {
            case NetworkEventType.BroadcastEvent:
                if (!isReadyToConnect)
                {
                    NetworkTransport.GetBroadcastConnectionInfo(hostId, out serverAddress, out serverPort, out error);
                    //Debug.Log(string.Format("Debug: address - {0}; port - {1}", serverAddress, serverPort));
                    isReadyToConnect = true;
                    connId = NetworkTransport.Connect(hostId, serverAddress, serverPort, 0, out error);
                }
                break;
            case NetworkEventType.DataEvent:
                PlayerMessage msg;
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(buff);
                msg = (PlayerMessage)bf.Deserialize(ms);
                handleData(msg);
                break;
            case NetworkEventType.ConnectEvent:
                //Debug.Log(string.Format("Debug: ConnectEvent hostId - {0}, connId - {1}", hostId, connId));
                isConnected = true;
                NetworkTransport.SetBroadcastCredentials(hostId, 0, 0, 0, out error);
                sendDeviceInfo();
                break;
            case NetworkEventType.DisconnectEvent:
                //Debug.Log(string.Format("Debug: DisconnectEvent hostId - {0}, connId - {1}", hostId, connId));
                isConnected = false;
                isReadyToConnect = false;
                NetworkTransport.SetBroadcastCredentials(hostId, Globals.key, Globals.version, Globals.subversion, out error);
                vMan.Stop();
                break;
            default:
                //Debug.Log(string.Format("Debug: network event - {0}", evnt));
                break;
        }
        if (SystemInfo.batteryLevel < battery - 10)
            sendDeviceInfo();
    }

    // Посылка сообщения серверу
    public void sendServer(PlayerMessage msg)
    {
        byte[] buff = new byte[MAX_MSG_BUFF_SIZE];
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buff);
        bf.Serialize(ms, msg);

        NetworkTransport.Send(hostId, connId, dataChanel, buff, MAX_MSG_BUFF_SIZE, out error);
    }
    
    // Определение типа полученного сообщения 
    private void handleData(PlayerMessage msg)
    {
        switch (msg.type)
        {
            case Globals.msgRequestInfo:
                sendDeviceInfo();
                break;
            case Globals.msgRequestPlayerState:
                onRequestPlayerState();
                break;
            case Globals.msgSetPlayerState:
                onSetPlayerState((SetPlayerState)msg);
                break;
        }
    }

    // Посылка сообщений о состоянии устройства
    public void sendDeviceInfo()
    {
        DeviceInfo info = new DeviceInfo();
        info.name = SystemInfo.deviceName;
        info.battery = SystemInfo.batteryLevel;
        var files = Directory.GetFiles("/mnt/sdcard/Movies/");
        string s = "Видеофайлы на устройстве:\n";
        string subs = "";
        foreach (var f in files)
           // subs = f.Remove(0, f.LastIndexOf('/') + 1);
        
            s += (f.Remove(0, f.LastIndexOf('/') + 1) + ";    ");
        info.id = s; 

        sendServer(info);

    }

    // Посылка сообщения о состоянии плеера
    private void onRequestPlayerState()
    {
        //Debug.Log(string.Format("Debug: onRequestPlayerState"));
        PlayerState ps;
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            GameObject CameraObject = GameObject.Find("Main Camera");
            VideoManager vManager = CameraObject.GetComponent<VideoManager>();
            ps = vManager.getPlayerState();
        }
        else
        {

            ps = new PlayerState();
            ps.videoFile = "";
            ps.frame = 0;
            ps.frameCount = 0;
            ps.frameRate = 0;
            ps.volume = 1f;
        }
        sendServer(ps);
    }

    // Посылка сообщения об установленном состоянии плеера
    private void onSetPlayerState(SetPlayerState msg)
    {
        //Debug.Log(string.Format("Debug: onSetPlayerState  state - {0}, filename - {1}, progress - {2}", msg.state, msg.videoFile, msg.frame));
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            GameObject CameraObject = GameObject.Find("Main Camera");
            VideoManager vManager = CameraObject.GetComponent<VideoManager>();
            if (msg.state != Globals.psStopped)
            {
                
                vManager.setPlayerState(msg);
                if (VideoManager.videoFile != vidFile)
                {
                    vidFile = msg.videoFile;
                    onRequestPlayerState();
                }
                
            }
            else
            {
                vManager.setPlayerState(msg);
                SceneManager.LoadScene(0);
            }
        }
        else
        {
            if (msg.state != Globals.psStopped)
            {
                initState = msg;
                SceneManager.LoadScene(1);
            }
        }
        
    }
}
