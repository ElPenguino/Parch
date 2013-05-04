using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Parch {
    struct FNTBTableEntry {
        public int DirStart;
        public short FilePos;
        public short DirCount;

        public FNTBTableEntry(int DirStart, short FilePos, short DirCount) {
            this.DirStart = DirStart;
            this.FilePos = FilePos;
            this.DirCount = DirCount;
        }

    }
    struct Chunk {
        public string Name;
        public byte[] data;
        public Chunk(String Name, byte[] data) {
            this.Name = Name;
            this.data = data;
        }
    }
    class NARCFile : GameArchive {

        public int numFiles { get; set; }
        private string[] filenames;
        private int[] FileSizes;
        private int[] FileOffsets;
        private BinaryReader File;
        private bool littleendian;
        private List<Chunk> chunks = new List<Chunk>();
        private List<FNTBTableEntry> FNTBTable = new List<FNTBTableEntry>();
        BinaryReader FIMGChunk;

        public bool LoadFile(FileStream file) {
            File = new BinaryReader(file);

            File.BaseStream.Seek(4, SeekOrigin.Begin);

            try
            {
                if (!File.ReadBytes(4).SequenceEqual(Encoding.ASCII.GetBytes("NARC")))
                    DecompLZSS();
            }
            catch
            {
                return false;
            }
            File.BaseStream.Seek(0, SeekOrigin.Begin);

            if (!File.ReadBytes(4).SequenceEqual(Encoding.ASCII.GetBytes("NARC"))) { // Check magic bytes
                return false;
                //throw new Exception("Not a NARC");
            }
            littleendian = File.ReadUInt16().Equals(0xFFFE); // Get BOM (Byte Order Mark)

            byte[] unknownbytes = File.ReadBytes(10); // wut these do?

            BuildChunkTable();

            File.Close();

            BuildFileTable();

            BuildFilenameTable();
            chunks.Clear();
            return true;
        }
        public String getFileExtensions() {
            return "*.narc;*.carc";
        }
        public String getFileType() {
            return "Nitro Archive (NDS)";
        }
        public Boolean addFileType() {
            return true;
        }
        public int getFileSize(int i) {
            return FileSizes[i];
        }
        public String getFileName(int i) {
            return filenames[i];
        }

        private void BuildChunkTable() {
            File.BaseStream.Seek(0x10, SeekOrigin.Begin);
            while (File.BaseStream.Position < File.BaseStream.Length)
                chunks.Add(ReadChunk());
        }

        private void BuildFileTable() {
            BinaryReader FATBChunk = null;
            foreach (Chunk chunk in chunks)
                if (chunk.Name.Equals("FATB"))
                    FATBChunk = new BinaryReader(new MemoryStream(chunk.data));
            if (FATBChunk == null)
                throw new Exception("No FATB Chunk found");

            foreach (Chunk chunk in chunks)
                if (chunk.Name.Equals("FIMG"))
                    FIMGChunk = new BinaryReader(new MemoryStream(chunk.data));
            if (FIMGChunk == null)
                throw new Exception("No FIMG Chunk found");

            numFiles = FATBChunk.ReadInt32();
            FileOffsets = new int[numFiles];
            FileSizes = new int[numFiles];
            filenames = new string[numFiles];
            for (int i = 0; i < numFiles; i++) {
                FileOffsets[i] = FATBChunk.ReadInt32();
                FileSizes[i] = FATBChunk.ReadInt32() - FileOffsets[i];
                filenames[i] = i.ToString();
            }
        }

        private void BuildFilenameTable() {
            BinaryReader FNTBChunk = null;
            foreach (Chunk chunk in chunks)
                if (chunk.Name.Equals("FNTB"))
                    FNTBChunk = new BinaryReader(new MemoryStream(chunk.data));
            if (FNTBChunk == null)
                throw new Exception("No FNTB Chunk found");

            FNTBTable.Add(new FNTBTableEntry(FNTBChunk.ReadInt32(), FNTBChunk.ReadInt16(), FNTBChunk.ReadInt16()));
            for (int i = 0; i < FNTBTable[0].DirCount - 1; i++)
                FNTBTable.Add(new FNTBTableEntry(FNTBChunk.ReadInt32(), FNTBChunk.ReadInt16(), FNTBChunk.ReadInt16()));
            List<String> paths = WalkDirectoryTree(FNTBChunk, 0xF000);
            for (int i = 0; i < paths.Count; i++)
                filenames[i] = paths[i];


        }

        private List<String> WalkDirectoryTree(BinaryReader FNTBChunk, ushort id, string curPath = "", List<String> paths = null) {
            if (paths == null)
                paths = new List<String>();
            String name;
            long curPos;
            FNTBChunk.BaseStream.Seek(FNTBTable[id - 0xF000].DirStart, SeekOrigin.Begin);
            byte len = FNTBChunk.ReadByte();
            while (len != 0) {
                if ((len & 0x80) == 0x80) {
                    name = Encoding.ASCII.GetString(FNTBChunk.ReadBytes(len & 0x7F));
                    id = FNTBChunk.ReadUInt16();
                    curPos = FNTBChunk.BaseStream.Position;
                    paths = WalkDirectoryTree(FNTBChunk, id, (curPath != "" ? curPath + "\\" : "") + name, paths);
                    FNTBChunk.BaseStream.Seek(curPos, SeekOrigin.Begin);
                }
                else
                    paths.Add((curPath != "" ? curPath + "\\" : "") + Encoding.ASCII.GetString(FNTBChunk.ReadBytes(len)));
                len = FNTBChunk.ReadByte();
            }
            return paths;

        }


        private Chunk ReadChunk() {
            byte[] buffer = new byte[0];
            string chunkType = Encoding.ASCII.GetString(File.ReadBytes(4));
            if (littleendian) {
                char[] tempName = chunkType.ToCharArray();
                Array.Reverse(tempName);
                chunkType = new string(tempName);
            }
            int len = File.ReadInt32() - 8;
            if ((File.BaseStream.Position + len > File.BaseStream.Length) || (len < 0))
                throw new Exception("Chunk specified invalid length");
            buffer = File.ReadBytes(len);
            return new Chunk(chunkType, buffer);
        }
        public byte[] getFile(int i) {
            FIMGChunk.BaseStream.Seek(FileOffsets[i], SeekOrigin.Begin);
            return FIMGChunk.ReadBytes(FileSizes[i]);

        }
        private void DecompLZSS() {
            File.BaseStream.Seek(0, SeekOrigin.Begin);
            if (File.ReadByte() != 0x10)
                return;
            uint decompressed = 0;
            byte[] buffer = new byte[File.ReadUInt32()];
            MemoryStream decomp = new MemoryStream(buffer);
            byte[] ring_buffer = new byte[4113];
            for (int x = 0; x < ring_buffer.Length; x++)
                ring_buffer[x] = 0xFF;
            int r = 4096 - 18;
            uint flags = 7;
            int z = 7;
            int i;
            int j;
            byte c;
            bool testHeader = true;
            File.BaseStream.Seek(4, SeekOrigin.Begin);
            while (true) {
                if (testHeader && (decomp.Length >= 4))
                {
                    long currentLocation = decomp.Position;
                    testHeader = false;
                    decomp.Seek(0, SeekOrigin.Begin);
                    byte[] testbuffer = new byte[4];
                    decomp.Read(testbuffer,0,4);
                    if (!Encoding.ASCII.GetString(testbuffer).Equals("NARC"))
                        throw new Exception("Not compressed NARC");
                    decomp.Seek(currentLocation, SeekOrigin.Begin);
                }
                flags <<= 1;
                z++;
                if (z == 8) {
                    c = File.ReadByte();
                    if (File.BaseStream.Position >= File.BaseStream.Length)
                        break;
                    flags = c;
                    z = 0;
                }
                if (!((flags & 0x80) == 0x80)) //Raw data
                {
                    c = File.ReadByte();
                    if (File.BaseStream.Position >= File.BaseStream.Length)
                        break;
                    if (decompressed < decomp.Length) {
                        decomp.WriteByte(c);
                        ring_buffer[r++] = c;
                        r &= 4095;
                        decompressed++;
                    }
                }
                else {
                    i = File.ReadByte();
                    if (File.BaseStream.Position >= File.BaseStream.Length)
                        break;
                    j = File.ReadByte();
                    if (File.BaseStream.Position >= File.BaseStream.Length)
                        break;
                    j = j | ((i << 8) & 0xF00);
                    i = ((i >> 4) & 0xF) + 2;
                    for (int k = 0; k <= i; k++) {
                        c = ring_buffer[(r - j - 1) & (4095)];
                        if (decompressed < decomp.Length) {
                            decomp.WriteByte(c);
                            ring_buffer[r++] = c;
                            r &= 4095;
                            decompressed++;
                        }
                    }
                }
            }
            File = new BinaryReader(decomp);
        }
        public void close() {
            if (this.File != null)
                this.File.Close();
        }
    }
}
