using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parch
{
    class RockX : GameArchive
    {
        private BinaryReader File;
        private BinaryReader locationFile;
        private String[] filenames;
        private Int32[] fileOffsets;
        private Int32[] fileSizes;
        public bool LoadFile(FileStream fileStream)
        {
            Console.WriteLine(fileStream.Name);
            this.File = new BinaryReader(fileStream);
            try
            {
                this.locationFile = new BinaryReader(System.IO.File.Open(fileStream.Name.Substring(0, fileStream.Name.Length - 3) + "loc", FileMode.Open));
            }
            catch
            {
                return false;
            }
            fileOffsets = new Int32[locationFile.BaseStream.Length / 8];
            fileSizes = new Int32[locationFile.BaseStream.Length / 8];
            filenames = new String[locationFile.BaseStream.Length / 8];
            for (int i = 0; i < locationFile.BaseStream.Length/8; i++)
            {
                fileOffsets[i] = locationFile.ReadInt32();
                filenames[i] = "File" + i + ".bin";
                fileSizes[i] = locationFile.ReadInt32();
            }
            numFiles = (int)(locationFile.BaseStream.Length / 8);
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
            return "rockx_pack.dat";
        }
        public String getFileType()
        {
            return "Maverick Hunter X Archive";
        }
        public Boolean addFileType()
        {
            return true;
        }
    }
}
