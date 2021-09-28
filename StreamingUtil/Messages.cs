using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StreamingUtil
{

    public enum MessageType
    {
        Song = 1,
        Difficulty = 2,
        ScoreUpdate = 3,
        ScoreFinal = 4,
        SongProgress = 5,
        Modifiers = 6,
        Hits = 7,
        HitsTaken = 8,
        Config = 9,
        GameStart = 10
    }

    public struct NetworkMessage<T>
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageType Type { get; set; }
        public T Content { get; set; }
    }

    public struct Styles
    {
        public List<string> Names { get; set; }
        public List<string> IconsAsPNG { get; set; }
    }

    public struct Config
    {
        public bool EnableHitsTaken { get; set; }
        public bool EnableHits { get; set; }
        public bool EnableToggleOverlayInMenu { get; set; }
    }

    public struct GameInfo
    {
        public int Score { get; set; }
        public float Accuracy { get; set; }
        public float OnBeat { get; set; }
        public float Time { get; set; }
    }

    public struct Song
    {
        public string Name { get; set; }
        public string Artists { get; set; }
        public float Length { get; set; }
        public int BPM { get; set; }

        public string Icon { get; set; }
    }

    public struct SongProgress
    {
        public float Time { get; set; }
    }

    public struct SongDifficulty
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Difficulty Difficulty { get; set; }
    }

    

}
