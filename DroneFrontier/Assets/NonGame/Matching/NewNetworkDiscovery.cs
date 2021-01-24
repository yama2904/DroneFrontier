using System.Collections.Generic;
using System;
using System.Net;
using Mirror;
using Mirror.Discovery;

/*
	Discovery Guide: https://mirror-networking.com/docs/Guides/NetworkDiscovery.html
    Documentation: https://mirror-networking.com/docs/Components/NetworkDiscovery.html
    API Reference: https://mirror-networking.com/docs/api/Mirror.Discovery.NetworkDiscovery.html
*/

public class DiscoveryRequest : NetworkMessage
{
    // Add properties for whatever information you want sent by clients
    // in their broadcast messages that servers will consume.
    public string name = "";
}

//応答
public class DiscoveryResponse : NetworkMessage
{
    // Add properties for whatever information you want the server to return to
    // clients for them to display or consume for establishing a connection.
    public IPEndPoint EndPoint { get; set; }
    public Uri uri;
    public long serverId;
}

public class NewNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, ServerResponse>
{
    public long ServerId { get; private set; }
    public Transport transport;
    public ServerFoundUnityEvent OnServerFound;

    public override void Start()
    {
        ServerId = RandomLong();

        if (transport == null)
            transport = Transport.activeTransport;

        base.Start();
    }

    #region Server

    /// <summary>
    /// Reply to the client to inform it of this server
    /// </summary>
    /// <remarks>
    /// Override if you wish to ignore server requests based on
    /// custom criteria such as language, full server game mode or difficulty
    /// </remarks>
    /// <param name="request">Request comming from client</param>
    /// <param name="endpoint">Address of the client that sent the request</param>
    //protected override void ProcessClientRequest(DiscoveryRequest request, IPEndPoint endpoint)
    //{
    //    //クライアントに返信
    //    base.ProcessClientRequest(request, endpoint);
    //}

    /// <summary>
    /// Process the request from a client
    /// </summary>
    /// <remarks>
    /// Override if you wish to provide more information to the clients
    /// such as the name of the host player
    /// </remarks>
    /// <param name="request">Request comming from client</param>
    /// <param name="endpoint">Address of the client that sent the request</param>
    /// <returns>A message containing information about this server</returns>
    protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
    {
        // In this case we don't do anything with the request
        // but other discovery implementations might want to use the data
        // in there,  This way the client can ask for
        // specific game mode or something                        
        try
        {
            // this is an example reply message,  return your own
            // to include whatever is relevant for your game
            MatchingManager.playerNames.Add(request.name);
            return new ServerResponse
            {
                serverId = ServerId,
                uri = transport.ServerUri()
            };
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    #endregion

    #region Client

    /// <summary>
    /// Create a message that will be broadcasted on the network to discover servers
    /// </summary>
    /// <remarks>
    /// Override if you wish to include additional data in the discovery message
    /// such as desired game mode, language, difficulty, etc... </remarks>
    /// <returns>An instance of ServerRequest with data to be broadcasted</returns>
    protected override ServerRequest GetRequest()
    {
        //サーバを検出するためのブロードキャストメッセージ
        return new ServerRequest
        {
            name = KuribocchiButtonsController.playerName
        };
    }

    /// <summary>
    /// Process the answer from a server
    /// </summary>
    /// <remarks>
    /// A client receives a reply from a server, this method processes the
    /// reply and raises an event
    /// </remarks>
    /// <param name="response">Response that came from the server</param>
    /// <param name="endpoint">Address of the server that replied</param>
    protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
    {
        //サーバからの回答を処理
        response.EndPoint = endpoint;

        UriBuilder realUri = new UriBuilder(response.uri)
        {
            Host = response.EndPoint.Address.ToString()
        };
        response.uri = realUri.Uri;

        CustomNetworkDiscoveryHUD.Instance.OnDiscoveredServer(response);
    }

    #endregion
}