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

    private Dictionary<int, Vector3> playerPositions = new Dictionary<int, Vector3>(); // Lưu trữ vị trí của các player

    void StartServer()
    {
        Debug.Log("Starting Server");

        networkDriver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 8888;
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
        if(networkDriver.IsCreated)
        {
            networkDriver.Dispose();
        }
        if(connections.IsCreated)
        {
            connections.Dispose();
        }
    }

    void Start()
    {
        if (RunLocal)
        {
            StartServer(); // Run the server locally
        }
        else
        {
            // TODO: Start from PlayFab configuration
        }
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                continue;
            }

            NetworkEvent.Type cmd;
            DataStreamReader stream;
            while ((cmd = networkDriver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    int playerId = stream.ReadInt(); // Nhận playerId từ client
                    float x = stream.ReadFloat();
                    float y = stream.ReadFloat();
                    float z = stream.ReadFloat();

                    Debug.Log("Received data from player " + playerId + ": " + new Vector3(x, y, z));

                    playerPositions[playerId] = new Vector3(x, y, z);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);
                    numPlayers--;
                }
            }

            // Gửi vị trí của tất cả các player cho tất cả các client
            networkDriver.BeginSend(NetworkPipeline.Null, connections[i], out var writer);
            writer.WriteInt(numPlayers);
            foreach (var kvp in playerPositions)
            {
                writer.WriteInt(kvp.Key); // Gửi playerId
                writer.WriteFloat(kvp.Value.x);
                writer.WriteFloat(kvp.Value.y);
                writer.WriteFloat(kvp.Value.z);
            }
            networkDriver.EndSend(writer);

            Debug.Log("Sent positions to client " + i);
        }
    }
}
