using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UniversalAnnotator
{
    using PortableCommon.Contract;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        FileManager fileManager;
        EntityCollection entityCollection = new EntityCollection();
        IndexedDocument currentDocument; 

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {

            // Do some setup specific to the example we are working on.

            txtStorageAccountConnectionString.Text = @"DefaultEndpointsProtocol=https;AccountName=luiscareco;AccountKey=REPLACEME;BlobEndpoint=https://luiscareco.blob.core.windows.net/;QueueEndpoint=https://luiscareco.queue.core.windows.net/;TableEndpoint=https://luiscareco.table.core.windows.net/;FileEndpoint=https://luiscareco.file.core.windows.net;";
            txtContainerName.Text = "enricherdemo";

            //TODO: Get entity types from a config file in blob storage.
            AddEntityType("Disease");
            AddEntityType("Learned Date");
            AddEntityType("Doctor Name");
            AddEntityType("Date");

            // TODO: Get file path from a config.
            fileManager = new FileManager(txtStorageAccountConnectionString.Text, txtContainerName.Text);

            fileManager.InitializeAnnotations();

            //TODO: Get annotations from a config file in blob storage.
            UpdateEntityList();
            UpdateFiles();

            // Initialize annotations on each of those files.
            
            UpdateDocumentView();
        }


        private async void UpdateFiles()
        {
            // Populate each of the controls.
            var blobItemList = await fileManager.GetFileList();

            foreach (var blobItem in blobItemList)
            {
                if (blobItem is CloudBlockBlob)
                {
                    //Add uri to the listview.
                    FileSelector.Items.Add(((CloudBlockBlob)blobItem).Name);
                }

                FileSelector.SelectedIndex = 0;
            }
        }

        private async void FileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (e.AddedItems.Count > 0)
            {
                string fileName = e.AddedItems[0].ToString();

                currentDocument = await fileManager.GetIndexedDocument(fileName);
                UpdateDocumentView();
            }
        }

        /// <summary>
        ///  Updates the rich textbox based on the state of the currrentDocument content and annotations.
        /// </summary>
        public void UpdateDocumentView()
        {
            DocumentView.Blocks.Clear();

            if (currentDocument == null) return; // nothing to do here.

            Paragraph p = new Paragraph();
            // TODO actually insert annotiations.

            int lastOffset = 0;

            // Do it for a single annotation first.
            if (currentDocument.Annotations != null)
            {
                foreach (var annotationTuple in currentDocument.Annotations)
                {
                    var annotation = annotationTuple.Value as Annotation;

                    // find the text between the last annotation and this annotation.
                    string preText = currentDocument.RawText.Substring(lastOffset, annotation.StartOffset - lastOffset);
                    p.Inlines.Add(new Run() { Text = preText });

                    // the anotation text.
                    string annotationText = currentDocument.RawText.Substring(annotation.StartOffset, annotation.EndOffset - annotation.StartOffset);

                    EntityType entityType;
                    entityCollection.TryGetValue(annotation.AnnotationName, out entityType);

                    Color highlightColor = Colors.Maroon;

                    if (entityType != null)
                    {
                        highlightColor = entityType.Color;
                    }

                    p.Inlines.Add(new Run() { Text = annotationText,
                                              Foreground =new SolidColorBrush(highlightColor),
                                              FontStyle = Windows.UI.Text.FontStyle.Italic});

                    lastOffset = annotation.EndOffset;
                }
            }
            // the final text all the way to the end of the document.
            string postText = currentDocument.RawText.Substring(lastOffset);
            p.Inlines.Add(new Run() { Text = postText });

            DocumentView.Blocks.Add(p);

        }

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private void DocumentView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            /*
            // Sender must be RichTextBox.
            RichTextBlock rtb = sender as RichTextBlock;
            if (rtb == null) return;

            var contextFlyout = rtb.ContextFlyout;

            // This uses HorizontalOffset and VerticalOffset properties to position the menu,
            // relative to the upper left corner of the parent element (RichTextBox in this case).
            //contextFlyout.Placement = FlyoutPlacementMode.Right;

            // Compute horizontal and vertical offsets to place the menu relative to selection end.
            
            OutputTextBlock.Text = "Selected " + rtb.SelectedText;
            
            var sb = new StringBuilder();

            foreach (var block in rtb.Blocks)
            {
                Paragraph paragraph = block as Paragraph;
                foreach (Run inline in paragraph.Inlines)
                {
                    sb.Append(inline.Text);
                }
            }

            String result = sb.ToString();


            */

            //OutputTextBlock.Text = "There are " + entityMenu.Items.Count + " items in entity menu.";

            // Finally, mark the event has handled.
            e.Handled = true;

        }


        private List<Inline> recurseClean(string paragraphString, string keyword)
        {

            List<Inline> result = new List<Inline>();

            if (paragraphString == null || paragraphString.Length == 0)
            {
                return result;
            }

            if (keyword == null || keyword.Length == 0)
            {
                result.Add(new Run() { Text = paragraphString });
                return result;
            }

            var keywordLocation = paragraphString.IndexOf(keyword);

            if (keywordLocation >= 0)
            {
                // the first section does not contain the keyword.
                string pre = paragraphString.Substring(0, keywordLocation);

                if (pre.Length >= 0)
                {
                    Run preRun = new Run();
                    preRun.Text = pre;
                    result.Add(preRun);
                }


                Run highlightRun = new Run();
                highlightRun.Text = keyword;
                highlightRun.Foreground = new SolidColorBrush(Colors.Red);
                result.Add(highlightRun);

                string post = paragraphString.Substring(keywordLocation + keyword.Length);

                if (post.Length >= 0)
                {
                    List<Inline> postRuns = recurseClean(post, keyword);

                    foreach (Run postRun in postRuns)
                    {
                        result.Add(postRun);
                    }
                }
            }
            else
            {
                // nothing to modify here.
                var run = new Run();
                run.Text = paragraphString;
                result.Add(run);
            }

            return result;
        }


        private void HighlightAllTermsInDocument(String keyword, SolidColorBrush brush)
        {
            var modifiedBlocks = new List<Block>();

            foreach (var block in DocumentView.Blocks)
            {
                Paragraph modifiedParagraph = new Paragraph();

                string paragraphString = "";


                Paragraph paragraph = block as Paragraph;

                foreach (Run inline in paragraph.Inlines)
                {
                    paragraphString += inline.Text;
                }

                foreach (Run run in recurseClean(paragraphString, keyword))
                {
                    modifiedParagraph.Inlines.Add(run);
                }

                modifiedBlocks.Add(modifiedParagraph);
            }

            DocumentView.Blocks.Clear();

            foreach (var block in modifiedBlocks)
            {
                DocumentView.Blocks.Add(block);
            }
        }


        private void AddAnnotation(EntityType entity)
        {
            // First remember the pointers so that we can use the Select method to extract different sections of the text.
            TextPointer selectionStart = DocumentView.SelectionStart;
            TextPointer selectionEnd = DocumentView.SelectionEnd;
            TextPointer documentStart = DocumentView.ContentStart;
            TextPointer documentEnd = DocumentView.ContentEnd;
            string selection = DocumentView.SelectedText;

            DocumentView.Select(documentStart, selectionStart);
            string textBeforeSelection = DocumentView.SelectedText;

            DocumentView.Select(documentStart, selectionStart);

            // Remove non-alpha numeric characters from the end of the string.
            string cleanSelection = CleanSelection(selection);

            var newAnnotation = new Annotation(textBeforeSelection.Length, textBeforeSelection.Length + cleanSelection.Length, currentDocument.FileName, entity.Name, cleanSelection);
            currentDocument.Annotations.Add(newAnnotation.StartOffset, newAnnotation);

            UpdateDocumentView();

            /*
            Run highlightRun = new Run();
            highlightRun.Text = DocumentView.SelectedText;
            highlightRun.Foreground = new SolidColorBrush(Colors.Red);*/

        }

        /// <summary>
        /// Removes alphanumeric characters from the end of the string.
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        private string CleanSelection(string selection)
        {
            if (selection.Length <= 1) return selection;

            int lastChar = selection.Length - 1;

            while (selection[lastChar]=='\n' || selection[lastChar] == '\r' || selection[lastChar] == ' ')
            {
                lastChar--;

                if (lastChar == 0)
                {
                    break;
                }
            }

            return selection.Substring(0, lastChar + 1);
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(DocumentView.SelectedText))
            {
                return;
            }

            // Get entity type from sender.
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            String entityName = item.Text;
            EntityType entityType;
            entityCollection.TryGetValue(entityName, out entityType);

            if (entityType != null)
            {
                AddAnnotation(entityType);
            }
            //HighlightAllTermsInDocument(DocumentView.SelectedText, new SolidColorBrush(Colors.Bisque));
        }

        char[] stopwords = new char[] {' ', ',', '.','\n','\r','\t',';'};

        /// <summary>
        /// If user simply right clicks on a word, this method will make sure to select the whole word under the
        /// pointer. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocumentView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // Get the location of the right tap.
            TextPointer pointer = DocumentView.GetPositionFromPoint(e.GetPosition(DocumentView));
            var endpointer = pointer.GetPositionAtOffset(10, LogicalDirection.Forward);
            DocumentView.Select(pointer, endpointer);

            var finalIndex = DocumentView.SelectedText.IndexOfAny(stopwords);

            string post = DocumentView.SelectedText;
            if (finalIndex > 0)
            {
                endpointer = pointer.GetPositionAtOffset(finalIndex, LogicalDirection.Forward);
            }

            // Now let's look backwards.
            int backspaces = 10;
            if (pointer.Offset < 10) { backspaces = pointer.Offset; }

            var prepointer = pointer.GetPositionAtOffset(-backspaces, LogicalDirection.Backward);
            
            DocumentView.Select(prepointer, pointer);

            // Clean stopwords/characters from the left.
            // I need to do this iteratively because I have no way to skip formatting characters. UWP still has some way to go.
            var firstIndex = DocumentView.SelectedText.LastIndexOfAny(stopwords);
            while (firstIndex >= 0)
            {
                prepointer = prepointer.GetPositionAtOffset(1, LogicalDirection.Forward);
                DocumentView.Select(prepointer, endpointer);
                firstIndex = DocumentView.SelectedText.LastIndexOfAny(stopwords);
            }

            String selection = DocumentView.SelectedText;
        }

        Random r = new Random();

        private void AddEntityButton_Click(object sender, RoutedEventArgs e)
        {
            AddEntityType(txtEntityName.Text);

            // Clear the text for the next insertion.
            txtEntityName.Text = "";

            AnnotationsFlyout.Hide();
            UpdateEntityList();

        }

        private void AddEntityType(String entityName)
        {
            Color randomColor = Color.FromArgb((byte)255, (byte)r.Next(50, 200), (byte)r.Next(50, 200), (byte)r.Next(50, 200));
            var entityType = new EntityType(entityName, randomColor);
            entityCollection.AddEntity(entityType);
        }

        private Panel CreateEntityVisual(EntityType entity)
        {
            var margin = new Thickness(10);

            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = margin;

            Rectangle rect = new Rectangle() { Width = 60, Height = 40, Fill = new SolidColorBrush(entity.Color) };
            var textBlock = new TextBlock() { Text = entity.Name, Margin = margin, VerticalAlignment = VerticalAlignment.Center };

            sp.Children.Add(rect);
            sp.Children.Add(textBlock);

            return sp;
        }

        private void UpdateEntityList()
        {
            entityMenu.Items.Clear();
            comboDeleteEntities.Items.Clear();
            entitiesList.Items.Clear();

            foreach (EntityType entity in entityCollection.Values)
            {
                // TODO: There must be a better way to do this -- bind the data to the visual... figure it out later.
                MenuFlyoutItem item = new MenuFlyoutItem();
                item.Text = entity.Name;
                item.Click += MenuFlyoutItem_Click;
                entityMenu.Items.Add(item);
                
                ComboBoxItem comboItem = new ComboBoxItem();
                comboItem.Content = entity.Name;
                comboDeleteEntities.Items.Add(comboItem);

                entitiesList.Items.Add(CreateEntityVisual(entity));

            }
        }

        private void DeleteEntity_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxItem item = comboDeleteEntities.SelectedItem as ComboBoxItem;
            if (item != null && (item.Content as String) != null)
            {
                entityCollection.Remove(item.Content as string);
            }

            UpdateEntityList();
        }

        private async void Train_Click(object sender, RoutedEventArgs e)
        {
            // First let's save all annotations in JSON format.

            //Let's save annotations for all files.

            string trainingData = await fileManager.SaveAnnotations();

            string programId = await SendTrainRequest(trainingData);

            if (programId == null)
            {
                // Setting this just for now so we can test the rest of the code.
                programId = "cfe46b80-278c-4124-bcdb-2d93da5296a0";
            }
            string newAnnotations = await SendExtractRequest(programId);

            if (newAnnotations != null)
            {
               int records = await fileManager.AddLearnedAnnotations(newAnnotations);
            }

            UpdateDocumentView();
        }

        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// If successful, should return the guid for the model which can be used on an Extract Call
        /// </summary>
        /// <param name="trainingData"></param>
        /// <returns></returns>
        private async Task<string> SendTrainRequest(string trainingData)
        {
            var stringContent = new StringContent(trainingData);

            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync("http://localhost:2711/api/extraction/Train?blobContainerName=enricherdemo&trainDirectory=files", stringContent);
            var responseString = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(responseString);

            // TODO: Fix server so it sends actual HTTP responses messages
            if (response.IsSuccessStatusCode)
            {
                return responseString;
            }
            else 
            {
                // Something bad happened. :-(
                Debug.WriteLine(responseString);
                return null;
            }
        }



        /// <summary>
        /// If successful, should return the guid for the model which can be used on an Extract Call
        /// </summary>
        /// <param name="trainingData"></param>
        /// <returns></returns>
        private async Task<string> SendExtractRequest(string programId)
        {
            char[] invalidChar = { '\"'};
            string program = programId.Trim(invalidChar);

            var response = await client.GetAsync("http://localhost:2711/api/extraction/Extract?blobContainerName=enricherdemo&scoreDirectory=files&programId="+ program);
            var responseString = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(responseString);

            // TODO: Fix server so it sends actual HTTP responses messages
            if (response.IsSuccessStatusCode)
            {
                // TODO: Deserialize annotations as strings.
                return responseString;
            }
            else
            {
                // Something bad happened. :-(
                Debug.WriteLine(responseString);
                return null;
            }
        }

    }
}
