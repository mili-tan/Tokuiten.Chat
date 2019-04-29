using System;
using System.Collections.Generic;
using Fleck;
using Newtonsoft.Json;

namespace Tokuiten
{
    static class Program
    {
        static void Main()
        {
            var allChat = new Dictionary<IWebSocketConnection, UserEntity>();
            var allConnection = new List<IWebSocketConnection>();
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
                                item.Key.Send(JsonConvert.SerializeObject(new
                                    {cmd = "leave", nick = allChat[socket].Nick}));

                        allChat.Remove(socket);
                    }
                    allConnection.Remove(socket);
                };
                socket.OnMessage = msg =>
                {
                    if (msg.Contains("cmd"))
                    {
                        Console.WriteLine(msg);
                        var jMsg = JsonConvert.DeserializeObject<dynamic>(msg);
                        var cmd = jMsg.cmd.ToString();
                        switch (cmd)
                        {
                            case "join":
                                {
                                    var channel = jMsg.channel.ToString();
                                    var mNick = jMsg.nick.ToString();

                                    if (allChat.ContainsValue(new UserEntity { Channel = channel, Nick = mNick }))
                                        socket.Send(JsonConvert.SerializeObject(new { cmd = "error", text = "nickname-exists" }));
                                    else
                                    {
                                        if (allChat.TryAdd(socket, new UserEntity { Channel = channel, Nick = mNick }))
                                            foreach (var item in allChat)
                                                if (item.Value.Channel == channel)
                                                    item.Key.Send(JsonConvert.SerializeObject(new
                                                    { cmd = "join", nick = mNick }));
                                    }

                                    break;
                                }
                            case "chat":
                                {
                                    var mText = jMsg.text.ToString();
                                    var mNick = allChat[socket].Nick;
                                    var mCid = Guid.NewGuid();
                                    var channel = allChat[socket].Channel;

                                    foreach (var item in allChat)
                                        if (item.Value.Channel == channel)
                                            item.Key.Send(JsonConvert.SerializeObject(new
                                            { cmd = "chat", nick = mNick, text = mText, cid = mCid }));
                                    break;
                                }
                            case "encrypt-chat":
                                {
                                    var mText = jMsg.text.ToString();
                                    var mEncrypt = jMsg.encrypt.ToString();
                                    var mNick = allChat[socket].Nick;
                                    var mCid = Guid.NewGuid();
                                    var channel = allChat[socket].Channel;

                                    foreach (var item in allChat)
                                        if (item.Value.Channel == channel)
                                            item.Key.Send(JsonConvert.SerializeObject(new
                                            { cmd = "chat", encrypt = mEncrypt, nick = mNick, text = mText, cid = mCid }));
                                    break;
                                }
                            case "whisper":
                                {
                                    var toWho = jMsg.to.ToString();
                                    var mText = jMsg.text.ToString();
                                    var mNick = allChat[socket].Nick;
                                    var mCid = Guid.NewGuid();

                                    foreach (var item in allChat)
                                        if (item.Value.Channel == allChat[socket].Channel && item.Value.Nick == toWho)
                                            item.Key.Send(JsonConvert.SerializeObject(new
                                            { cmd = "whisper", nick = mNick, text = mText, cid = mCid }));
                                    break;
                                }
                            case "delete":
                                {
                                    var mCid = jMsg.cid.ToString();
                                    var mNick = allChat[socket].Nick;

                                    foreach (var item in allChat)
                                        if (item.Value.Channel == allChat[socket].Channel)
                                            item.Key.Send(JsonConvert.SerializeObject(new
                                            { cmd = "delete", nick = mNick, cid = mCid }));
                                    break;
                                }
                            case "edit":
                                {
                                    var mCid = jMsg.cid.ToString();
                                    var mText = jMsg.text.ToString();
                                    var mNick = allChat[socket].Nick;

                                    foreach (var item in allChat)
                                        if (item.Value.Channel == allChat[socket].Channel)
                                            item.Key.Send(JsonConvert.SerializeObject(new
                                            { cmd = "edit", nick = mNick, text = mText, cid = mCid }));
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
