
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Networking.Transport;
using Unity.Collections; 

public class Server : MonoBehaviour
{
    public bool RunLocal;
    public NetworkDriver networkDriver;
    private NativeList<NetworkConnection> connections;
    private int numPlayers = 0;
    private List<Vector3> playerPositions = new List<Vector3>();
    private List<float> playerSpeeds = new List<float>();

    void StartServer()
    {
        Debug.Log("Starting server");

        //start transport server
        networkDriver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 7777;
        if (networkDriver.Bind(endpoint) != 0)
        {
            Debug.Log("Failed to bind to port " + endpoint.Port);
        }
        else
        {
            networkDriver.Listen();
        }

        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

    }

    void OnDestroy()
    {
        if (networkDriver.IsCreated)
        {
            networkDriver.Dispose();
        }
        if (connections.IsCreated)
        {
            connections.Dispose();
        }
    }

    void Start()
    {
        if (RunLocal)
        {
            StartServer();
        }
        else
        {
            //todo: start from playfab configuration
        }
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        //client up connection
        for(int i = 0; i < connections.Length; i++)
        {
            if( !connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        //accept new connection
        NetworkConnection c;
        while((c = networkDriver.Accept() ) != default(NetworkConnection))
        {
            connections.Add(c);
            playerPositions.Add(new Vector3(0,0,0));
            playerSpeeds.Add(0f);
            Debug.Log("Accepted a connection");
            numPlayers++;
        }

        DataStreamReader stream;
        for (int i = 0; i < connections.Length; i++)
        {
            if(!connections[i].IsCreated)
            {
                continue;
            }

            NetworkEvent.Type cmd;
            while( (cmd = networkDriver.PopEventForConnection( connections[i],out stream)) != NetworkEvent.Type.Empty)
            {
                if( cmd == NetworkEvent.Type.Data)
                {
                    float posX = stream.ReadFloat();
                    float posY = stream.ReadFloat();
                    float posZ = stream.ReadFloat();
                    float speed = stream.ReadFloat();
                    
                    playerPositions[i] = new Vector3(posX, posY, posZ);
                    playerSpeeds[i] = speed;
                }
                else if( cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);
                    playerPositions.RemoveAt(i);
                    playerSpeeds.RemoveAt(i);
                    numPlayers--;
                }
            }

            // broadcast Game State
            networkDriver.BeginSend( NetworkPipeline.Null, connections[i], out var writer);
            writer.WriteUInt( (uint)numPlayers);
            for(int j = 0; j < numPlayers; j++)
            {    
                 writer.WriteFloat(playerPositions[j].x);
                writer.WriteFloat(playerPositions[j].y);
                writer.WriteFloat(playerPositions[j].z);
                writer.WriteFloat(playerSpeeds[j]);
            }    
            networkDriver.EndSend(writer);           
        }
    }
}
