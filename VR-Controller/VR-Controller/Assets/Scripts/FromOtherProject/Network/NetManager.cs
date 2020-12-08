using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Net.Sockets;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

// Класс для обеспечения сопряжения между устройствами и пересылки сообщений
public class NetManager : MonoBehaviour
{
    // Текстовое поле для подсчета количества активных и подключенных устройств
    public Text numOfDevicesText;
    // Слайдер прогресса воспроизведения видео
    public Slider trackingSlider;
    // Префабы для элементов списка подключенных устройств
    public GameObject client;
    public GameObject parent;
    // Спрайты для различного уровня заряда батареи устройства
    public Sprite bat0, bat25, bat50, bat75, bat100;
    // Текстовое поле прогресса воспроизведения видео
    public Text videoProgressText;

    // Переменные для сопряжения устройств и создания сервера
    private byte dataChanel;
    private int hostId;
    private const int MAX_USER = 200;
    private const int MAX_MSG_BUFF_SIZE = 1024;
    private const int SERVER_PORT = 55555;
    private const int CLIENT_PORT = 55556;
    private byte error;

    // Количество активных устройств
    public static int activeDevsNum = 0;
    // Список подключенных устройств с их состоянием активности
    public static Dictionary<int, bool> clientsDict = new Dictionary<int, bool>();
    // Колоичество кадров в секунду у воспроизводимого видео
    public float frameRate = 1;

