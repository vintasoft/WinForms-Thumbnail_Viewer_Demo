using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Codecs.Encoders;
using Vintasoft.Imaging.UI;

using DemosCommonCode;
using DemosCommonCode.Imaging.Codecs;
using DemosCommonCode.Imaging;

namespace ThumbnailViewerDemo
{
    /// <summary>
    /// Main form of Thumbnail Viewer Demo.
    /// </summary>
    public partial class MainForm : Form
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            // register the evaluation license for VintaSoft Imaging .NET SDK
            Vintasoft.Imaging.ImagingGlobalSettings.Register("REG_USER", "REG_EMAIL", "EXPIRATION_DATE", "REG_CODE");

            InitializeComponent();

            Jbig2AssemblyLoader.Load();
            Jpeg2000AssemblyLoader.Load();
            RawAssemblyLoader.Load();
            DicomAssemblyLoader.Load();
            DocxAssemblyLoader.Load();

            Text = string.Format("VintaSoft Thumbnail Viewer Demo v{0}", ImagingGlobalSettings.ProductVersion);

            containerThumbnailViewer.Images.ImageCollectionSavingProgress += new EventHandler<ProgressEventArgs>(ContainerImages_ImageCollectionSavingProgress);
            containerThumbnailViewer.Images.ImageSavingException += new EventHandler<ExceptionEventArgs>(ContainerImages_ImageSavingException);
            containerThumbnailViewer.Images.ImageCollectionChanged += new EventHandler<ImageCollectionChangeEventArgs>(ContainerImages_ImageCollectionChanged);

            folderViewer.ThumbnailMultipageTextFormat = "{0}\n({1} pages)";

            folderFlowStyleToolStripComboBox.Items.Add(ThumbnailFlowStyle.SingleRow);
            folderFlowStyleToolStripComboBox.Items.Add(ThumbnailFlowStyle.WrappedRows);
            folderFlowStyleToolStripComboBox.SelectedIndex = 1;

            fileFlowStyleToolStripComboBox.Items.Add(ThumbnailFlowStyle.SingleColumn);
            fileFlowStyleToolStripComboBox.Items.Add(ThumbnailFlowStyle.WrappedRows);
            fileFlowStyleToolStripComboBox.SelectedIndex = 1;

            folderViewer.ThumbnailControlCreating += folderViewer_ThumbnailControlCreating;
            folderViewer.ThumbnailControlDisposing += folderViewer_ThumbnailControlDisposing;
            folderViewer.SelectedIndices.Changed += folderViewer_SelectedIndices_Changed;

            folderViewer.DefaultFileIcon = DemosResourcesManager.GetResourceAsImage("iconDefault.png");
            folderViewer.ErrorFileIcon = DemosResourcesManager.GetResourceAsImage("iconError.png");

            VintasoftImage icon;

