using System;
using System.Collections.Generic;
using Fleck;
using MojoUnity;

namespace Tokuiten
{
    static class Program
    {
        static void Main()
        {
            var allConnection = new List<IWebSocketConnection>();
            var allChat = new Dictionary<IWebSocketConnection, UserEntity>();
            var wsServer = new WebSocketServer("ws://0.0.0.0:23333");
            wsServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine(socket.ConnectionInfo.Id + ": " + "Open!");
                    allConnection.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine(socket.ConnectionInfo.Id + ": " + "Close!");

                    if (allChat.ContainsKey(socket))
                    {
                        foreach (var item in allChat)
                            if (item.Value.Channel == allChat[socket].Channel)
                                item.Key.Send($"{allChat[socket].Nick}:leave");

                        allChat.Remove(socket);
                    }

                    allConnection.Remove(socket);
                };
                socket.OnMessage = msg =>
                {
                    if (msg.Contains("cmd"))
                    {
                        JsonValue jMsg = Json.Parse(msg);
                        var cmd = jMsg.AsObjectGetString("cmd");
                        if (cmd == "join")
                        {
                            var channel = jMsg.AsObjectGetString("channel");
                            var nick = jMsg.AsObjectGetString("nick");
                            if (allChat.TryAdd(socket, new UserEntity {Channel = channel, Nick = nick}))
                                foreach (var item in allChat)
                                    if (item.Value.Channel == channel)
                                        item.Key.Send($"{allChat[socket].Nick}:join");
                        }
                        if (cmd == "chat")
                        {
                            var text = jMsg.AsObjectGetString("text");
                                foreach (var item in allChat)
                                    if (item.Value.Channel == allChat[socket].Channel)
                                        item.Key.Send($"{allChat[socket].Nick}:{text}");
                        }
                    }
                    
                };

            });
            Console.ReadKey();
        }
        public class UserEntity
        {
            public string Nick;
            public string Channel;
        }
    }
}
