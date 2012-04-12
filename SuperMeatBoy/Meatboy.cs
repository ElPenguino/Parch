using System;
using System.Collections;
using System.IO;

namespace GameArchiver {
    struct DirInfo {
        public int a;
        public int b;

        public DirInfo(int a, int b) {
            this.a = a;
            this.b = b;
        }

    }
    struct FileInfo {
        public int offset;
        public int size;
        public int dir;

        public FileInfo(int offset, int size, int dir) {
            this.offset = offset;
            this.size = size;
            this.dir = dir;
        }

    }
    class MeatBoy : GameArchive, IEnumerable
    {
        private BinaryReader File;
        public int numFiles { get; set; }
        private int[] fileoffsets;
        private int[] filesizes;
        private int[] unknowns;
        private string[] filenames;

        public bool LoadFile(FileStream file)
        {
            File = new BinaryReader(file);

            File.BaseStream.Seek(0, SeekOrigin.Begin);

            if (file.Name.Substring(file.Name.LastIndexOf('\\')+1) == "gameaudio.dat") {
                byte[] header = File.ReadBytes(0x18); // wut these do?
                numFiles = header[20] + (header[21] << 8);
                fileoffsets = new int[numFiles];
                filesizes = new int[numFiles];
                filenames = new string[numFiles];
                unknowns = new int[numFiles];

                for (int i = 0; i < numFiles; i++) {
                    fileoffsets[i] = File.ReadInt32();
                    filesizes[i] = File.ReadInt32();
                    if ((uint)fileoffsets[i] + (uint)filesizes[i] > File.BaseStream.Length) {
                        //throw new Exception("Bad Offset/Size! Most likely reading a bad file");
                        return false;
                    }
                    unknowns[i] = File.ReadInt32();
                }
                byte[] header2 = File.ReadBytes(0x18); //more???

                string tempname = null;
                byte tempchar;
                for (int i = 0; i < numFiles; i++) {
                    for (tempchar = File.ReadByte(); tempchar != 0; tempchar = File.ReadByte())
                        tempname = tempname + (char)tempchar;
                    filenames[i] = tempname;
                    tempname = null;
                }
                return true;
            }
            else if (file.Name.Substring(file.Name.LastIndexOf('\\') + 1) == "gamedata.dat") {
                int numdirs = File.ReadInt32();
                DirInfo[] dirData = new DirInfo[numdirs];

                for (int i = 0; i < numdirs; i++)
                    dirData[i] = new DirInfo(File.ReadInt32(), File.ReadInt32());
                //Console.WriteLine("Num file pos: {0:X}", File.BaseStream.Position);
                int numfiles = File.ReadInt32();
                //Console.WriteLine("Number of files: {0:X}", numfiles);
                FileInfo[] fileData = new FileInfo[numfiles];

                for (int i = 0; i < numfiles; i++)
                    fileData[i] = new FileInfo(File.ReadInt32(), File.ReadInt32(), File.ReadInt32());

                //Console.WriteLine("Dir name pos: {0:X}", File.BaseStream.Position);
                int dirnamesize = File.ReadInt32();
                int filenamesize = File.ReadInt32();

                String[] rawfilenames = new String[numfiles];

                String[] dirnames = new String[numdirs];
                int x = 0;
                byte temp;
                dirnames[0] = "";
                for (int i = 0; i < dirnamesize; i++) {
                    temp = File.ReadByte();
                    if (temp == 0) {
                        x++;
                        if (x < numdirs)
                            dirnames[x] = "";
                    }
                    else
                        dirnames[x] += (char)temp;
                }
                x = 0;
                rawfilenames[0] = "";
                for (int i = 0; i < filenamesize; i++) {
                    temp = File.ReadByte();
                    if (temp == 0) {
                        x++;
                        if (x < numfiles)
                            rawfilenames[x] = "";
                    }
                    else
                        rawfilenames[x] += (char)temp;
                }
                numFiles = numfiles;
                fileoffsets = new int[numFiles];
                filesizes = new int[numFiles];
                filenames = new string[numFiles];
                for (int i = 0; i < numfiles; i++) {
                    fileoffsets[i] = fileData[i].offset;
                    filesizes[i] = fileData[i].size;
                    filenames[i] = (dirnames[fileData[i].dir] + "/" + rawfilenames[i]).Replace("/", "\\");
                }
                //foreach (String filename in rawfilenames)
                //    Console.WriteLine(filename);

                return true;
            }
            return false;
        }
        public String getFileExtensions() {
            return "gameaudio.dat;gamedata.dat";
        }
        public String getFileType() {
            return "Super Meat Boy Archive";
        }
        public Boolean addFileType() {
            return true;
        }
        public int getFileSize(int i) {
            return filesizes[i];
        }
        public String getFileName(int i) {
            return filenames[i].Replace("/", "\\");
        }
        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < numFiles; i++)
                yield return getFile(i);
        }

        public byte[] getFile(int i)
        {
            File.BaseStream.Seek(fileoffsets[i], SeekOrigin.Begin);
            return File.ReadBytes(filesizes[i]);
        }
        public void close() {
            if (this.File != null)
                this.File.Close();
        }
    }
}
