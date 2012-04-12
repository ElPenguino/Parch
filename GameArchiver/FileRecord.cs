using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parch {
    public class FileRecord {

        public FileRecord(int ID, string Name, int Size) {
            this.ID = ID;
            this.Name = Name;
            this.Size = Size;
        }
        public int ID { get; set; }

        public string Name { get; set; }

        public int Size { get; set; }
    }
}
