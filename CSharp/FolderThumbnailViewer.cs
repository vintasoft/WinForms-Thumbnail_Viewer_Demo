using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Codecs.Decoders;
using Vintasoft.Imaging.ImageProcessing;
using Vintasoft.Imaging.UI;

using DemosCommonCode.Imaging;

namespace ThumbnailViewerDemo
{
    /// <summary> 
    /// Provides a folder thumbnail viewer control.
    /// </summary>
    public class FolderThumbnailViewer : ThumbnailViewer
    {

        #region Nested classes

        public class FileIconTagData
        {

            #region Constructors

            public FileIconTagData(string filename)
            {
                _filename = filename;
            }

            #endregion



            #region Properties

            string _filename;
            /// <summary>
            /// Gets or sets a filename.
            /// </summary>
            public string Filename
            {
                get
                {
                    return _filename;
                }
                set
                {
                    _filename = value;
                }
            }

            string _decoderName;
            public string DecoderName
            {
                get
                {
                    return _decoderName;
                }
            }

            int _pageCount = -1;
            public int PageCount
            {
                get
                {
                    return _pageCount;
                }
            }

            public bool IsLoaded
            {
                get
                {
                    return _pageCount >= 0;
                }
            }

            public bool IsLoadingError
            {
                get
                {
                    return _pageCount == 0;
                }
            }

            #endregion



            #region Methods

            internal void Reload()
            {
                _pageCount = -1;
            }

            internal void OnIconLoaded(string decoderName, int pagesCount)
            {
                _decoderName = decoderName;
                _pageCount = pagesCount;
            }

            #endregion

        }

        #endregion



        #region Fields

        Thread _thumbnailCreationThread = null;
        Stack<int> _thumbnailLoadIndexes = new Stack<int>();
        FileSystemWatcher _fileSystemWatcher = new FileSystemWatcher();

        #endregion



        #region Constructors

        public FolderThumbnailViewer()
        {
            ThumbnailSize = new Size(100, 100 + _textSpaceHeight);
            _textPadding = new Padding();
            _textPadding.Top = 3;

            _fileSystemWatcher = new FileSystemWatcher();
            _fileSystemWatcher.Filter = "*";
            _fileSystemWatcher.IncludeSubdirectories = false;
            _fileSystemWatcher.Changed += new FileSystemEventHandler(fileSystemWatcher_SelectedFolderChanged);
            _fileSystemWatcher.Created += new FileSystemEventHandler(fileSystemWatcher_SelectedFolderChanged);
            _fileSystemWatcher.Deleted += new FileSystemEventHandler(fileSystemWatcher_SelectedFolderChanged);
            _fileSystemWatcher.Renamed += new RenamedEventHandler(fileSystemWatcher_SelectedFolderRenamed);

            // embedded decoders
            _imageFileExtensions.AddRange(new string[]{
                ".BMP",
                ".JPG",
                ".JPEG",
                ".PNG",
                ".TIF",
                ".TIFF",
                ".GIF",
                ".WMF",
                ".EMF",
                ".ICO",
                ".PCX",
                ".JLS"
            });
            // PDF
            if (AvailableDecoders.IsDecoderAvailable("Pdf"))
                _imageFileExtensions.Add(".PDF");
            // JBIG2
            if (AvailableDecoders.IsDecoderAvailable("Jbig2"))
            {
                _imageFileExtensions.Add(".JB2");
                _imageFileExtensions.Add(".JBIG2");
            }
            // JPEG 2000
            if (AvailableDecoders.IsDecoderAvailable("Jpeg2000"))
            {
                _imageFileExtensions.Add(".JP2");
                _imageFileExtensions.Add(".J2K");
                _imageFileExtensions.Add(".J2C");
                _imageFileExtensions.Add(".JPC");
            }
            // RAW
            if (AvailableDecoders.IsDecoderAvailable("Raw"))
            {
                _imageFileExtensions.Add(".NEF");
                _imageFileExtensions.Add(".NRW");
                _imageFileExtensions.Add(".CR2");
                _imageFileExtensions.Add(".CRW");
                _imageFileExtensions.Add(".DNG");
            }
            // DICOM
            if (AvailableDecoders.IsDecoderAvailable("Dicom"))
            {
                _imageFileExtensions.Add(".DCM");
            }
            // DOCX
            if (AvailableDecoders.IsDecoderAvailable("Docx"))
            {
                _imageFileExtensions.Add(".DOCX");
            }
            // XLSX
            if (AvailableDecoders.IsDecoderAvailable("Xlsx"))
            {
                _imageFileExtensions.Add(".XLSX");
            }
        }

