using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MelonLoader;

using Newtonsoft.Json;
using System.IO;

using WebSocketSharp;
using WebSocketSharp.Server;

namespace StreamingUtil
{
    public class Server : WebSocketBehavior
    {
        private static Server _default;
        public static Config config;

        public static Server Default { get {
                return _default;
            } 
        }

        public Server()
        {
            
            if (_default == null)
            {
                _default = this;
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            MelonLogger.Msg("Opened a weksocket on Test");

            //Broadcast config immediately when client connects to disable/enable elements
            SendConfig();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            MelonLogger.Msg(e.Reason);
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);
            MelonLogger.Error(e.Message);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            MelonLogger.Msg((e.IsText ? e.Data : "Received binary data on Test"));
        }

        public bool AnyConnected { get => Sessions.Count > 0; }

        public void SendSong(Song song)
        {
            NetworkMessage<Song> msg = new NetworkMessage<Song>
            {
                Type = MessageType.Song,
                Content = song
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        public void SendConfig()
        {
            NetworkMessage<Config> msg = new NetworkMessage<Config>
            {
                Type = MessageType.Config,
                Content = config
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        public void SendModifiers(Styles styles)
        {
            NetworkMessage<Styles> msg = new NetworkMessage<Styles>
            {
                Type = MessageType.Modifiers,
                Content = styles
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        public void SendDifficulty(SongDifficulty diff)
        {
            NetworkMessage<SongDifficulty> msg = new NetworkMessage<SongDifficulty>
            {
                Type = MessageType.Difficulty,
                Content = diff
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        public void SendProgress(SongProgress progress)
        {
            NetworkMessage<SongProgress> msg = new NetworkMessage<SongProgress>
            {
                Type = MessageType.SongProgress,
                Content = progress
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        public void SendObj<T>(MessageType type, T obj)
        {
            NetworkMessage<T> msg = new NetworkMessage<T>
            {
                Type = type,
                Content = obj
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }

        public void SendInfo(MessageType type, GameInfo info)
        {
            NetworkMessage<GameInfo> msg = new NetworkMessage<GameInfo>
            {
                Type = type,
                Content = info
            };
            Sessions.Broadcast(JsonConvert.SerializeObject(msg));

        }
    }
}
