/** Глобальные константы состояний
 */
public class Globals
{

    /* Message types */
    public const int msgRequestInfo = 0;
    public const int msgInfo = 1;
    public const int msgRequestPlayerState = 2;
    public const int msgSetPlayerState = 3;
    public const int msgPlayerState = 4;
    public const int msgServerInfo = 5;
    /* Message types */

    /* Player states */
    public const int psStopped = 0;
    public const int psPlaying = 1;
    public const int psPaused = 2;
    /* Player states */


    public const int portServer = 55555;
    public const int port = 55556;
    public static bool isServer;
    public static string serverIp = "192.168.0.59";
    public const long stateInterval = 1000;
    public const double syncDiff = 1; //Разница в времени проигрывания для синхронизации

    public const int key = 3452;
    public const int version = 0;
    public const int subversion = 1;
}