        #endregion



        #region Properties

        int _textSpaceHeight = 25;
        [DefaultValue(25)]
        public int TextSpaceHeight
        {
            get
            {
                return _textSpaceHeight;
            }
            set
            {
                _textSpaceHeight = value;
                Invalidate();
            }
        }

        Padding _textPadding;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Padding TextPadding
        {
            get
            {
                return _textPadding;
            }
            set
            {
                _textPadding = value;
                Invalidate();
            }
        }

        string _thumbnailTextFormat = "{0}";
        [DefaultValue("{0}")]
        public string ThumbnailTextFormat
        {
            get
            {
                return _thumbnailTextFormat;
            }
            set
            {
                _thumbnailTextFormat = value;
            }
        }

        string _thumbnailMultipageTextFormat = null;
        [DefaultValue("{0}")]
        public string ThumbnailMultipageTextFormat
        {
            get
            {
                if (_thumbnailMultipageTextFormat == null)
                    return _thumbnailTextFormat;
                return _thumbnailMultipageTextFormat;
            }
            set
            {
                _thumbnailMultipageTextFormat = value;
            }
        }



        List<string> _imageFileExtensions = new List<string>();
        /// <summary>
        /// Gets a list of image file extensions.
        /// </summary>
        public List<string> ImageFileExtensions
        {
            get
            {
                return _imageFileExtensions;
            }
        }

        string _selectedFolder = null;
        /// <summary>
        /// Gets or sets the current folder.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SelectedFolder
        {
            get
            {
                return _selectedFolder;
            }
            set
            {
                if (_selectedFolder != value)
                {
                    if (!DesignMode)
                    {
                        _fileSystemWatcher.EnableRaisingEvents = false;
                        StopThumbnailCreationThread();
                        _selectedFolder = value;
                        Images.ClearAndDisposeItems();
                        if (_selectedFolder == null)
                            return;

                        string[] files = Directory.GetFiles(_selectedFolder);
                        Array.Sort(files);
                        ImageCollection fileThumbnails = new ImageCollection();
                        foreach (string file in files)
                        {
                            string extension = Path.GetExtension(file).ToUpperInvariant();
                            if (_imageFileExtensions.Contains(extension))
                            {
                                VintasoftImage icon = GetFileThumbnail(extension);
                                icon.Tag = new FileIconTagData(file);
                                fileThumbnails.Add(icon);
                            }
                        }
                        Images.AddRange(fileThumbnails.ToArray());

                        _fileSystemWatcher.Path = _selectedFolder;
                        _fileSystemWatcher.EnableRaisingEvents = true;

                        _thumbnailLoadIndexes.Clear();
                        for (int i = Images.Count - 1; i >= 0; i--)
                            _thumbnailLoadIndexes.Push(i);
                        StartThumbnailCreationThread();
                    }
                }
            }
        }

        VintasoftImage _errorFileIcon;
        /// <summary>
        /// Gets or sets an icon of file with errors.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public VintasoftImage ErrorFileIcon
        {
            get
            {
                if (!DesignMode)
                {
                    if (_errorFileIcon == null)
                        _errorFileIcon = (VintasoftImage)DefaultFileIcon.Clone();
                }
                return _errorFileIcon;
            }
            set
            {
                _errorFileIcon = value;
            }
        }

