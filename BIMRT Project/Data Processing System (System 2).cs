using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Update;
using VDS.RDF.Writing;

public class TargetSystem
{
    public string SystemName { get; set; }
    public IPAddress IPAddress { get; set; }
    public int Port { get; set; }
}

public class Message
{
    public string SerializedXml { get; set; }
    public string Acknowledgment { get; set; }
}

public class Server
{
    static void Main()
    {
        TcpListener tcpListener = null;

        try
        {
            // Set the server IP address and port for System 1
            IPAddress ipAddress = IPAddress.Parse("172.21.4.122");
            int port = 62962;

            // Start listening for incoming connections
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();

            Console.WriteLine("Server is listening on port " + port);

            while (true)
            {
                // Add a delay to control the frequency of the "Server is listening" message
                Thread.Sleep(1000); // Adjust the delay time as needed

                // Accept the pending client connection
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                Console.WriteLine("Client connected!");

                // Handle the client in a separate thread
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(tcpClient);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            // Close the TcpListener (this code is unreachable in an infinite loop)
            tcpListener?.Stop();
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;

        try
        {
            Console.WriteLine("Client connected!");

            // Receive the serialized XML from the client
            string serializedXml = ReceiveSerializedXml(tcpClient);

            // Deserialize the XML object with metadata
            UpdateQueryMetadata updateQueryMetadata = DeserializeUpdateXml(serializedXml);

            // Print the timestamp when receiving the serialized XML
            //Console.WriteLine($"Received Serialized XML at: {DateTime.Now}");
            Console.WriteLine($"Deserialized Metadata Timestamp: {updateQueryMetadata.Metadata.Timestamp}");
            Console.WriteLine($"Deserialized Metadata Author: {updateQueryMetadata.Metadata.Author}");
            Console.WriteLine($"Deserialized Update Query: {updateQueryMetadata.Update.Query}");

            // Update the RDF database using the deserialized data
            TripleStore tripleStore = LoadRdfData("C:\\Users\\selva\\OneDrive\\Desktop\\Project\\wall-standard-case.ttl");
            UpdateRdfDatabase(updateQueryMetadata.Update.Query, tripleStore);

            // Save the updated RDF data to a Turtle file
            string updatedFilePath = Path.Combine("C:\\Users\\selva\\OneDrive\\Desktop\\Project", "wall-standard-case-updated.ttl");
            SaveRdfToFile(tripleStore, updatedFilePath);

            Console.WriteLine("RDF data updated and saved to file");

            // List to store target systems
            LinkedList<TargetSystem> targetSystems = new LinkedList<TargetSystem>();

            // Add target systems with their details
            targetSystems.AddLast(new TargetSystem { SystemName = "System2", IPAddress = IPAddress.Parse("172.21.13.181"), Port = 57844 });
            targetSystems.AddLast(new TargetSystem { SystemName = "System3", IPAddress = IPAddress.Parse("172.21.8.240"), Port = 53607 });

            // Send the received serialized XML to the target systems
            SendSerializedXmlToTargetSystems(targetSystems, serializedXml, "RDF Database updated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error handling client: " + ex.Message);
        }
        finally
        {
            // Close the TcpClient
            tcpClient.Close();
        }
    }

    private static string ReceiveSerializedXml(TcpClient tcpClient)
    {
        try
        {
            // Get the network stream
            NetworkStream networkStream = tcpClient.GetStream();

            // Read the incoming data
            using (StreamReader reader = new StreamReader(networkStream))
            {
                return reader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return null;
        }
    }

    private static void SendSerializedXmlToTargetSystems(LinkedList<TargetSystem> targetSystems, string serializedXml, string acknowledgmentMessage)
    {
        foreach (var targetSystem in targetSystems)
        {
            // Send the serialized XML and acknowledgment to each target system
            SendSerializedXmlToSystem(targetSystem, serializedXml, acknowledgmentMessage);
        }
    }

    private static void SendSerializedXmlToSystem(TargetSystem targetSystem, string serializedXml, string acknowledgmentMessage)
    {
        try
        {
            // Create a TcpClient for the target system
            using (TcpClient targetTcpClient = new TcpClient(targetSystem.IPAddress.ToString(), targetSystem.Port))
            {
                // Get the network stream
                NetworkStream networkStream = targetTcpClient.GetStream();

                // Create a Message object with serialized XML and acknowledgment
                Message message = new Message
                {
                    SerializedXml = serializedXml,
                    Acknowledgment = acknowledgmentMessage
                };

                // Serialize the Message object
                XmlSerializer messageSerializer = new XmlSerializer(typeof(Message));
                using (StreamWriter writer = new StreamWriter(networkStream))
                {
                    messageSerializer.Serialize(writer, message);
                }

                Console.WriteLine($"Serialized XML and Acknowledgment sent to {targetSystem.SystemName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending to {targetSystem.SystemName}: {ex.Message}");
        }
    }

    private static TripleStore LoadRdfData(string filePath)
    {
        TripleStore tripleStore = new TripleStore();
        FileLoader.Load(tripleStore, filePath);
        return tripleStore;
    }

    private static void UpdateRdfDatabase(string updateQuery, TripleStore tripleStore)
    {
        // Use your RDF library to update the RDF data
        LeviathanUpdateProcessor updateProcessor = new LeviathanUpdateProcessor(tripleStore);
        SparqlUpdateCommandSet updateCommand = new SparqlUpdateParser().ParseFromString(updateQuery);
        updateProcessor.ProcessCommandSet(updateCommand);
    }

    private static void SaveRdfToFile(TripleStore tripleStore, string filePath)
    {
        // Save each graph in the TripleStore to a Turtle file
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            CompressingTurtleWriter turtleWriter = new CompressingTurtleWriter();

            foreach (IGraph graph in tripleStore.Graphs)
            {
                turtleWriter.Save(graph, writer);
            }
        }

        Console.WriteLine($"Updated RDF data saved to: {filePath}");
    }

    private static UpdateQueryMetadata DeserializeUpdateXml(string xml)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(UpdateQueryMetadata));

        using (StringReader reader = new StringReader(xml))
        {
            return (UpdateQueryMetadata)serializer.Deserialize(reader);
        }
    }
}

[XmlRoot("UpdateQuery")]
public class UpdateQueryMetadata
{
    [XmlElement("Metadata")]
    public MyMetadata Metadata { get; set; }

    [XmlElement("Update")]
    public Update Update { get; set; }
}

public class MyMetadata
{
    [XmlElement("Timestamp")]
    public DateTime Timestamp { get; set; }

    [XmlElement("Author")]
    public string Author { get; set; }

    //public string Serialize()
    //{
    //    // generate xml string
    //}

    //public static Deserialize MyMetadata(string serialized)
    //{
    //    // deserialization
    //}
}

public class Update
{
    [XmlElement("Query")]
    public string? Query { get; set; }
}