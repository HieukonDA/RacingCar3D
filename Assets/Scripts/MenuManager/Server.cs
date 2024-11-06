
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
    const int numEnemies = 12;
    private byte[] enemyStatus;
    private int numPlayers = 0;

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

        enemyStatus = new byte[numEnemies];
        for(int i =  0; i < numEnemies; i++)
        {
            enemyStatus[i] = 1;
        }

    }

    void OnDestroy()
    {
        networkDriver.Dispose();
        connections.Dispose();
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
                    uint number = stream.ReadUInt();
                    //check that the number of enemies
                    if(number == numEnemies)
                    {
                        for(int b = 0; b < numEnemies; b++)
                        {
                            byte isAlive = stream.ReadByte();
                            if(isAlive == 0 && enemyStatus[b] > 0)
                            {
                                Debug.Log("Enemy " + b + " is dead" +1);
                                enemyStatus[b] = 0;
                            }
                        }
                    }
                }
                else if( cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);
                    numPlayers--;
                }
            }

            // broadcast Game State
            networkDriver.BeginSend( NetworkPipeline.Null, connections[i], out var writer);
            writer.WriteUInt( numEnemies);
            for(int b = 0; b < numEnemies; b++)
            {    
                writer.WriteByte(enemyStatus[b]);
            }    
            networkDriver.EndSend(writer);           
        }
    }
}