        VintasoftImage _defaultFileIcon;
        /// <summary>
        /// Gets or sets the default icon of file.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public VintasoftImage DefaultFileIcon
        {
            get
            {
                if (!DesignMode)
                {
                    if (_defaultFileIcon == null)
                    {
                        _defaultFileIcon = new VintasoftImage(ThumbnailSize.Width, ThumbnailSize.Height, PixelFormat.Bgr24);
                        ClearImageCommand clearImage = new ClearImageCommand(Color.Black);
                        clearImage.ExecuteInPlace(_defaultFileIcon);
                    }
                }
                return _defaultFileIcon;
            }
            set
            {
                _defaultFileIcon = value;
            }
        }

        Dictionary<string, VintasoftImage> _fileTypeIcons = new Dictionary<string, VintasoftImage>();
        /// <summary>
        /// Gets or sets the dictionary: file extension => icon image.
        /// </summary>
        public Dictionary<string, VintasoftImage> FileTypeIcons
        {
            get
            {
                return _fileTypeIcons;
            }
        }

        #endregion



        #region Methods

        #region PROTECTED

        /// <summary>
        /// Raises the <see cref="FocusedIndexChanged"/> event.
        /// </summary>
        /// <param name="e">An <see cref="FocusedIndexChangedEventArgs"/> that contains the event data.</param>
        protected override void OnFocusedIndexChanged(FocusedIndexChangedEventArgs e)
        {
            base.OnFocusedIndexChanged(e);

            if (e.FocusedIndex > 0)
                if (!((FileIconTagData)Images[e.FocusedIndex].Tag).IsLoaded)
                    _thumbnailLoadIndexes.Push(e.FocusedIndex);
        }

        /// <summary>
        /// Raises the <see cref="System.Windows.Forms.Control.DragEnter">DragEnter</see> event.
        /// </summary>
        /// <param name="drgevent">A <see cref="System.Windows.Forms.DragEventArgs"/>
        /// that contains the event data.</param>
        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            // allows file copy
            if (drgevent.Data.GetDataPresent("FileNameW"))
                base.OnDragEnter(drgevent);
        }

        /// <summary>
        /// Raises the <see cref="ThumbnailPainting"/> event.
        /// </summary>
        /// <param name="sender">Painting thumbnail.</param>
        /// <param name="e">A <see cref="ThumbnailPaintEventArgs"/> that contains the event data.</param>
        protected override void OnThumbnailPainting(object sender, ThumbnailPaintEventArgs e)
        {
            if (!e.Handled)
            {
                Graphics g = e.Graphics;
                Thumbnail thumbnail = e.Thumbnail;
                using (VintasoftImage image = thumbnail.GetThumbnailImage(ThumbnailSize.Width, ThumbnailSize.Height))
                {
                    e.Handled = true;

                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                    int w = (int)g.VisibleClipBounds.Width;
                    int h = (int)g.VisibleClipBounds.Height - TextSpaceHeight;

                    Rectangle rect = new Rectangle((w - image.Width) / 2, (h - image.Height) / 2, image.Width, image.Height);
                    image.Draw(g, rect, new Rectangle(0, 0, image.Width, image.Height));

                    FileIconTagData data = (FileIconTagData)thumbnail.Source.Tag;

                    string textFormat;
                    if (data.PageCount > 1)
                        textFormat = ThumbnailMultipageTextFormat;
                    else
                        textFormat = ThumbnailTextFormat;

                    string text = string.Format(textFormat, Path.GetFileName(data.Filename), data.PageCount, data.DecoderName);

                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    RectangleF textRect = new RectangleF(_textPadding.Left, h + _textPadding.Top, w - _textPadding.Horizontal, TextSpaceHeight - _textPadding.Vertical);
                    g.DrawString(text, Font, new SolidBrush(ForeColor), textRect, format);
                }

                base.OnThumbnailPainting(sender, e);
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the
        /// <see cref="System.Windows.Forms.Control"/> and its child controls and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether to release both managed and unmanaged resources.
        /// <b>True</b> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _fileSystemWatcher.Dispose();
                StopThumbnailCreationThread();

                if (_errorFileIcon != null)
                {
                    _errorFileIcon.Dispose();
                    _errorFileIcon = null;
                }
                if (_defaultFileIcon != null)
                {
                    _defaultFileIcon.Dispose();
                    _defaultFileIcon = null;
                }
                VintasoftImage[] images = new VintasoftImage[_fileTypeIcons.Count];
                _fileTypeIcons.Values.CopyTo(images, 0);
                _fileTypeIcons.Clear();
                for (int i = 0; i < images.Length; i++)
                    images[i].Dispose();
            }
        }

