using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;

using HarmonyLib;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using Il2Cpp;


namespace StreamingUtil
{
    public class Entry : MelonMod
    {
        WebSocketServer server;

        float timer = 0;
        float tickRate = 1;

        //<modifier Name, png as base64>
        private Dictionary<string, string> modifierLookup = new Dictionary<string, string>();
        //
        private Dictionary<string, string> posterLookup = new Dictionary<string, string>();

        //TODO config to enable or disable hits taken and hits
        MelonPreferences_Category config;
        MelonPreferences_Entry<bool> config_enableHitsTaken;
        MelonPreferences_Entry<bool> config_enableHits;
        MelonPreferences_Entry<bool> config_disableOverlayInMenu;

        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            Messenger.Default.Register<Messages.ModifiersSet>(new Action<Messages.ModifiersSet>(OnModifiersSet));
            Messenger.Default.Register<Messages.LevelSelectEvent>(new Action<Messages.LevelSelectEvent>(OnLevelSelected));
            Messenger.Default.Register<Messages.GameScoreEvent>(new Action<Messages.GameScoreEvent>(OnGameScore));
            Messenger.Default.Register<Messages.GameStartEvent>(new Action<Messages.GameStartEvent>(OnGameStart));


            Enhancements.Transport.Messenger.Default.Register<Enhancements.Transport.Messages.UpdatedInternalScore>(OnInternalScore);
            Enhancements.Transport.Messenger.Default.Register<Enhancements.Transport.Messages.UpdateHits>(OnHits);
            Enhancements.Transport.Messenger.Default.Register<Enhancements.Transport.Messages.UpdateHitsTaken>(OnHitsTaken);

            config = MelonPreferences.CreateCategory("Config");
            config.SetFilePath("UserData/StreamUtil_config.cfg");
            config_enableHitsTaken = config.CreateEntry("enableHitsTaken", true);
            config_enableHits = config.CreateEntry("enableHits", true);
            config_disableOverlayInMenu = config.CreateEntry("disableOverlayInMenu", false);
            config.SaveToFile();


            MelonLogger.Msg("Starting websocket server");

            server = new WebSocketServer("ws://localhost:9000");
            server.AddWebSocketService<Server>("/");
            server.Start();
            Server.config = new Config
            {
                EnableHits = config_enableHits.Value,
                EnableHitsTaken = config_enableHitsTaken.Value,
                EnableToggleOverlayInMenu = config_disableOverlayInMenu.Value
            };
        }

        private void OnGameStart(Messages.GameStartEvent obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;

            Server.Default.SendObj(MessageType.GameStart, "placeholder");
        }

        private void SetConfig()
        {

        }

        private void OnHitsTaken(Enhancements.Transport.Messages.UpdateHitsTaken obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;

            if (config_enableHitsTaken.Value)
                Server.Default.SendObj(MessageType.HitsTaken, obj.HitsTaken);
        }

        private void OnHits(Enhancements.Transport.Messages.UpdateHits obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;

            if (config_enableHits.Value)
                Server.Default.SendObj(MessageType.Hits, obj.Hits);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Server.Default == null || !Server.Default.AnyConnected)
                return; 

