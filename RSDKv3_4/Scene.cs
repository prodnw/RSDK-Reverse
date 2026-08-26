using System;
using System.Collections.Generic;
using System.Linq;
using RSDKv3_4;

namespace RSDKv3_4
{
    public abstract class Scene
    {
        [System.Serializable]
        public abstract class Entity
        {
            /// <summary>
            /// The type of object the entity is
            /// </summary>
            public byte type = 0;
            /// <summary>
            /// The entity's property value (aka subtype in classic sonic games)
            /// </summary>
            public byte propertyValue = 0;
            /// <summary>
            /// the x position (shifted 16-bit format, so 1.0 == (1 << 16))
            /// </summary>
            public int xpos = 0;
            /// <summary>
            /// the y position (shifted 16-bit format, so 1.0 == (1 << 16))
            /// </summary>
            public int ypos = 0;

            /// <summary>
            /// XPos but represented as a floating point number rather than a fixed point one
            /// </summary>
            public float xposF
            {
                get { return xpos / 65536.0f; }
                set { xpos = (int)(value / 65536); }
            }

            /// <summary>
            /// YPos but represented as a floating point number rather than a fixed point one
            /// </summary>
            public float yposF
            {
                get { return ypos / 65536.0f; }
                set { ypos = (int)(value / 65536); }
            }

            public abstract void Read(Reader reader);

            public abstract void Write(Writer writer);

        }

        public enum ActiveLayers
        {
            Foreground,
            Background1,
            Background2,
            Background3,
            Background4,
            Background5,
            Background6,
            Background7,
            Background8,
            None,
        }
        public enum LayerMidpoints
        {
            BeforeLayer0,
            AfterLayer0,
            AfterLayer1,
            AfterLayer2,
            AfterLayer3,
        }

        /// <summary>
        /// the stage's name (what the titlecard displays)
        /// </summary>
        public string title = "STAGE";

        /// <summary>
        /// the chunk layout for the FG layer
        /// </summary>
        public ushort[][] layout;

        /// <summary>
        /// Active Layer 0
        /// </summary>
        public ActiveLayers activeLayer0 = ActiveLayers.Background1;
        /// <summary>
        /// Active Layer 1
        /// </summary>
        public ActiveLayers activeLayer1 = ActiveLayers.None;
        /// <summary>
        /// Active Layer 2
        /// </summary>
        public ActiveLayers activeLayer2 = ActiveLayers.Foreground;
        /// <summary>
        /// Active Layer 3
        /// </summary>
        public ActiveLayers activeLayer3 = ActiveLayers.Foreground;
        /// <summary>
        /// Determines what layers should draw using high visual plane, in an example of 2, active layers 2 & 3 would use high plane tiles, while 0 & 1 would use low plane
        /// </summary>
        public LayerMidpoints layerMidpoint = LayerMidpoints.AfterLayer2;

        /// <summary>
        /// the list of entities in the stage
        /// </summary>
        public List<Entity> entities = new List<Entity>();

        /// <summary>
        /// stage width (in chunks)
        /// </summary>
        public byte width = 0;
        /// <summary>
        /// stage height (in chunks)
        /// </summary>
        public byte height = 0;

        /// <summary>
        /// the Max amount of entities that can be in a single stage
        /// </summary>
        public const int ENTITY_LIST_SIZE = 2048;

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

        /// <summary>
        /// Resizes a layer.
        /// </summary>
        /// <param name="width">The new Width</param>
        /// <param name="height">The new Height</param>
        public void resize(byte width, byte height)
        {
            // first take a backup of the current dimensions
            // then update the internal dimensions
            byte oldWidth = this.width;
            byte oldHeight = this.height;
            this.width = width;
            this.height = height;

            // resize the tile map
            System.Array.Resize(ref layout, this.height);

            // fill the extended tile arrays with "empty" values

            // if we're actaully getting shorter, do nothing!
            for (byte i = oldHeight; i < this.height; i++)
            {
                // first create arrays child arrays to the old width
                // a little inefficient, but at least they'll all be equal sized
                layout[i] = new ushort[oldWidth];
                for (int j = 0; j < oldWidth; ++j)
                    layout[i][j] = 0; // fill the new ones with blanks
            }

            for (byte y = 0; y < this.height; y++)
            {
                // now resize all child arrays to the new width
                System.Array.Resize(ref layout[y], this.width);
                for (ushort x = oldWidth; x < this.width; x++)
                    layout[y][x] = 0; // and fill with blanks if wider
            }
        }
    }
}