        #endregion


        #region PRIVATE

        /// <summary>
        /// Handles the SelectedFolderRenamed event of fileSystemWatcher object.
        /// </summary>
        private void fileSystemWatcher_SelectedFolderRenamed(object sender, RenamedEventArgs e)
        {
            if (InvokeRequired)
                Invoke(new SelectedFolderChangedDelegate(SelectedFolderChanged), e);
            else
                SelectedFolderChanged(e);
        }

        /// <summary>
        /// Handles the SelectedFolderChanged event of fileSystemWatcher object.
        /// </summary>
        private void fileSystemWatcher_SelectedFolderChanged(object sender, FileSystemEventArgs e)
        {
            if (InvokeRequired)
                Invoke(new SelectedFolderChangedDelegate(SelectedFolderChanged), e);
            else
                SelectedFolderChanged(e);
        }

        private void SelectedFolderChanged(FileSystemEventArgs e)
        {
            VintasoftImage icon;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    icon = GetIconFromFilename(e.FullPath);
                    if (icon != null)
                    {
                        ((FileIconTagData)icon.Tag).Reload();
                        icon.SetImage(GetFileThumbnail(Path.GetExtension(e.FullPath)));
                        ReloadIcon(icon);
                    }
                    else
                    {
                        string extension = Path.GetExtension(e.FullPath).ToUpperInvariant();
                        if (_imageFileExtensions.Contains(extension))
                        {
                            icon = GetFileThumbnail(extension);
                            icon.Tag = new FileIconTagData(e.FullPath);
                            int insertIndex = FindInsertIndex(e.FullPath);
                            lock (Images)
                            {
                                Images.Insert(insertIndex, icon);
                                lock (_thumbnailLoadIndexes)
                                {
                                    if (_thumbnailLoadIndexes.Count != 0)
                                        ReloadIcons();
                                    else
                                        ReloadIcon(icon);
                                }
                            }
                        }
                    }
                    break;

                case WatcherChangeTypes.Deleted:
                    RemoveIcon(e.FullPath);
                    break;

                case WatcherChangeTypes.Renamed:
                    RenamedEventArgs renamedArgs = (RenamedEventArgs)e;
                    icon = GetIconFromFilename(renamedArgs.OldFullPath);
                    if (icon != null)
                    {
                        lock (Images)
                        {

                            Images.Remove(icon);
                            ((FileIconTagData)icon.Tag).Filename = e.FullPath;
                            Images.Insert(FindInsertIndex(e.FullPath), icon);
                        }
                    }
                    break;
            }
        }

        private void RemoveIcon(string filename)
        {
            VintasoftImage icon = GetIconFromFilename(filename);
            if (icon != null)
            {
                lock (Images)
                {
                    Images.Remove(icon);
                    icon.Dispose();
                    ReloadIcons();
                }
            }
        }

        private int FindInsertIndex(string filename)
        {
            filename = filename.ToLowerInvariant();
            for (int i = 0; i < Images.Count; i++)
                if (string.Compare(filename, ((FileIconTagData)Images[i].Tag).Filename.ToLowerInvariant()) < 0)
                    return i;
            return Images.Count;
        }

        private VintasoftImage GetIconFromFilename(string filename)
        {
            filename = filename.ToLowerInvariant();
            for (int i = 0; i < Images.Count; i++)
                if (((FileIconTagData)Images[i].Tag).Filename.ToLowerInvariant() == filename)
                    return Images[i];
            return null;
        }

        private void ReloadIcon(VintasoftImage icon)
        {
            int index = Images.IndexOf(icon);
            lock (_thumbnailLoadIndexes)
                _thumbnailLoadIndexes.Push(index);
            if (_thumbnailCreationThread == null)
                StartThumbnailCreationThread();
            if (index == FocusedIndex)
                OnFocusedIndexChanged(new FocusedIndexChangedEventArgs(index, true));
        }

        private void ReloadIcons()
        {
            lock (_thumbnailLoadIndexes)
            {
                _thumbnailLoadIndexes.Clear();
                lock (Images)
                    for (int i = Images.Count - 1; i >= 0; i--)
                        _thumbnailLoadIndexes.Push(i);
            }
            if (_thumbnailCreationThread == null)
                StartThumbnailCreationThread();
            OnFocusedIndexChanged(new FocusedIndexChangedEventArgs(FocusedIndex, true));
        }

        private void StartThumbnailCreationThread()
        {
            if (_thumbnailCreationThread != null)
                StopThumbnailCreationThread();
            _thumbnailCreationThread = new Thread(LoadThumbnails);
            _thumbnailCreationThread.IsBackground = true;
            _thumbnailCreationThread.Priority = ThreadPriority.Lowest;
            _thumbnailCreationThread.Start();
        }

        private void StopThumbnailCreationThread()
        {
            lock (_thumbnailLoadIndexes)
                _thumbnailCreationThread = null;
        }

        private void LoadThumbnail(int index)
        {
            FileIconTagData data = (FileIconTagData)Images[index].Tag;
            if (!data.IsLoaded)
            {
                VintasoftImage image = GetFileThumbnail(data, ThumbnailSize.Width, ThumbnailSize.Height - TextSpaceHeight);
                if (image != null && !Images.IsDisposed)
                    Images[index].SetImage(image);
            }
        }

        private void LoadThumbnails()
        {
            while (true)
            {
                lock (_thumbnailLoadIndexes)
                {
                    if (_thumbnailCreationThread == null)
                        break;

                    if (_thumbnailLoadIndexes.Count == 0)
                        break;

                    try
                    {
                        LoadThumbnail(_thumbnailLoadIndexes.Pop());
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            _thumbnailCreationThread = null;
        }

        /// <summary>
        /// Returns a file thumbnail.
        /// </summary>
        private VintasoftImage GetFileThumbnail(string extension)
        {
            extension = extension.ToUpperInvariant();
            VintasoftImage icon = null;
            if (FileTypeIcons.ContainsKey(extension))
                icon = (VintasoftImage)FileTypeIcons[extension].Clone();
            else
                icon = (VintasoftImage)DefaultFileIcon.Clone();
            return icon;
        }

        /// <summary>
        /// Returns a file thumbnail of specified size.
        /// </summary>
        private VintasoftImage GetFileThumbnail(FileIconTagData fileIconData, int width, int height)
        {
            try
            {
                using (Stream imageFileStream = new FileStream(fileIconData.Filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (DecoderBase decoder = AvailableDecoders.CreateDecoder(imageFileStream))
                {
                    DocumentPasswordForm.Authenticate(decoder);

                    VintasoftImage result = decoder.GetThumbnail(0, new Size(width, height));
                    fileIconData.OnIconLoaded(decoder.Name, decoder.PageCount);
                    return result;
                }
            }
            catch
            {
                fileIconData.OnIconLoaded(null, 0);
                return (VintasoftImage)ErrorFileIcon.Clone();
            }

        }

        #endregion

        #endregion


        #region Delegates

        delegate void SelectedFolderChangedDelegate(FileSystemEventArgs e);

        #endregion

    }
}
