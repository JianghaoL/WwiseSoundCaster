using Newtonsoft.Json.Linq;
using AK.Wwise.Waapi;

/// <summary>
/// This class manages the connection to the Wwise Authoring API (WAAPI) and provides methods for interacting with Wwise.
/// It serves as a central point for WAAPI communication, allowing other parts of the application to call its methods to perform actions like connecting, registering game objects, subscribing to events, etc.
/// </summary>
public class WwiseClient
{
    public static AK.Wwise.Waapi.JsonClient? client;
    public static string? message;
    public static bool isConnected;

    WwiseClient()
    {
        client = null;
        message = null;
        isConnected = false;
    }

    public static async System.Threading.Tasks.Task Connect()
    {
        try
        {
            client = new AK.Wwise.Waapi.JsonClient();
            await client.Connect();
            isConnected = true;
            message = "Connected to Wwise Authoring.";
        }
        catch (System.Exception ex)
        {
            isConnected = false;
            message = $"Connection failed : {ex.Message}";
        }
        
    }   

}