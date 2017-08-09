namespace PortableCommon.Contract
{
    using System;

    public class Annotation
    {
        public Annotation() { }

        public Annotation(int startOffset, int endOffset, string fileName, string annotationName, string expectedContent = null)
        {
            this.StartOffset = startOffset;
            this.EndOffset = endOffset;
            this.FileName = fileName;
            this.AnnotationName = annotationName;
        }

        public int StartOffset { set; get; }
        public int EndOffset { set; get; }
        public string AnnotationName { set; get; }
        public string FileName { set; get; }

        // this one is here for debugging purposes -- to make sure we are highlighting the right content.
        public string expectedContent { set; get; }
    }
}