            icon = DemosResourcesManager.GetResourceAsImage("iconBMP.png");
            folderViewer.FileTypeIcons.Add(".BMP", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconPNG.png");
            folderViewer.FileTypeIcons.Add(".PNG", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconGIF.png");
            folderViewer.FileTypeIcons.Add(".GIF", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconPDF.png");
            folderViewer.FileTypeIcons.Add(".PDF", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconTIFF.png");
            folderViewer.FileTypeIcons.Add(".TIF", icon);
            folderViewer.FileTypeIcons.Add(".TIFF", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconJPEG.png");
            folderViewer.FileTypeIcons.Add(".JPEG", icon);
            folderViewer.FileTypeIcons.Add(".JPG", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconJPEG2000.png");
            folderViewer.FileTypeIcons.Add(".JP2", icon);
            folderViewer.FileTypeIcons.Add(".J2K", icon);
            folderViewer.FileTypeIcons.Add(".J2C", icon);
            folderViewer.FileTypeIcons.Add(".JPC", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconJBIG2.png");
            folderViewer.FileTypeIcons.Add(".JB2", icon);
            folderViewer.FileTypeIcons.Add(".JBIG2", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconEMF.png");
            folderViewer.FileTypeIcons.Add(".EMF", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconWMF.png");
            folderViewer.FileTypeIcons.Add(".WMF", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconICO.png");
            folderViewer.FileTypeIcons.Add(".ICO", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconPCX.png");
            folderViewer.FileTypeIcons.Add(".PCX", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconDICOM.png");
            folderViewer.FileTypeIcons.Add(".DCM", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconJPEG-LS.png");
            folderViewer.FileTypeIcons.Add(".JLS", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconRAW.png");
            folderViewer.FileTypeIcons.Add(".CR2", icon);
            folderViewer.FileTypeIcons.Add(".CRW", icon);
            folderViewer.FileTypeIcons.Add(".NEF", icon);
            folderViewer.FileTypeIcons.Add(".NRW", icon);
            folderViewer.FileTypeIcons.Add(".DNG", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconDOCX.png");
            folderViewer.FileTypeIcons.Add(".DOCX", icon);

            icon = DemosResourcesManager.GetResourceAsImage("iconXLSX.png");
            folderViewer.FileTypeIcons.Add(".XLSX", icon);

            // set CustomFontProgramsController for all opened documents
            CustomFontProgramsController.SetDefaultFontProgramsController();

            DocumentPasswordForm.EnableAuthentication(fileThumbnailViewer);

            DemosTools.SetTestFilesFolder(folderBrowserDialog1);
        }

        #endregion



        #region Properties

        /// <summary>
        /// Gets or sets the selected folder.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedFolder
        {
            get
            {
                return folderViewer.SelectedFolder;
            }
            set
            {
                folderGroupBox.Text = value;
                folderViewer.SelectedFolder = value;
            }
        }

        #endregion



        #region Methods

        #region UI

        #region 'File' menu

        /// <summary>
        /// Handles the Click event of openFolderToolStripMenuItem object.
        /// </summary>
        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                SelectedFolder = folderBrowserDialog1.SelectedPath;
        }

        /// <summary>
        /// Handles the Click event of exitToolStripMenuItem object.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion


        #region 'Container' menu

        /// <summary>
        /// Handles the Click event of clearToolStripMenuItem object.
        /// </summary>
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            containerThumbnailViewer.Images.ClearAndDisposeItems();
            if (viewersTabControl.SelectedTab != containerTabPage)
                viewersTabControl.SelectedTab = containerTabPage;
        }

        /// <summary>
        /// Handles the Click event of saveAsToolStripMenuItem object.
        /// </summary>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (containerThumbnailViewer.Images.Count > 0)
            {
                // set filters for saving dialog
                CodecsFileFilters.SetSaveFileDialogFilter(saveImageFileDialog, containerThumbnailViewer.Images.Count > 1, false);
                saveImageFileDialog.InitialDirectory = SelectedFolder;
                if (saveImageFileDialog.ShowDialog() == DialogResult.OK)
                {
                    EncoderBase encoder = null;
                    // get encoder
                    if (PluginsEncoderFactory.Default.GetEncoder(saveImageFileDialog.FileName, out encoder))
                    {
                        if (encoder == null)
                        {
                            MessageBox.Show(string.Format("Encoder for file '{0}' was not found.", Path.GetFileName(saveImageFileDialog.FileName)), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        try
                        {
                            if (encoder is MultipageEncoderBase)
                            {
                                // save images
                                containerThumbnailViewer.Images.SaveAsync(saveImageFileDialog.FileName, (MultipageEncoderBase)encoder);
                            }
                            else
                            {
                                Thread saveThread = new Thread(SaveSingleImageFromContainer);
                                saveThread.IsBackground = true;
                                saveThread.Start(encoder);
                            }
                        }
                        catch (Exception ex)
                        {
                            DemosTools.ShowErrorMessage(ex, saveImageFileDialog.FileName);
                        }
                    }
                }
            }
        }

        #endregion


        #region 'View' menu

        /// <summary>
        /// Handles the SelectedIndexChanged event of folderFlowStyleToolStripComboBox object.
        /// </summary>
        private void folderFlowStyleToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            folderViewer.ThumbnailFlowStyle = (ThumbnailFlowStyle)folderFlowStyleToolStripComboBox.SelectedItem;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of fileFlowStyleToolStripComboBox object.
        /// </summary>
        private void fileFlowStyleToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            fileThumbnailViewer.ThumbnailFlowStyle = (ThumbnailFlowStyle)fileFlowStyleToolStripComboBox.SelectedItem;
        }

        /// <summary>
        /// Handles the CheckStateChanged event of selectionCheckBoxToolStripMenuItem object.
        /// </summary>
        private void selectionCheckBoxToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            folderViewer.ShowThumbnailCheckBox = selectionCheckBoxToolStripMenuItem.Checked;
        }

        #endregion


        #region 'About' menu

        /// <summary>
        /// Handles the Click event of aboutToolStripMenuItem object.
        /// </summary>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutBoxForm aboutDlg = new AboutBoxForm())
            {
                aboutDlg.ShowDialog();
            }
        }

        #endregion


        #region Drag&Drop

        #region to FolderViewer

        /// <summary>
        /// Handles the DragEnter event of folderViewer object.
        /// </summary>
        private void folderViewer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileNameW"))
            {
                // allow copy
                e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Handles the DragDrop event of folderViewer object.
        /// </summary>
        private void folderViewer_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileNameW"))
            {
                string[] filenames = (string[])e.Data.GetData("FileNameW");
                CopyFiles(filenames, folderViewer.SelectedFolder);
            }
        }

        #endregion


        #region to FileThumbnailViewer

        /// <summary>
        /// Handles the DragEnter event of fileThumbnailViewer object.
        /// </summary>
        private void fileThumbnailViewer_DragEnter(object sender, DragEventArgs e)
        {
            string[] formats = e.Data.GetFormats();
            for (int i = 0; i < formats.Length; i++)
                if (e.Data.GetData(formats[i]) != fileThumbnailViewer)
                {
                    // disable Drag&Drop
                    fileThumbnailViewer.AllowDrop = false;
                    break;
                }
        }

        /// <summary>
        /// Handles the DragLeave event of fileThumbnailViewer object.
        /// </summary>
        private void fileThumbnailViewer_DragLeave(object sender, EventArgs e)
        {
            fileThumbnailViewer.AllowDrop = true;
        }

        #endregion


        #region to ContainerThumbnailViewer

        /// <summary>
        /// Handles the DragEnter event of containerThumbnailViewer object.
        /// </summary>
        private void containerThumbnailViewer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileNameW"))
            {
                // allow copy
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(typeof(FolderThumbnailViewer)))
            {
                // add image file from FolderThumbnailViewer
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(typeof(ThumbnailViewer)))
            {
                // Drag&Drop from fileThumbnailViewer
                if (e.Data.GetData(typeof(ThumbnailViewer)) == fileThumbnailViewer)
                {
                    // add image file from fileThumbnailViewer
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        /// <summary>
        /// Handles the DragDrop event of containerThumbnailViewer object.
        /// </summary>
        private void containerThumbnailViewer_DragDrop(object sender, DragEventArgs e)
        {
            // Screen -> Control
            Point viewerSpacePoint = containerThumbnailViewer.PointToClient(new Point(e.X, e.Y));
            // Control -> Thumbanil index
            int insertIndex = containerThumbnailViewer.DropIndexFromPoint(viewerSpacePoint);

            string[] filenames = null;
            if (e.Data.GetDataPresent("FileNameW"))
            {
                // Drag&Drop from OS
                filenames = (string[])e.Data.GetData("FileNameW");
            }
            else if (e.Data.GetDataPresent(typeof(FolderThumbnailViewer)))
            {
                // Drag&Drop from FolderThumbnailViewer
                e.Effect = DragDropEffects.None;
                filenames = new string[folderViewer.SelectedIndices.Count];
                for (int i = 0; i < folderViewer.SelectedIndices.Count; i++)
                {
                    VintasoftImage fileThumbnail = folderViewer.Images[folderViewer.SelectedIndices[i]];
                    filenames[i] = ((FolderThumbnailViewer.FileIconTagData)fileThumbnail.Tag).Filename;
                }
            }
            else if (e.Data.GetDataPresent(typeof(ThumbnailViewer)))
            {
                // Drag&Drop from fileThumbnailViewer
                if (e.Data.GetData(typeof(ThumbnailViewer)) == fileThumbnailViewer)
                {
                    e.Effect = DragDropEffects.None;
                    VintasoftImage[] imagesToInsert = new VintasoftImage[fileThumbnailViewer.SelectedIndices.Count];
                    for (int i = 0; i < fileThumbnailViewer.SelectedIndices.Count; i++)
                    {
                        int index = fileThumbnailViewer.SelectedIndices[i];
                        imagesToInsert[i] = (VintasoftImage)fileThumbnailViewer.Images[index].Clone();
                    }
                    // insert images
                    containerThumbnailViewer.Images.InsertRange(insertIndex, imagesToInsert);
                }
            }

            if (filenames != null)
            {
                ImageCollection images = new ImageCollection();
                for (int i = 0; i < filenames.Length; i++)
                {
                    try
                    {
                        images.Add(filenames[i]);
                    }
                    catch (Exception ex)
                    {
                        DemosTools.ShowErrorMessage(ex, filenames[i]);
                    }
                }
                // insert images
                containerThumbnailViewer.Images.InsertRange(insertIndex, images.ToArray());
            }
        }

        #endregion

        #endregion


        #region Folder Viewer

        /// <summary>
        /// Handles the DeleteKeyPressed event of folderViewer object.
        /// </summary>
        private void folderViewer_DeleteKeyPressed(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (folderViewer.SelectedIndices.Count > 0)
            {
                string message;
                if (folderViewer.SelectedIndices.Count > 1)
                {
                    message = string.Format("Delete selected files({0}) form disk?", folderViewer.SelectedIndices.Count);
                }
                else
                {
                    string filename = ((FolderThumbnailViewer.FileIconTagData)folderViewer.Images[folderViewer.SelectedIndices[0]].Tag).Filename;
                    message = string.Format("Delete file '{0}' from disk?", Path.GetFileName(filename));
                }
                if (MessageBox.Show(message, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    HashSet<string> filesToRemove = new HashSet<string>();
                    foreach (int index in folderViewer.SelectedIndices.ToArray())
                    {
                        if (index == folderViewer.FocusedIndex)
                            folderViewer.FocusedIndex = -1;
                        string filename = ((FolderThumbnailViewer.FileIconTagData)folderViewer.Images[index].Tag).Filename;
                        filesToRemove.Add(filename);                        
                    }
                    foreach(string filename in filesToRemove)
                    {
                        try
                        {
                            File.Delete(filename);
                        }
                        catch (Exception ex)
                        {
                            DemosTools.ShowErrorMessage(ex, filename);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the FocusedIndexChanged event of folderViewer object.
        /// </summary>
        private void folderViewer_FocusedIndexChanged(object sender, FocusedIndexChangedEventArgs e)
        {
            OpenFocusedFile();
        }

        /// <summary>
        /// Handles the SelectedIndices_Changed event of folderViewer object.
        /// </summary>
        private void folderViewer_SelectedIndices_Changed(object sender, EventArgs e)
        {
            // if thumbnail viewer shows thumbnail checkboxes
            if (folderViewer.ShowThumbnailControl)
            {
                // update states of thumbnail checkboxes

                if (InvokeRequired)
                    Invoke(new UpdateCheckBoxesStateDelegate(UpdateStatesOfThumbnailCheckboxes));
                else
                    UpdateStatesOfThumbnailCheckboxes();
            }
        }

        /// <summary>
        /// Updates states of thumbnail checkboxes.
        /// </summary>
        private void UpdateStatesOfThumbnailCheckboxes()
        {
            // get all thumbnail checkboxes
            IEnumerable<KeyValuePair<VintasoftImage, Control>> imageToCheckboxes = folderViewer.GetThumbnailControls();
            // for each checkbox
            foreach (KeyValuePair<VintasoftImage, Control> imageToControl in imageToCheckboxes)
            {
                // get image index
                int imageIndex = folderViewer.Images.IndexOf(imageToControl.Key);
                // if checkbox exists
                if (imageToControl.Value != null)
                {
                    // check/uncheck the checkbox
                    ((CheckBox)imageToControl.Value).Checked = folderViewer.SelectedIndices.Contains(imageIndex);
                }
            }
        }

        /// <summary>
        /// Handles the ThumbnailControlCreating event of folderViewer object.
        /// </summary>
        private void folderViewer_ThumbnailControlCreating(object sender, ThumbnailControlEventArgs e)
        {
            // get checkbox, which is created by ThumbnailViewer
            CheckBox thumbanilCheckbox = (CheckBox)e.ThumbnailControl;
            // save information about thumbnail in checkbox
            thumbanilCheckbox.Tag = e.Image.Thumbnail;

            // subscribe to the events of thumbnail checkbox
            thumbanilCheckbox.CheckedChanged += new EventHandler(thumbnailCheckBox_CheckedChanged);
        }

        /// <summary>
        /// Handles the ThumbnailControlDisposing event of folderViewer object.
        /// </summary>
        private void folderViewer_ThumbnailControlDisposing(object sender, ThumbnailControlEventArgs e)
        {
            // unsubscribe from events of thumbnail checkbox
            ((CheckBox)e.ThumbnailControl).CheckedChanged -= new EventHandler(thumbnailCheckBox_CheckedChanged);
        }

        /// <summary>
        /// Handles the CheckedChanged event of thumbnailCheckBox object.
        /// </summary>
        private void thumbnailCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // get thumbnail checkbox
            CheckBox checkBox = (CheckBox)sender;
            // get thumbnail
            Thumbnail thumbnail = (Thumbnail)checkBox.Tag;
            // get index of image that is associated with thumbnail
            int imageIndex = folderViewer.Images.IndexOf(thumbnail.Source);
            // if image is found
            if (imageIndex >= 0)
            {
                // if checkbox is checked
                if (checkBox.Checked)
                {
                    // if thumbnail is not selected in viewer
                    if (!folderViewer.SelectedIndices.Contains(imageIndex))
                        // select thumbnail in viewer
                        folderViewer.SelectedIndices.Add(imageIndex);
                }
                // if checkbox is unchecked
                else
                {
                    // if thumbnail is selected in viewer
                    if (folderViewer.SelectedIndices.Contains(imageIndex))
                        // unselect thumbnail in viewer
                        folderViewer.SelectedIndices.Remove(imageIndex);
                }

                // set focus to the viewer
                folderViewer.Focus();
            }
        }

        #endregion


        #region Container Thumbnail Viewer

        /// <summary>
        /// Handles the DeleteKeyPressed event of containerThumbnailViewer object.
        /// </summary>
        private void containerThumbnailViewer_DeleteKeyPressed(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            while (containerThumbnailViewer.SelectedIndices.Count > 0)
            {
                int index = containerThumbnailViewer.SelectedIndices[0];
                containerThumbnailViewer.Images.RemoveAt(index);
            }
        }

        #endregion


        #region Page Image Viewer

        /// <summary>
        /// Handles the ImageLoadingProgress event of pageImageViewer object.
        /// </summary>
        private void pageImageViewer_ImageLoadingProgress(object sender, ProgressEventArgs e)
        {
            if (e.Progress != 100)
                imageProgressBar.Value = e.Progress;
            else
                imageProgressBar.Value = 0;
        }

        /// <summary>
        /// Handles the ValueChanged event of imageZoomTrackBar object.
        /// </summary>
        private void imageZoomTrackBar_ValueChanged(object sender, EventArgs e)
        {
            pageImageViewer.Zoom = imageZoomTrackBar.Value;
        }

        #endregion


        #region Main form

        /// <summary>
        /// Handles the Shown event of MainForm object.
        /// </summary>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.SelectedPath != "")
                SelectedFolder = folderBrowserDialog1.SelectedPath;
            else
                SelectedFolder = Environment.CurrentDirectory;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of viewersTabControl object.
        /// </summary>
        private void viewersTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (viewersTabControl.SelectedTab == imageTabPage)
                pageImageViewer.MasterViewer = fileThumbnailViewer;
            else
                pageImageViewer.MasterViewer = null;
        }

        #endregion


        /// <summary>
        /// Handles the ThumbnailsLoadingProgress event of fileThumbnailViewer object.
        /// </summary>
        private void fileThumbnailViewer_ThumbnailsLoadingProgress(object sender, ThumbnailsLoadingProgressEventArgs e)
        {
            fileProgressBar.Value = e.Progress;
            if (fileProgressBar.Value == 100)
                fileProgressBar.Value = 0;
        }

        #endregion


        #region File Thumbnail Viewer

        /// <summary>
        /// Closes current file.
        /// </summary>
        private void CloseCurrentFile()
        {
            fileThumbnailViewer.Images.ClearAndDisposeItems();
            fileGroupBox.Text = "File";
        }

        /// <summary>
        /// Opens the focused file.
        /// </summary>
        private void OpenFocusedFile()
        {
            CloseCurrentFile();
            if (folderViewer.FocusedIndex >= 0)
            {
                string filename = ((FolderThumbnailViewer.FileIconTagData)folderViewer.Images[folderViewer.FocusedIndex].Tag).Filename;
                try
                {
                    fileGroupBox.Text = Path.GetFileName(filename);
                    fileThumbnailViewer.Images.Add(filename);
                }
                catch (IOException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseCurrentFile();
                }
            }
        }

        #endregion


        /// <summary>
        /// Handler of the image collection changed event.
        /// </summary>
        private void ContainerImages_ImageCollectionChanged(object sender, ImageCollectionChangeEventArgs e)
        {
            string text = "Container ({0})";
            if (containerThumbnailViewer.Images.Count > 0)
                containerTabPage.Text = string.Format(text, containerThumbnailViewer.Images.Count + " images");
            else
                containerTabPage.Text = string.Format(text, "use Drag&&Drop");
        }

        /// <summary>
        /// Saves the single image from container.
        /// </summary>
        /// <param name="encoder">Encoder to save the image.</param>
        private void SaveSingleImageFromContainer(object encoder)
        {
            try
            {
                containerThumbnailViewer.Images[0].Save(saveImageFileDialog.FileName, (EncoderBase)encoder, encoder_ImageSavingProgress);
            }
            catch (Exception ex)
            {
                DemosTools.ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// Handler of the image saving progress event.
        /// </summary>
        private void encoder_ImageSavingProgress(object sender, ProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ProgressEventArgs>(encoder_ImageSavingProgress), sender, e);
            }
            else
            {
                if (e.Progress == 100)
                    containerProgressBar.Value = 0;
                else
                    containerProgressBar.Value = e.Progress;
            }
        }

        /// <summary>
        /// Handler of the image collection saving progress event.
        /// </summary>
        private void ContainerImages_ImageCollectionSavingProgress(object sender, ProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ProgressEventArgs>(ContainerImages_ImageCollectionSavingProgress), sender, e);
            }
            else
            {
                if (e.Progress == 100)
                    containerProgressBar.Value = 0;
                else
                    containerProgressBar.Value = e.Progress;
            }
        }

        /// <summary>
        /// Handler of the image saving exception event.
        /// </summary>
        private void ContainerImages_ImageSavingException(object sender, ExceptionEventArgs e)
        {
            DemosTools.ShowErrorMessage(e.Exception);
        }

        /// <summary>
        /// Copies the files to the destination folder.
        /// </summary>
        /// <param name="filenames">The filenames.</param>
        /// <param name="destFolder">The destination folder.</param>
        private static void CopyFiles(string[] filenames, string destFolder)
        {
            for (int i = 0; i < filenames.Length; i++)
            {
                try
                {
                    string sourceFilename = filenames[i];
                    string destFilename = Path.Combine(destFolder, Path.GetFileName(sourceFilename));
                    // if destination file is not the source file
                    if (sourceFilename.ToLowerInvariant() != destFilename.ToLowerInvariant())
                    {
                        bool needCopy = true;
                        // if the file with this name is already exists
                        if (File.Exists(destFilename))
                        {
                            string message = string.Format("File {0} already exists, replace it?", Path.GetFileName(destFilename));
                            if (MessageBox.Show(message, "Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                                needCopy = false;
                        }
                        if (needCopy)
                        {
                            // copy file
                            File.Copy(sourceFilename, destFilename, true);
                        }
                    }

                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
            }
        }

        #endregion



        #region Delegates

        private delegate void UpdateCheckBoxesStateDelegate();

        #endregion

    }
}
