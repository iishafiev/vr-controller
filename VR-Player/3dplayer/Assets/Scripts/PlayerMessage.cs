using System;

// Класс для сообщений между устройствами
[System.Serializable]
public class PlayerMessage
{
    public int type { set; get; }
    public long timestamp { set; get; }

    public PlayerMessage()
    {
        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}

// Класс для сообщений о запросе состояния устройства
[System.Serializable]
public class RequestDeviceInfo : PlayerMessage
{

    public RequestDeviceInfo() : base()
    {
        type = Globals.msgRequestInfo;
    }

}

// Класс для сообщений с информацией об устройстве
[System.Serializable]
public class DeviceInfo : PlayerMessage
{
    public string id { set; get; }
    public string name { set; get; }
    public float battery { set; get; }

    public DeviceInfo() : base()
    {
        type = Globals.msgInfo;
    }

}

// Класс для сообщений о состоянии плеера
[System.Serializable]
public class PlayerState : PlayerMessage
{
    public int state { set; get; }
    public string videoFile { set; get; }
    public long frame { set; get; }
    public ulong frameCount { set; get; }
    public float frameRate { set; get; }
    public float volume { set; get; }
    public PlayerState() : base()
    {
        type = Globals.msgPlayerState;
    }

}

// Класс для сообщений об установке состояния плеера
[System.Serializable]
public class SetPlayerState : PlayerState
{

    public SetPlayerState() : base()
    {
        type = Globals.msgSetPlayerState;
    }

}

// Класс для сообщений о запросе состояния плеера
[System.Serializable]
public class RequestPlayerState : PlayerMessage
{
    public RequestPlayerState() : base()
    {
        type = Globals.msgRequestPlayerState;
    }

}

// Класс для сообщений с информацией о сервере
[System.Serializable]
public class ServerInfo : PlayerMessage
{
    public ServerInfo() : base()
    {
        type = Globals.msgServerInfo;
    }
    public string host { set; get; }
    public int port { set; get; }
}