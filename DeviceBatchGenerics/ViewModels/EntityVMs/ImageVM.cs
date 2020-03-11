using System.Drawing;
using System.IO;
using DeviceBatchGenerics.Support.Bases;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class ImageVM : CrudVMBase
    {
        public ImageVM()
        {
            TheImage = new EFDeviceBatchCodeFirst.Image();
            //ctx.Entry(TheImage).State = System.Data.Entity.EntityState.Added;
        }
        public ImageVM(EFDeviceBatchCodeFirst.Image image)
        {
            TheImage = image;
            CroppedImagePath = TheImage.FilePath.Replace(@"\Images", @"\Cropped Images");
            if (File.Exists(CroppedImagePath))
                DisplayedImagePath = CroppedImagePath;
            else
            {
                Directory.CreateDirectory(CroppedImagePath.Substring(0, CroppedImagePath.LastIndexOf(@"\")));
                CropImage();
            }
        }
        #region Members
        private int _cropRectXCoord = 803;
        private int _cropRectYCoord = 382;
        private int _cropRectWidth = 315;
        private int _cropRectHeight = 315;
        private string _displayedImagePath;
        private string _croppedImagePath;
        private string _label;
        #endregion
        #region Properties
        public string CroppedImagePath
        {
            get { return _croppedImagePath; }
            set
            {
                _croppedImagePath = value;
                OnPropertyChanged();
            }
        }
        public string DisplayedImagePath
        {
            get { return _displayedImagePath; }
            set
            {
                _displayedImagePath = value;
                OnPropertyChanged();
            }
        }
        public string Label
        {
            get { return _label; }
            set
            {
                _label = value;
                OnPropertyChanged();
            }
        }
        public EFDeviceBatchCodeFirst.Image TheImage { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Crop the image to remove most of the black space.
        /// (This is a really lazy way to do this because it assumes an image with resolution 1920x1080 and flies blind
        /// in that no information about the pixel location is acquired. Much room for improvement.)
        /// </summary>
        private void CropImage()
        {
            DisplayedImagePath = null;
            byte[] photoBytes = File.ReadAllBytes(TheImage.FilePath);
            MemoryStream ms = new MemoryStream(photoBytes);
            var imageToCrop = new Bitmap(ms);
            var cropArea = new Rectangle(_cropRectXCoord, _cropRectYCoord, _cropRectWidth, _cropRectHeight);
            var croppedImage = imageToCrop.Clone(cropArea, imageToCrop.PixelFormat);
            croppedImage.Save(CroppedImagePath);
            ms.Close();
            DisplayedImagePath = CroppedImagePath;
        }
        #endregion
    }

}
