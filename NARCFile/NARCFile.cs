using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GameArchiver
{
    class NARCFile : GameArchive,IEnumerable {

        public int numFiles { get; set; }
        private byte[][] files;
        private string[] filenames;
        private int[] FileSizes;
        private int[] FileOffsets;
        private BinaryReader File;
        private bool littleendian;
        private List<Tuple<string, byte[]>> chunks = new List<Tuple<string, byte[]>>();
        private List<Tuple<int, short, short>> FNTBTable = new List<Tuple<int, short, short>>();

        public bool LoadFile(FileStream file)
        {
            File = new BinaryReader(file);

            File.BaseStream.Seek(4, SeekOrigin.Begin);

            if (!File.ReadBytes(4).SequenceEqual(Encoding.ASCII.GetBytes("NARC")))
                DecompLZSS();

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

        private void BuildFileTable()
        {
            BinaryReader FATBChunk = null;
            foreach (Tuple<string, byte[]> chunk in chunks)
                if (chunk.Item1.Equals("FATB"))
                   FATBChunk = new BinaryReader(new MemoryStream(chunk.Item2));
            if (FATBChunk == null)
                throw new Exception("No FATB Chunk found");

            BinaryReader FIMGChunk = null;
            foreach (Tuple<string, byte[]> chunk in chunks)
                if (chunk.Item1.Equals("FIMG"))
                    FIMGChunk = new BinaryReader(new MemoryStream(chunk.Item2));
            if (FIMGChunk == null)
                throw new Exception("No FIMG Chunk found");

            files = new byte[FATBChunk.ReadInt32()][];
            FileOffsets = new int[files.Length];
            FileSizes = new int[files.Length];
            filenames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                FileOffsets[i] = FATBChunk.ReadInt32();
                FileSizes[i] = FATBChunk.ReadInt32()-FileOffsets[i];
                filenames[i] = i.ToString();
            }

            for (int i = 0; i < files.Length; i++)
            {
                FIMGChunk.BaseStream.Seek(FileOffsets[i], SeekOrigin.Begin);
                files[i] = FIMGChunk.ReadBytes(FileSizes[i]);
            }
            numFiles = files.Length;
        }

        private void BuildFilenameTable()
        {
            BinaryReader FNTBChunk = null;
            foreach (Tuple<string, byte[]> chunk in chunks)
                if (chunk.Item1.Equals("FNTB"))
                    FNTBChunk = new BinaryReader(new MemoryStream(chunk.Item2));
            if (FNTBChunk == null)
                throw new Exception("No FNTB Chunk found");

            FNTBTable.Add(new Tuple<int, short, short>(FNTBChunk.ReadInt32(), FNTBChunk.ReadInt16(), FNTBChunk.ReadInt16()));
            for (int i = 0; i < FNTBTable[0].Item3-1; i++)
                FNTBTable.Add(new Tuple<int, short, short>(FNTBChunk.ReadInt32(), FNTBChunk.ReadInt16(), FNTBChunk.ReadInt16()));
            List<String> paths = WalkDirectoryTree(FNTBChunk, 0xF000);
            for (int i = 0; i < paths.Count; i++)
                filenames[i] = paths[i];
                

        }

        private List<String> WalkDirectoryTree(BinaryReader FNTBChunk, ushort id, string curPath = "", List<String> paths = null)
        {
            if (paths == null)
                paths = new List<String>();
            String name;
            long curPos;
            FNTBChunk.BaseStream.Seek(FNTBTable[id - 0xF000].Item1, SeekOrigin.Begin);
            byte len = FNTBChunk.ReadByte();
            while (len != 0)
            {
                if ((len & 0x80) == 0x80)
                {
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

        public String[] ListFiles()
        {
            return filenames;
        }


        private Tuple<string, byte[]> ReadChunk()
        {
            byte[] buffer = new byte[0];
            string chunkType = Encoding.ASCII.GetString(File.ReadBytes(4));
            if (littleendian)
            {
                char[] tempName = chunkType.ToCharArray();
                Array.Reverse(tempName);
                chunkType = new string(tempName);
            }
            int len = File.ReadInt32() - 8;
            if ((File.BaseStream.Position + len > File.BaseStream.Length) || (len < 0))
                throw new Exception("Chunk specified invalid length");
            buffer = File.ReadBytes(len);
            return new Tuple<string, byte[]>(chunkType, buffer);
        }
        public byte[] getFile(int i)
        {
            if ((i > files.Length) || (i < 0))
                throw new Exception("File Not Found");
            return files[i];

        }
        public Tuple<string, byte[]> GetFile(string filename)
        {
            if (!filenames.Contains(filename))
                throw new Exception("File Not Found");
            for (int i = 0; i < filenames.Length; i++)
                if (filenames[i].Equals(filename))
                    return new Tuple<string, byte[]>(filenames[i], files[i]);
            return null;
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < files.Length; i++)
                yield return getFile(i);
        }
        private void DecompLZSS()
        {
            File.BaseStream.Seek(0, SeekOrigin.Begin);
            if (File.ReadByte() != 0x10)
               return;
            uint decompressed = 0;
            MemoryStream decomp = new MemoryStream(new byte[File.ReadUInt32()]);
            byte[] ring_buffer = new byte[4113];
            for (int x = 0; x < ring_buffer.Length; x++)
                ring_buffer[x] = 0xFF;
            int r = 4096 - 18;
            uint flags = 7;
            int z = 7;
            int i;
            int j;
            byte c;
            File.BaseStream.Seek(4, SeekOrigin.Begin);
            while (true)
            {
                flags <<= 1;
                z++;
                if (z == 8)
                {
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
                    if (decompressed < decomp.Length)
                    {
                        decomp.WriteByte(c);
                        ring_buffer[r++] = c;
                        r &= 4095;
                        decompressed++;
                    }
                }
                else
                {
                    i = File.ReadByte();
                    if (File.BaseStream.Position >= File.BaseStream.Length)
                        break;
                    j = File.ReadByte();
                    if (File.BaseStream.Position >= File.BaseStream.Length)
                        break;
                    j = j | ((i << 8) & 0xF00);
                    i = ((i >> 4) & 0xF) + 2;
                    for (int k = 0; k <= i; k++)
                    {
                        c = ring_buffer[(r - j - 1) & (4095)];
                        if (decompressed < decomp.Length)
                        {
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
