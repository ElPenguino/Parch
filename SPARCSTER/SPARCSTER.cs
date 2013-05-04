using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parch
{
    public class SPARCSTER : GameArchive
    {
        private BinaryReader File;
        private String[] filenames;
        private Int32[] fileOffsets;
        private Int32[] fileSizes;
        private Int32[] fileUnknowns;
        private Int32[] fileUnknowns2;
        public bool LoadFile(FileStream fileStream)
        {
            this.File = new BinaryReader(fileStream);
            int UnknownA = File.ReadInt32();
            Console.WriteLine("Header: {0}", UnknownA);
            if (UnknownA != 0xFA10)
                return false;
            numFiles = File.ReadInt32();
            int EndOfFileEntries = File.ReadInt32();
            Console.WriteLine(numFiles);
            int padding = File.ReadInt32();
            fileOffsets = new Int32[numFiles];
            fileSizes = new Int32[numFiles];
            filenames = new String[numFiles];
            fileUnknowns = new Int32[numFiles];
            fileUnknowns2 = new Int32[numFiles];
            for (int i = 0; i < numFiles; i++)
            {
                fileUnknowns[i] = File.ReadInt32();
                fileOffsets[i] = File.ReadInt32();
                filenames[i] = "File" + i + ".bin";
                fileSizes[i] = File.ReadInt32();
                fileUnknowns2[i] = File.ReadInt32();
            }
            return true;
        }
        public int numFiles { get; set; }

        public int getFileSize(int id)
        {
            return fileSizes[id];
        }
        public String getFileName(int id)
        {
            return filenames[id];
        }
        public byte[] getFile(int id)
        {
            this.File.BaseStream.Seek(fileOffsets[id], SeekOrigin.Begin);
            return this.File.ReadBytes(fileSizes[id]);
        }
        public void close()
        {
            if (this.File != null)
                File.Close();
        }
        public String getFileExtensions()
        {
            return "DATA.ARC";
        }
        public String getFileType()
        {
            return "Rocket Knight Archive";
        }
        public Boolean addFileType()
        {
            return true;
        }
    }
}
