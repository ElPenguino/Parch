using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GameArchiver {
    class AFSFile : GameArchive {
        private BinaryReader File;
        private String[] filenames;
        private Int32[] fileOffsets;
        private Int32[] fileSizes;
        private byte[][] unknowns;
        public AFSFile() {

        }
        public bool LoadFile(FileStream fileStream) {
            this.File = new BinaryReader(fileStream);

            if (!this.File.ReadBytes(4).SequenceEqual(Encoding.ASCII.GetBytes("AFS\0"))) {
                return false;
                //throw new Exception("Not a valid AFS File");
            }
            numFiles = this.File.ReadInt32();

            if ((numFiles + 2) * 8 > File.BaseStream.Length) {
                return false;
                //throw new Exception("File too small!");
            }
            
            fileOffsets = new int[numFiles];
            fileSizes = new int[numFiles];
            for (int i = 0; i < numFiles; i++) {
                fileOffsets[i] = this.File.ReadInt32();
                fileSizes[i] = this.File.ReadInt32();
            }

            int fileNameOffset = this.File.ReadInt32();
            int fileNameSize = this.File.ReadInt32();

            this.File.BaseStream.Seek(fileNameOffset, SeekOrigin.Begin);

            
            unknowns = new byte[numFiles][];
            filenames = new String[numFiles];
            String tmpfilename;
            for (int i = 0; i < numFiles; i++) {
                tmpfilename = new String(this.File.ReadChars(0x20)).Trim('\0');
                while (filenames.Contains(tmpfilename))
                    tmpfilename += "_";
                filenames[i] = tmpfilename;
                unknowns[i] = this.File.ReadBytes(0x10);
            }
            return true;
        }
        public int numFiles { get; set; }

        public int getFileSize(int id) {
            return fileSizes[id];
        }
        public String getFileName(int id) {
            return filenames[id];
        }
        public byte[] getFile(int id) {
            this.File.BaseStream.Seek(fileOffsets[id], SeekOrigin.Begin);
            return this.File.ReadBytes(fileSizes[id]);
        }
        public void close() {
            if (this.File != null)
                File.Close();
        }
        public String getFileExtensions() {
            return "*.afs";
        }
        public String getFileType() {
            return "AFS Archive";
        }
        public Boolean addFileType() {
            return true;
        }
    }
}
