using System.IO;

namespace GameArchiver {
    public interface GameArchive {

        int numFiles { get; set; }

        string getFileName(int i);

        int getFileSize(int i);

        byte[] getFile(int p);

        string getFileType();

        string getFileExtensions();

        bool addFileType();

        bool LoadFile(FileStream fileStream);

        void close();
    }
}
