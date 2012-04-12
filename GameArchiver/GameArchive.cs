using System.IO;

namespace Parch {
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
