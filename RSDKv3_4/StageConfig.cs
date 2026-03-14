using System.Collections.Generic;
using RSDKv3_4;

namespace RSDKv3_4
{
    public abstract class StageConfig
    {
        /// <summary>
        /// the stageconfig palette (index 96-128)
        /// </summary>
        public Palette stagePalette = new Palette();

        /// <summary>
        /// the list of stage-specific SoundFX
        /// </summary>
        public List<GameConfig.SoundInfo> soundFX = new List<GameConfig.SoundInfo>();

        /// <summary>
        /// the list of stage-specific objects
        /// </summary>
        public List<GameConfig.ObjectInfo> objects = new List<GameConfig.ObjectInfo>();

        /// <summary>
        /// whether or not to load the global objects in this stage
        /// </summary>
        public bool loadGlobalObjects = true;

        public abstract void Read(Reader reader);

        public void Write(string filename)
        {
            using (Writer writer = new Writer(filename))
                Write(writer);
        }

        public void Write(System.IO.Stream stream)
        {
            using (Writer writer = new Writer(stream))
                Write(writer);
        }

        public abstract void Write(Writer writer);

    }
}

namespace RSDKv3
{
    public class StageConfig : RSDKv3_4.StageConfig
    {
        public StageConfig() { }

        public StageConfig(string filename) : this(new Reader(filename)) { }

        public StageConfig(System.IO.Stream stream) : this(new Reader(stream)) { }

        public StageConfig(Reader reader)
        {
            Read(reader);
        }

        public override void Read(Reader reader)
        {
            // General
            loadGlobalObjects = reader.ReadBoolean();

            // Palettes
            stagePalette.Read(reader, 2);

            // Objects
            objects.Clear();
            byte objectCount = reader.ReadByte();
            for (int i = 0; i < objectCount; ++i)
            {
                GameConfig.ObjectInfo info = new GameConfig.ObjectInfo();
                info.name = reader.ReadStringRSDK();

                objects.Add(info);
            }

            foreach (GameConfig.ObjectInfo info in objects)
                info.script = reader.ReadStringRSDK();

            // SoundFX
            soundFX.Clear();
            byte sfxCount = reader.ReadByte();
            for (int i = 0; i < sfxCount; ++i)
            {
				RSDKv3_4.GameConfig.SoundInfo item = new RSDKv3_4.GameConfig.SoundInfo() { path = reader.ReadStringRSDK() };
                item.name = System.IO.Path.GetFileNameWithoutExtension(item.path);
                soundFX.Add(item);
            }

            reader.Close();
        }

        public override void Write(Writer writer)
        {
            // General
            writer.Write(loadGlobalObjects);

            // Palettes
            stagePalette.Write(writer);

            // Objects
            writer.Write((byte)objects.Count);

            foreach (GameConfig.ObjectInfo info in objects)
                writer.WriteStringRSDK(info.name);

            foreach (GameConfig.ObjectInfo info in objects)
                writer.WriteStringRSDK(info.script);

            // SoundFX
            writer.Write((byte)soundFX.Count);

            foreach (var path in soundFX)
                writer.WriteStringRSDK(path.path);

            writer.Close();

        }

    }
}

namespace RSDKv4
{
    public class StageConfig : RSDKv3_4.StageConfig
    {
        public StageConfig() { }

        public StageConfig(string filename) : this(new Reader(filename)) { }

        public StageConfig(System.IO.Stream stream) : this(new Reader(stream)) { }

        public StageConfig(Reader reader)
        {
            Read(reader);
        }

        public override void Read(Reader reader)
        {
            // General
            loadGlobalObjects = reader.ReadBoolean();

            // Palettes
            stagePalette.Read(reader, 2);

            // SoundFX
            soundFX.Clear();
            byte sfxCount = reader.ReadByte();
            for (int i = 0; i < sfxCount; ++i)
            {
                GameConfig.SoundInfo info = new GameConfig.SoundInfo();
                info.name = reader.ReadStringRSDK();

                soundFX.Add(info);
            }

            foreach (GameConfig.SoundInfo info in soundFX)
                info.path = reader.ReadStringRSDK();

            // Objects
            objects.Clear();
            byte objectCount = reader.ReadByte();
            for (int i = 0; i < objectCount; ++i)
            {
                GameConfig.ObjectInfo info = new GameConfig.ObjectInfo();
                info.name = reader.ReadStringRSDK();

                objects.Add(info);
            }

            foreach (GameConfig.ObjectInfo info in objects)
                info.script = reader.ReadStringRSDK();

            reader.Close();
        }

        public override void Write(Writer writer)
        {
            // General
            writer.Write(loadGlobalObjects);

            // Palettes
            stagePalette.Write(writer);

            // SoundFX
            writer.Write((byte)soundFX.Count);

            foreach (GameConfig.SoundInfo info in soundFX)
                writer.WriteStringRSDK(info.name);

            foreach (GameConfig.SoundInfo info in soundFX)
                writer.WriteStringRSDK(info.path);

            // Objects
            writer.Write((byte)objects.Count);

            foreach (GameConfig.ObjectInfo info in objects)
                writer.WriteStringRSDK(info.name);

            foreach (GameConfig.ObjectInfo info in objects)
                writer.WriteStringRSDK(info.script);

            writer.Close();
        }

    }
}
