using System;
using System.Collections.Generic;
using System.IO;

namespace Parch {
    class VVVFile : GameArchive {
        private BinaryReader file;
        public int numFiles { get; set; }
        private List<int> fileOffsets;
        private List<int> fileSizes;
        private List<int> reserved;
        private List<String> fileNames;

        public bool LoadFile(FileStream File) {
            if (File.Name.Substring(File.Name.LastIndexOf('\\') + 1) != "vvvvvvmusic.vvv")
                return false;
            file = new BinaryReader(File);

            file.BaseStream.Seek(0, SeekOrigin.Begin);

            numFiles = 0;
            fileOffsets = new List<int>();
            fileSizes = new List<int>();
            fileNames = new List<String>();
            reserved = new List<int>();

            int baseoffset = 128 * 60;
            if (file.BaseStream.Length < 128 * 60)
                return false;
            String filenameTmp;
            int offsetTmp;
            int reservedTmp;
            int sizeTmp;
            byte hasFile;
            for (int i = 0; i < 128; i++) {
                filenameTmp = new System.Text.ASCIIEncoding().GetString(file.ReadBytes(48)).Trim('\0');
                offsetTmp = baseoffset;
                reservedTmp = file.ReadInt32();
                sizeTmp = file.ReadInt32();
                //Console.WriteLine(offsetTmp);
                baseoffset += sizeTmp;
                hasFile = file.ReadByte();
                file.BaseStream.Seek(3, SeekOrigin.Current);
                if (hasFile > 0) {
                    fileNames.Add(filenameTmp);
                    fileOffsets.Add(offsetTmp);
                    reserved.Add(reservedTmp);
                    fileSizes.Add(sizeTmp);
                    numFiles++;
                    if ((uint)offsetTmp + (uint)sizeTmp > file.BaseStream.Length) {
                        return false;
                        //throw new Exception("Bad Offset/Size! Most likely reading a bad file");
                    }
                }
            }
            return true;
        }
        public String getFileExtensions() {
            return "vvvvvvmusic.vvv";
        }
        public String getFileType() {
            return "VVVVVV Music Archive";
        }
        public Boolean addFileType() {
            return true;
        }

        public byte[] getFile(int i) {
            file.BaseStream.Seek(fileOffsets[i], SeekOrigin.Begin);
            return file.ReadBytes(fileSizes[i]);
        }
        public void close() {
            if (this.file != null)
                this.file.Close();
        }
        public int getFileSize(int i) {
            return fileSizes[i];
        }
        public String getFileName(int i) {
            return fileNames[i].Replace("/", "\\");
        }
    }
}