namespace RSDKv3
{
    public class Scene : RSDKv3_4.Scene
    {
        [System.Serializable]
        public new class Entity : RSDKv3_4.Scene.Entity
        {
            public Entity() { }

            public Entity(byte type, byte propertyValue, int xpos, int ypos) : this()
            {
                this.type = type;
                this.propertyValue = propertyValue;
                this.xpos = xpos;
                this.ypos = ypos;
            }

            public Entity(Reader reader) : this()
            {
                Read(reader);
            }

            public override void Read(Reader reader)
            {
                // entity type, 1 byte, unsigned
                type = reader.ReadByte();
                // Property value, 1 byte, unsigned
                propertyValue = reader.ReadByte();

                // X Position, 2 bytes, big-endian, signed			
                xpos = (short)(reader.ReadSByte() << 8);
                xpos |= reader.ReadByte();
                xpos <<= 16;

                // Y Position, 2 bytes, big-endian, signed
                ypos = (short)(reader.ReadSByte() << 8);
                ypos |= reader.ReadByte();
                ypos <<= 16;
            }

            public override void Write(Writer writer)
            {
                writer.Write(type);
                writer.Write(propertyValue);

                writer.Write((byte)(xpos >> 24));
                writer.Write((byte)((xpos >> 16) & 0xFF));

                writer.Write((byte)(ypos >> 24));
                writer.Write((byte)((ypos >> 16) & 0xFF));
            }
        }

        /// <summary>
        /// a list of names for each Object Type
        /// </summary>
        public List<string> objectTypeNames = new List<string>();

        public Scene()
        {
            layout = new ushort[1][];
            layout[0] = new ushort[1];
        }

        public Scene(string filename) : this(new Reader(filename)) { }

        public Scene(System.IO.Stream stream) : this(new Reader(stream)) { }

        public Scene(Reader reader)
        {
            Read(reader);
        }

        public override void Read(Reader reader)
        {
            title = reader.ReadStringRSDK();

            activeLayer0 = (ActiveLayers)reader.ReadByte();
            activeLayer1 = (ActiveLayers)reader.ReadByte();
            activeLayer2 = (ActiveLayers)reader.ReadByte();
            activeLayer3 = (ActiveLayers)reader.ReadByte();
            layerMidpoint = (LayerMidpoints)reader.ReadByte();

            // Map width/height in 128 pixel units
            // In RSDKv3 it's one byte long

            width = reader.ReadByte();
            height = reader.ReadByte();

            layout = new ushort[height][];
            for (int i = 0; i < height; i++)
                layout[i] = new ushort[width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 128x128 Block number is 16-bit
                    // Big-Endian in RSDKv3
                    layout[y][x] = (ushort)(reader.ReadByte() << 8);
                    layout[y][x] |= reader.ReadByte();
                }
            }

            // Read number of object types
            int objectTypeCount = reader.ReadByte();

            objectTypeNames.Clear();
            for (int n = 0; n < objectTypeCount; n++)
                objectTypeNames.Add(reader.ReadStringRSDK());

            // Read entities

            // 2 bytes, Big-Endian, unsigned
            int entityCount = reader.ReadByte() << 8;
            entityCount |= reader.ReadByte();

            entities.Clear();
            for (int n = 0; n < entityCount; n++)
                entities.Add(new Entity(reader));

            reader.Close();
        }

        public override void Write(Writer writer)
        {
            // Write zone name		
            writer.WriteStringRSDK(title);

            // Write the active layers & midpoint
            writer.Write((byte)activeLayer0);
            writer.Write((byte)activeLayer1);
            writer.Write((byte)activeLayer2);
            writer.Write((byte)activeLayer3);
            writer.Write((byte)layerMidpoint);

            // Write width and height
            writer.Write(width);
            writer.Write(height);

            // Write tile layout
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    writer.Write((byte)(layout[h][w] >> 8));
                    writer.Write((byte)(layout[h][w] & 0xff));
                }
            }

            // Write number of object types
            writer.Write((byte)objectTypeNames.Count);

            // Write object type names
            // Ignore first object type (Blank Object), it is not stored.
            foreach (string typeName in objectTypeNames)
                writer.WriteStringRSDK(typeName);

            // Write number of entities
            writer.Write((byte)(entities.Count >> 8));
            writer.Write((byte)(entities.Count & 0xFF));

            // Write entities
            foreach (Entity entity in entities)
                entity.Write(writer);

            writer.Close();
        }
    }
}

