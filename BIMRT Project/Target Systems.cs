using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class TcpServer
{
    static void Main()
    {
        TcpListener tcpListener = null;

        try
        {
            // Set the server IP address and port
            IPAddress ipAddress = IPAddress.Parse("172.21.13.181");
            int port = 57844; // Use the desired port number

            // Start TcpListener
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();

            while (true)
            {
                Console.WriteLine("Server is listening on port " + port);

                // Accept the client connection 
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                // Handle the client in a separate thread or asynchronously if needed
                HandleClient(tcpClient);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            // Stop the TcpListener when exiting the loop
            tcpListener?.Stop();
        }
    }

    static void HandleClient(TcpClient tcpClient)
    {
        try
        {
            // Get the network stream
            using (NetworkStream networkStream = tcpClient.GetStream())
            {
                // Read the string from the network stream
                byte[] buffer = new byte[4096];
                StringBuilder stringBuilder = new StringBuilder();
                int bytesRead;
                while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                }

                // Print the received string on the console
                Console.WriteLine(stringBuilder.ToString());
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error handling client: " + ex.Message);
        }
        finally
        {
            // Close the TcpClient after handling the client
            tcpClient.Close();
        }
    }
}