using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalAnnotator
{
    using PortableCommon.Contract;

    public class IndexedDocument
    {
        // the entity that knows how to read the file.
        private FileManager fileManager;
        
        // Unique identifier for the file -- really a Uri
        private string  fileName;

        private IndexedDocument()
        {
            Annotations = new SortedList<int, Annotation>();
        }

        public static async Task<IndexedDocument> Create(FileManager fileManager, String name)
        {
            IndexedDocument thisDoc = new IndexedDocument();

            thisDoc.fileManager = fileManager;
            thisDoc.fileName = name;
            thisDoc.RawText = await fileManager.GetFileContent(name);

            return thisDoc;
        }

        public SortedList<int, Annotation> Annotations
        { get; set;}


        public string RawText { get; set; }
    }
}
