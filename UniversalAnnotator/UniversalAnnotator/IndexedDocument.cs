using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalAnnotator
{
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

    public class Annotation
    {
        public Annotation(int startOffset, int endOffset, string annotationName, string expectedContent = null)
        {
            this.StartOffset = startOffset;
            this.EndOffset = endOffset;
            this.AnnotationName = annotationName;
        }

        public int StartOffset;
        public int EndOffset;
        public string AnnotationName;

        // this one is here for debugging purposes -- to make sure we are highlighting the right content.
        public string expectedContent;
    }
}
