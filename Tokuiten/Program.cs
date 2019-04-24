using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;

namespace Tokuiten
{
    class Program
    {
        static void Main(string[] args)
        {
            var allConnection = new List<IWebSocketConnection>();
            var wsServer = new WebSocketServer("ws://0.0.0.0:23333");
            wsServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine(socket.ConnectionInfo.Id + ": " + "Open!");
                    socket.Send("");
                    allConnection.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine(socket.ConnectionInfo.Id + ": " + "Close!");
                    socket.Send("");
                    allConnection.Remove(socket);
                };
                socket.OnMessage = msg =>
                {

                };

            });
            Console.ReadKey();
        }
    }
}
