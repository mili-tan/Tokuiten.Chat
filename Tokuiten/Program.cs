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
            var allChat = new Dictionary<IWebSocketConnection, UserEntity>();
            var wsServer = new WebSocketServer("ws://0.0.0.0:23333");
            wsServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine(socket.ConnectionInfo.Id + ": " + "Open!");
                    Console.WriteLine(socket.ConnectionInfo.Path);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine(socket.ConnectionInfo.Id + ": " + "Close!");

                    if (allChat.ContainsKey(socket))
                    {
                        foreach (var item in allChat)
                            if (item.Value.Channel == allChat[socket].Channel)
                                item.Key.Send($"{allChat[socket].Nick}:leave:{Guid.NewGuid()}");

                        allChat.Remove(socket);
                    }
                };
                socket.OnMessage = msg =>
                {
                    if (msg.Contains("cmd"))
                    {
                        JsonValue jMsg = Json.Parse(msg);
                        var cmd = jMsg.AsObjectGetString("cmd");
                        switch (cmd)
                        {
                            case "join":
                            {
                                var channel = jMsg.AsObjectGetString("channel");
                                var nick = jMsg.AsObjectGetString("nick");
                                if (allChat.TryAdd(socket, new UserEntity {Channel = channel, Nick = nick}))
                                    foreach (var item in allChat)
                                        if (item.Value.Channel == channel)
                                            item.Key.Send($"{allChat[socket].Nick}:join:{Guid.NewGuid()}");
                                break;
                            }
                            case "chat":
                            {
                                var text = jMsg.AsObjectGetString("text");
                                foreach (var item in allChat)
                                    if (item.Value.Channel == allChat[socket].Channel)
                                        item.Key.Send($"{allChat[socket].Nick}:{text}:{Guid.NewGuid()}");
                                break;
                            }
                            case "whisper":
                            {
                                var toWho = jMsg.AsObjectGetString("to");
                                var text = jMsg.AsObjectGetString("text");
                                foreach (var item in allChat)
                                    if (item.Value.Channel == allChat[socket].Channel && item.Value.Nick == toWho)
                                        item.Key.Send($"{allChat[socket].Nick}:{text}:{Guid.NewGuid()}");
                                break;
                            }
                            case "delete":
                            {
                                var cid = jMsg.AsObjectGetString("cid");
                                foreach (var item in allChat)
                                    if (item.Value.Channel == allChat[socket].Channel)
                                        item.Key.Send($"{allChat[socket].Nick}:delete:{cid}");
                                break;
                            }
                            case "edit":
                            {
                                var cid = jMsg.AsObjectGetString("cid");
                                var text = jMsg.AsObjectGetString("text");
                                foreach (var item in allChat)
                                    if (item.Value.Channel == allChat[socket].Channel)
                                        item.Key.Send($"{allChat[socket].Nick}:edit:{cid}:{text}");
                                break;
                            }
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