    // Вызывается до первого фрейма сцены
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Init();
    }

    // Инициализация сервера и начало трансляции
    public void Init()
    {
        IPAddress localIP = GetLocalIPAddress();

        //Debug.Log(string.Format("Debug: Local ip - {0}", localIP.ToString()));

        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        dataChanel = cc.AddChannel(QosType.Reliable);
        HostTopology topo = new HostTopology(cc, MAX_USER);

        hostId = NetworkTransport.AddHost(topo, SERVER_PORT, null);

        ServerInfo info = new ServerInfo();

        info.host = localIP.ToString();
        info.port = SERVER_PORT;

        byte[] buff = new byte[MAX_MSG_BUFF_SIZE];

        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buff);
        formatter.Serialize(ms, info);

        NetworkTransport.StartBroadcastDiscovery(hostId, CLIENT_PORT, Globals.key, Globals.version, Globals.subversion, buff, MAX_MSG_BUFF_SIZE, 1000, out error);

        //Debug.Log("Debug: Starting broacasting");
    }

    // Получение локального IP адреса
    public static IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        return ipAddress;
    }

    // Сканирование сети на наличие входящих сообщений и их обработка
    private void Update()
    {
        int connId;
        int chanellId;
        byte[] buff = new byte[MAX_MSG_BUFF_SIZE];
        int msgSize = 0;
        NetworkEventType evnt = NetworkTransport.Receive(out hostId, out connId, out chanellId, buff, MAX_MSG_BUFF_SIZE, out msgSize, out error);
        switch (evnt)
        {
            case NetworkEventType.DataEvent:
                PlayerMessage msg;
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(buff);
                msg = (PlayerMessage)bf.Deserialize(ms);
                handleData(connId, chanellId, msg);
                break;
            case NetworkEventType.ConnectEvent:
                //Debug.Log(string.Format("Debug: ConnectEvent cientHostID - {0}, connId - {1}", hostId, connId));
                addNewDevice(connId);
                break;
            case NetworkEventType.DisconnectEvent:
                //Debug.Log(string.Format("Debug: DisconnectEvent cientHostID - {0}, connId - {1}", hostId, connId));
                deleteDevice(connId);
                activeDevsNum--;
                numOfDevicesText.text = "Активно " + activeDevsNum.ToString() + " из " + (clientsDict.Count).ToString() + " устройств";
                break;
            default:
                //Debug.Log(string.Format("Debug: network event - {0}", evnt));
                break;
        }
    }

    // Добавление нового устройства в список и создание соответствующего элемента интерфейса
    private void addNewDevice(int connId)
    {
        clientsDict.Add(connId, false);

        GameObject device = Instantiate(client, 
                             new Vector3(0, 1-clientsDict.Count(), 0), 
                             new Quaternion(0,0,0,0), parent.transform) as GameObject;
        device.name = connId.ToString();
        numOfDevicesText.text = "Активно " + activeDevsNum.ToString() + " из " + (clientsDict.Count).ToString() + " устройств";
    }

    // Удаление отключенного устройства из списка и соответствующего элемента интерфейса
    private void deleteDevice(int connId)
    {
        GameObject device = GameObject.Find(connId.ToString());
        Destroy(device);
        clientsDict.Remove(connId);
    }

    // Передача сообщения всем активным подключенным устройствам
    public void sendToAll(PlayerMessage msg) {
        byte[] buff = new byte[MAX_MSG_BUFF_SIZE];
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buff);
        bf.Serialize(ms, msg);

        foreach (KeyValuePair<int, bool> kvp in clientsDict) {
            if(kvp.Value)
                NetworkTransport.Send(hostId, kvp.Key, dataChanel, buff, MAX_MSG_BUFF_SIZE, out error);
        }
    }

    // Передача сообщения конкретному устройству
    public void sendMessage(int connId, PlayerMessage msg)
    {
        byte[] buff = new byte[MAX_MSG_BUFF_SIZE];
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buff);
        bf.Serialize(ms, msg);

        NetworkTransport.Send(hostId, connId, dataChanel, buff, MAX_MSG_BUFF_SIZE, out error);
    }

    // Определение типа полученного сообщения
    private void handleData(int connId, int chanellId, PlayerMessage msg)
    {
        //Debug.Log(string.Format("Debug: DataMessage - {0}, connId - {1}, chanellId - {2}", msg.ToString(), connId, chanellId));
        switch (msg.type)
        {
            case Globals.msgInfo:
                onInfo((DeviceInfo)msg, connId);
                break;
            case Globals.msgPlayerState:
                onRequestPlayerState((PlayerState)msg);
                break;
            /*case Globals.msgSetPlayerState:
                onSetPlayerState((SetPlayerState)msg);
                break;*/
        }
    }

    // Обработка сообщения с информацией об устройстве
    private void onInfo(DeviceInfo msg, int connId) 
    {
        foreach (Transform child in GameObject.Find(connId.ToString()).transform)
        {
            if (child.name == "DeviceName")
                child.GetComponent<Text>().text = msg.name.ToString() + " " + connId.ToString();
            else if (child.name == "Borders")
            {
                foreach (Transform childc in child.transform)
                {
                    if (childc.name == "DeviceDescription")
                    {
                        childc.GetComponent<Text>().text = msg.id.ToString();
                        break;
                    }
                }
            }
            else if (child.name == "Battery")
            {
                if (msg.battery < 0.2f)
                    child.GetComponent<Image>().sprite = bat0;
                if (msg.battery >= 0.2f && msg.battery < 0.4f)
                    child.GetComponent<Image>().sprite = bat25;
                if (msg.battery >= 0.4f && msg.battery < 0.6f)
                    child.GetComponent<Image>().sprite = bat50;
                if (msg.battery >= 0.6f && msg.battery < 0.8f)
                    child.GetComponent<Image>().sprite = bat75;
                if (msg.battery >= 0.8f)
                    child.GetComponent<Image>().sprite = bat100;
            }
            else if (child.name == "BatteryTxt")
            {
                child.GetComponent<Text>().text = msg.battery.ToString() + "%";
            }
        }
    }

    // Обработка сообщения с информацией о плеере
    private void onRequestPlayerState(PlayerState msg)
    {
        trackingSlider.maxValue = msg.frameCount;
        trackingSlider.value = msg.frame;
        frameRate = msg.frameRate;
        videoProgressText.text = ((int)(trackingSlider.value / frameRate / 60)).ToString() + ":" + ((int)((trackingSlider.value / frameRate) % 60)).ToString() + " / " +
            ((int)(trackingSlider.maxValue / frameRate / 60)).ToString() + ":" + ((int)((trackingSlider.maxValue / frameRate) % 60)).ToString();

    }
}
