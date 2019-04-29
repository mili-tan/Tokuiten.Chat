using System;
using System.Collections.Generic;
using Fleck;
using MojoUnity;
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

                        JsonValue jMsg = Json.Parse(msg);
                        var cmd = jMsg.AsObjectGetString("cmd");
                        if (cmd == "join")
                        {
                            var channel = jMsg.AsObjectGetString("channel");
                            var mNick = jMsg.AsObjectGetString("nick");

                            if (allChat.ContainsValue(new UserEntity {Channel = channel, Nick = mNick}))
                                socket.Send(JsonConvert.SerializeObject(new {cmd = "error", text = "nickname-exists"}));
                            else
                            {
                                if (allChat.TryAdd(socket, new UserEntity {Channel = channel, Nick = mNick}))
                                    foreach (var item in allChat)
                                        if (item.Value.Channel == channel)
                                            item.Key.Send(JsonConvert.SerializeObject(new
                                                {cmd = "join", nick = mNick}));                            }
                        }
                        if (cmd == "chat")
                        {
                            var mText = jMsg.AsObjectGetString("text");
                            var mNick = allChat[socket].Nick;
                            var mCid = Guid.NewGuid();
                            var channel = allChat[socket].Channel;

                            foreach (var item in allChat)
                                if (item.Value.Channel == channel)
                                    item.Key.Send(JsonConvert.SerializeObject(new
                                        {cmd = "chat", nick = mNick, text = mText, cid = mCid}));                        }
                        if (cmd == "encrypt-chat")
                        {
                            var mText = jMsg.AsObjectGetString("text");
                            var mEncrypt = jMsg.AsObjectGetString("encrypt");
                            var mNick = allChat[socket].Nick;
                            var mCid = Guid.NewGuid();
                            var channel = allChat[socket].Channel;

                            foreach (var item in allChat)
                                if (item.Value.Channel == channel)
                                    item.Key.Send(JsonConvert.SerializeObject(new
                                        {cmd = "chat", encrypt = mEncrypt, nick = mNick, text = mText, cid = mCid}));                        }
                        if (cmd == "whisper")
                        {
                            var toWho = jMsg.AsObjectGetString("to");
                            var mText = jMsg.AsObjectGetString("text");
                            var mNick = allChat[socket].Nick;
                            var mCid = Guid.NewGuid();

                            foreach (var item in allChat)
                                if (item.Value.Channel == allChat[socket].Channel && item.Value.Nick == toWho)
                                    item.Key.Send(JsonConvert.SerializeObject(new
                                        {cmd = "whisper", nick = mNick, text = mText, cid = mCid}));
                        }
                        if (cmd == "delete")
                        {
                            var mCid = jMsg.AsObjectGetString("cid");
                            var mNick = allChat[socket].Nick;

                            foreach (var item in allChat)
                                if (item.Value.Channel == allChat[socket].Channel)
                                    item.Key.Send(JsonConvert.SerializeObject(new
                                        {cmd = "delete", nick = mNick, cid = mCid}));
                        }
                        if (cmd == "edit")
                        {
                            var mCid = jMsg.AsObjectGetString("cid");
                            var mText = jMsg.AsObjectGetString("text");
                            var mNick = allChat[socket].Nick;

                            foreach (var item in allChat)
                                if (item.Value.Channel == allChat[socket].Channel)
                                    item.Key.Send(JsonConvert.SerializeObject(new
                                        {cmd = "edit", nick = mNick, text = mText, cid = mCid}));
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
