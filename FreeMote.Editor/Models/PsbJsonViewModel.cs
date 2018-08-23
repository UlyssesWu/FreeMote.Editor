using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FreeMote.Editor.Annotations;
using FreeMote.Psb;
using FreeMote.PsBuild;
using Newtonsoft.Json;

namespace FreeMote.Editor.Models
{
    class PsbJsonViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public BitmapSource RenderImage
        {
            get => _renderImage;
        }

        public PSB Model { get; set; }
        internal PsbPainter Painter { get; set; }

        private FileSystemWatcher _watcher;
        private PsbResourceJson _resxJson;
        private string _workDir;
        private Dictionary<string, ResourceMetadata> _resDic = new Dictionary<string, ResourceMetadata>();
        //private BitmapSource _renderImage;
        private WriteableBitmap _renderImage = BitmapFactory.New(4096, 4096);

        public void Load(string path)
        {
            Model = PsbCompiler.LoadPsbFromJsonFile(path);
            _workDir = Path.GetDirectoryName(path);
            _resxJson = JsonConvert.DeserializeObject<PsbResourceJson>(Path.ChangeExtension(path, ".resx.json"));
            _resDic = new Dictionary<string, ResourceMetadata>();
            var psbResMds = Model.CollectResources();
            foreach (var resxJsonResource in _resxJson.Resources)
            {
                var md = psbResMds.Find(m => m.GetFriendlyName(Model.Type) == resxJsonResource.Key);
                if (md == null)
                {
                    continue;
                }

                _resDic.Add(Path.Combine(_workDir, resxJsonResource.Value), md);
            }
            var resDir = Path.ChangeExtension(path, null);
            if (Directory.Exists(resDir))
            {
                _watcher = new FileSystemWatcher();
                _watcher.Changed += WatcherOnChanged;
            }
            else
            {
                throw new DirectoryNotFoundException(resDir);
            }
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (Model == null)
            {
                return;
            }
            if (_resDic.ContainsKey(e.FullPath))
            {
                _resDic[e.FullPath].SetData(new Bitmap(e.FullPath));
                Update();
            }
        }

        public void Update()
        {
            //https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/how-to-draw-an-image-using-imagedrawing
            using (_renderImage.GetBitmapContext())
            {
                _renderImage.Clear();

                foreach (var res in Painter.Resources)
                {
                    if (res.Opacity <= 0 || !res.Visible)
                    {
                        continue;
                    }

                    if (res.Name.StartsWith("icon") && res.Name != "icon1")
                    {
                        continue;
                    }

                    Debug.WriteLine(
                        $"Drawing {res} at {res.OriginX},{res.OriginY} w:{res.Width},h:{res.Height}");
                    //g.DrawImage(res.ToImage(), new PointF(res.OriginX + width / 2f, res.OriginY + height / 2f));
                    //_renderImage. DrawImage(res.ToImage(), new PointF(res.OriginX + width / 2f - res.Width / 2f, res.OriginY + height / 2f - res.Height / 2f));
                }
            }
        }

        public bool ReplaceTexture()
        {
            return true;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
