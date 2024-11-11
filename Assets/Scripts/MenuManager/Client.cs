using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Networking.Transport;
using Unity.Collections;

public class Client : MonoBehaviour
{
    public bool RunLocal;
    private NetworkDriver networkDriver;
    private NetworkConnection networkConnection;
    private bool isConnected = false;
    private bool startedConnectionRequest = false;

    public GameObject sphere; // Tham chiếu đến vật thể sphere
    private int playerId = -1;  // PlayerId sẽ được cấp phát từ server

    // Kết nối đến server
    private void connectToServer(string address, ushort port)
    {
        Debug.Log("Connecting to " + address + ":" + port);
        networkDriver = NetworkDriver.Create();
        networkConnection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.Parse(address, port);
        networkConnection = networkDriver.Connect(endpoint);
        startedConnectionRequest = true;
    }

    public void OnDestroy()
    {
        if (networkDriver.IsCreated)
        {
            networkDriver.Dispose();
        }
    }

    void Start()
    {
        Debug.Log("Starting client");
        if (RunLocal)
        {
            connectToServer("127.0.0.1", 8888);
        }
        else
        {
            // TODO: Start from PlayFab configuration
        }
    }

    void Update()
    {
        if (!startedConnectionRequest)
        {
            return;
        }

        networkDriver.ScheduleUpdate().Complete();

        if (!networkConnection.IsCreated)
        {
            Debug.LogError("Connection failed! Retrying...");
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = networkConnection.PopEvent(networkDriver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server");
                isConnected = true;
                if (stream.IsCreated)
                {
                    playerId = stream.ReadInt();  // Server trả lại playerId
                    Debug.Log("Received playerId: " + playerId);
                }
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                if (stream.IsCreated)
                {
                    ReceiveDataFromServer(stream);
                }
                else
                {
                    Debug.LogError("Stream is null!");
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                networkConnection = default(NetworkConnection);
                break;
            }
        }

        SendPositionToServer(); // Gửi vị trí của sphere đến server
    }

    private void ReceiveDataFromServer(DataStreamReader stream)
    {
        if (stream.Length < 4)
        {
            Debug.LogError("Not enough data to read. Skipping.");
            return;
        }

        int playerCount = stream.ReadInt();
        Debug.Log("Number of players: " + playerCount);

        for (int i = 0; i < playerCount; i++)
        {
            if (stream.Length < 16)
            {
                Debug.LogError("Not enough data to read player info. Skipping.");
                return;
            }

            int receivedPlayerId = stream.ReadInt();
            float x = stream.ReadFloat();
            float y = stream.ReadFloat();
            float z = stream.ReadFloat();

            Debug.Log("Received data for player " + receivedPlayerId + ": " + new Vector3(x, y, z));

            // Cập nhật vị trí của sphere của player khác
            if (receivedPlayerId != playerId)  // Tránh cập nhật vị trí của chính mình
            {
                sphere.transform.position = new Vector3(x, y, z);
                Debug.Log("Updated position for player " + receivedPlayerId);
            }
        }
    }

    private void SendPositionToServer()
    {
        if (!isConnected)
        {
            return;
        }

        // Gửi vị trí của sphere cùng với playerId
        networkDriver.BeginSend(NetworkPipeline.Null, networkConnection, out var writer);
        writer.WriteInt(playerId); // Gửi playerId
        writer.WriteFloat(sphere.transform.position.x);
        writer.WriteFloat(sphere.transform.position.y);
        writer.WriteFloat(sphere.transform.position.z);
        networkDriver.EndSend(writer);

        Debug.Log("Client sent position to server: " + sphere.transform.position);
    }
}