            if (GameManager.Initialized &&  GameManager.GameOn)
            {
                if (timer < Time.time)
                {
                    timer = Time.time + 1 / tickRate;

                    Server.Default.SendProgress(new SongProgress { Time = GameManager.GameTime });
                }
            }
        }

        private void OnInternalScore(Enhancements.Transport.Messages.UpdatedInternalScore obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;
            GameInfo info = new GameInfo
            {
                Score = obj.Score,
                Accuracy = obj.Accuracy,
                OnBeat = obj.OnBeatAccuracy,
                Time = GameManager.GameProgress
            };
            Server.Default.SendInfo(MessageType.ScoreUpdate, info);
        }

        private void OnGameScore(Messages.GameScoreEvent obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;

            GameInfo info = new GameInfo
            {
                Score = obj.data.score,
                Accuracy = obj.data.accuracy,
                OnBeat = obj.data.onBeat,
                Time = 0
            };
            Server.Default.SendInfo(MessageType.ScoreFinal, info);
        }

        private void OnLevelSelected(Messages.LevelSelectEvent obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;

            if (!LevelMetaDatabase.Initialized)
            {
                return;
            }


            string base64Img;
            string key = obj.level.data.__iv_sceneName + obj.level.data.__iv_songName; //Try avoid collisions with custom levels
            if (!posterLookup.TryGetValue(key, out base64Img))
            {
                Sprite posterSprite = LevelMetaDatabase.Current.GetPosterSpriteFromLevelData(obj.level.data);
                Texture2D poster = TexFromSprite(posterSprite);
                var png = ImageConversion.EncodeToPNG(poster);
                base64Img = Convert.ToBase64String(png.ToArray());
                posterLookup.Add(key, base64Img);
            }


            Song song = new Song
            {
                Name = obj.level.data.__iv_sceneName,
                Length = obj.level.data.songLength,
                Artists = LevelMetaDatabase.CachedCurrent.GetArtistFromLevelData(obj.level.data),
                BPM = LevelMetaDatabase.CachedCurrent.GetTempoFromLevelData(obj.level.data),
                Icon = base64Img
            };

            Server.Default.SendSong(song);

            //Figure out where we can call this to set the initial difficulty
            Server.Default.SendDifficulty(new SongDifficulty { Difficulty = GameManager.GetDifficulty() });
        }

        private void OnModifiersSet(Messages.ModifiersSet obj)
        {
            if (Server.Default == null || !Server.Default.AnyConnected)
                return;

            var styles = new Styles();

            List<string> modifierImages = new List<string>();
            foreach (var mod in obj.modifiers)
            {
                string base64Img;
                if (!modifierLookup.TryGetValue(mod.Name, out base64Img))
                {
                    //MelonLogger.Msg($"Adding modifier icon to cache {mod.Name}");
                    Texture2D modIcon = TexFromSprite(mod.menuIcon);
                    var png = ImageConversion.EncodeToPNG(modIcon);
                    base64Img = Convert.ToBase64String(png.ToArray());
                    modifierLookup.Add(mod.Name, base64Img);
                }
                modifierImages.Add(base64Img);
            }

            styles.Names = obj.modifiers.ToArray().Select(m => m.Name).ToList();
            styles.IconsAsPNG = modifierImages;
            Server.Default.SendModifiers(styles);
        }

#region Patches

        [HarmonyPatch(typeof(CncrgDifficultyButtonController), "DifficultyButtonHandler", new Type[0] { })]
        private static class DifficultyButtonController_Mod
        {
            public static void Postfix(CncrgDifficultyButtonController __instance)
            {
                if (Server.Default == null || !Server.Default.AnyConnected)
                    return;

                SongDifficulty difficulty = new SongDifficulty
                {
                    Difficulty = __instance.Difficulty,
                };

                Server.Default.SendDifficulty(difficulty);
            }
        }

#if DEBUG
        [HarmonyPatch(typeof(UIStateController), "OnReturnToMainMenu")]
        private static class ReturnToMenuHook
        {
            public static void Postfix()
            {

                MelonLogger.Msg("Return To Menu");
            }
        }
#endif
#endregion

#region Util
        private static Texture2D TexFromNonReadableSprite(Sprite sprite)
        {
            Texture2D nonReadableTex = sprite.texture;

            RenderTexture tmp = RenderTexture.GetTemporary(
                nonReadableTex.width,
                nonReadableTex.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(nonReadableTex, tmp);

            //Backup current texture
            RenderTexture previous = RenderTexture.active;

            //Set our temp RT to be active so we can read from it.
            RenderTexture.active = tmp;

            //Write the nonReadable texture to a readable texture
            Texture2D readableTex = new Texture2D(nonReadableTex.width, nonReadableTex.height);
            readableTex.ReadPixels(new Rect(0, 0, nonReadableTex.width, nonReadableTex.height), 0, 0);
            readableTex.Apply();

            //Reset active rendertexture
            RenderTexture.active = previous;
            tmp.Release();

            return readableTex;
        }

        private static Texture2D TexFromSprite(Sprite sprite)
        {
            var tex = Decompress(sprite.texture);
            var croppedTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            //MelonLogger.Msg($"{(int)sprite.rect.width}, {(int)sprite.rect.height}");
            var pixels = tex.GetPixels(
                            (int)sprite.rect.x,
                            (int)sprite.rect.y,
                            (int)sprite.rect.width,
                            (int)sprite.rect.height);
            croppedTexture.SetPixels(pixels);

            croppedTexture.Apply();
            return croppedTexture;
        }

        private static Texture2D Decompress(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
#endregion
    }
}