namespace RSDKv4
{
    public class Scene : RSDKv3_4.Scene
    {
        [System.Serializable]
        public new class Entity : RSDKv3_4.Scene.Entity
        {
            public enum InkEffects { None, Blend, Alpha, Add, Sub }

            public enum Priorities { Bounds, Active, Always, XBounds, XBoundsDestroy, Inactive, BoundsSmall, ActiveSmall }

            public int? state;
            public Tiles128x128.Block.Tile.Directions? direction;
            public int? scale;
            public int? rotation;
            public byte? drawOrder;
            public Priorities? priority;
            public byte? alpha;
            public byte? animation;
            public int? animationSpeed;
            public byte? frame;
            public InkEffects? inkEffect;
            public int? value0;
            public int? value1;
            public int? value2;
            public int? value3;

            public Entity()
            {
            }

            public Entity(byte type, byte propertyValue, int xpos, int ypos)
            {
                this.type = type;
                this.propertyValue = propertyValue;
                this.xpos = xpos;
                this.ypos = ypos;
            }

            public Entity(Reader reader)
            {
                Read(reader);
            }

            public override void Read(Reader reader)
            {
                //Variable flags, 2 bytes, unsigned
                ushort flags = reader.ReadUInt16();

                // entity type, 1 byte, unsigned
                type = reader.ReadByte();

                // PropertyValue, 1 byte, unsigned
                propertyValue = reader.ReadByte();

                //a position, made of 8 bytes, 4 for X, 4 for Y
                xpos = reader.ReadInt32();
                ypos = reader.ReadInt32();

                if ((flags & 1) != 0)
                    state = reader.ReadInt32();
                else
                    state = null;
                if ((flags & 2) != 0)
                    direction = (Tiles128x128.Block.Tile.Directions)reader.ReadByte();
                else
                    direction = null;
                if ((flags & 4) != 0)
                    scale = reader.ReadInt32();
                else
                    scale = null;
                if ((flags & 8) != 0)
                    rotation = reader.ReadInt32();
                else
                    rotation = null;
                if ((flags & 16) != 0)
                    drawOrder = reader.ReadByte();
                else
                    drawOrder = null;
                if ((flags & 32) != 0)
                    priority = (Priorities)reader.ReadByte();
                else
                    priority = null;
                if ((flags & 64) != 0)
                    alpha = reader.ReadByte();
                else
                    alpha = null;
                if ((flags & 128) != 0)
                    animation = reader.ReadByte();
                else
                    animation = null;
                if ((flags & 256) != 0)
                    animationSpeed = reader.ReadInt32();
                else
                    animationSpeed = null;
                if ((flags & 512) != 0)
                    frame = reader.ReadByte();
                else
                    frame = null;
                if ((flags & 1024) != 0)
                    inkEffect = (InkEffects)reader.ReadByte();
                else
                    inkEffect = null;
                if ((flags & 2048) != 0)
                    value0 = reader.ReadInt32();
                else
                    value0 = null;
                if ((flags & 4096) != 0)
                    value1 = reader.ReadInt32();
                else
                    value1 = null;
                if ((flags & 8192) != 0)
                    value2 = reader.ReadInt32();
                else
                    value2 = null;
                if ((flags & 16384) != 0)
                    value3 = reader.ReadInt32();
                else
                    value3 = null;
            }

            public override void Write(Writer writer)
            {
                int flags = 0;
                if (state.HasValue)
                    flags |= 1;
                if (direction.HasValue)
                    flags |= 2;
                if (scale.HasValue)
                    flags |= 4;
                if (rotation.HasValue)
                    flags |= 8;
                if (drawOrder.HasValue)
                    flags |= 16;
                if (priority.HasValue)
                    flags |= 32;
                if (alpha.HasValue)
                    flags |= 64;
                if (animation.HasValue)
                    flags |= 128;
                if (animationSpeed.HasValue)
                    flags |= 256;
                if (frame.HasValue)
                    flags |= 512;
                if (inkEffect.HasValue)
                    flags |= 1024;
                if (value0.HasValue)
                    flags |= 2048;
                if (value1.HasValue)
                    flags |= 4096;
                if (value2.HasValue)
                    flags |= 8192;
                if (value3.HasValue)
                    flags |= 16384;
                writer.Write((ushort)flags);

                writer.Write(type);
                writer.Write(propertyValue);

                writer.Write(xpos);
                writer.Write(ypos);

                if (state.HasValue)
                    writer.Write(state.Value);
                if (direction.HasValue)
                    writer.Write((byte)direction.Value);
                if (scale.HasValue)
                    writer.Write(scale.Value);
                if (rotation.HasValue)
                    writer.Write(rotation.Value);
                if (drawOrder.HasValue)
                    writer.Write(drawOrder.Value);
                if (priority.HasValue)
                    writer.Write((byte)priority.Value);
                if (alpha.HasValue)
                    writer.Write(alpha.Value);
                if (animation.HasValue)
                    writer.Write(animation.Value);
                if (animationSpeed.HasValue)
                    writer.Write(animationSpeed.Value);
                if (frame.HasValue)
                    writer.Write(frame.Value);
                if (inkEffect.HasValue)
                    writer.Write((byte)inkEffect.Value);
                if (value0.HasValue)
                    writer.Write(value0.Value);
                if (value1.HasValue)
                    writer.Write(value1.Value);
                if (value2.HasValue)
                    writer.Write(value2.Value);
                if (value3.HasValue)
                    writer.Write(value3.Value);
            }

        }

