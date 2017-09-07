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
        // Unique identifier for the file -- really a Uri
        public string FileName;

        public string RawText { get; set; }

        public IndexedDocument(String fileName, String rawText)
        {
            FileName = fileName;
            RawText = rawText; // await fileManager.GetFileContent(name);
            Annotations = new SortedList<int, Annotation>();
        }

        public SortedList<int, Annotation> Annotations
        { get; set;}
    }
}