        public Scene()
        {
            layout = new ushort[1][];
            layout[0] = new ushort[1];
        }

        public Scene(string filename) : this(new Reader(filename)) { }

        public Scene(System.IO.Stream stream) : this(new Reader(stream)) { }

        public Scene(Reader reader)
        {
            Read(reader);
        }

        public override void Read(Reader reader)
        {
            title = reader.ReadStringRSDK();

            activeLayer0  = (ActiveLayers)reader.ReadByte();
            activeLayer1  = (ActiveLayers)reader.ReadByte();
            activeLayer2  = (ActiveLayers)reader.ReadByte();
            activeLayer3  = (ActiveLayers)reader.ReadByte();
            layerMidpoint = (LayerMidpoints)reader.ReadByte();

            // Map width in 128 pixel units
            // In RSDKv4 it's one byte long (with an unused byte after each one), little-endian

            width = reader.ReadByte();
            reader.ReadByte(); // Unused

            height = reader.ReadByte();
            reader.ReadByte(); // Unused

            layout = new ushort[height][];
            for (int i = 0; i < height; i++)
                layout[i] = new ushort[width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 128x128 Block number is 16-bit
                    // Little-Endian in RSDKv4	
                    layout[y][x] = reader.ReadByte();
                    layout[y][x] |= (ushort)(reader.ReadByte() << 8);
                }
            }

            // Read entities

            // 2 bytes, little-endian, unsigned
            int entityCount = reader.ReadByte();
            entityCount |= reader.ReadByte() << 8;

            entities.Clear();
            for (int o = 0; o < entityCount; o++)
                entities.Add(new Entity(reader));

            reader.Close();
        }

        public override void Write(Writer writer)
        {
            // Write zone name		
            writer.WriteStringRSDK(title);

            // Write the active layers & midpoint
            writer.Write((byte)activeLayer0);
            writer.Write((byte)activeLayer1);
            writer.Write((byte)activeLayer2);
            writer.Write((byte)activeLayer3);
            writer.Write((byte)layerMidpoint);

            // Write width
            writer.Write(width);
            writer.Write((byte)0);

            // Write height
            writer.Write(height);
            writer.Write((byte)0);

            // Write tile layout
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    writer.Write((byte)(layout[h][w] & 0xFF));
                    writer.Write((byte)(layout[h][w] >> 8));
                }
            }

            // Write number of entities

            writer.Write((byte)(entities.Count & 0xFF));
            writer.Write((byte)(entities.Count >> 8));

            // Write entities
            foreach (Entity entity in entities)
                entity.Write(writer);

            writer.Close();

        }
    }
}
