using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FreeMote.Editor.Annotations;
using FreeMote.Psb;
using FreeMote.Psb.Textures;
using FreeMote.PsBuild;
using Newtonsoft.Json;

namespace FreeMote.Editor.Models
{
    class PsbJsonViewModel : INotifyPropertyChanged, IDisposable
    {
        public Dispatcher WindowDispatcher { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public ImageSource RenderImage
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
        private DrawingImage _renderImage = new DrawingImage();
        private DrawingGroup _drawingGroup = new DrawingGroup();

        public void Load(string path)
        {
            Model = PsbCompiler.LoadPsbFromJsonFile(path);

            Painter = new PsbPainter(Model);
            _drawingGroup = new DrawingGroup();
            _renderImage = new DrawingImage(_drawingGroup);

            _workDir = Path.GetDirectoryName(path);
            var resxPath = Path.ChangeExtension(path, ".resx.json");
            _resxJson = JsonConvert.DeserializeObject<PsbResourceJson>(File.ReadAllText(resxPath));
            _resDic = new Dictionary<string, ResourceMetadata>();
            var psbResMds = Model.CollectResources();
            foreach (var resxJsonResource in _resxJson.Resources)
            {
                var md = psbResMds.Find(m => m.GetFriendlyName(Model.Type) == resxJsonResource.Key);
                if (md == null)
                {
                    continue;
                }
                //Path.Combine(_workDir, resxJsonResource.Value).Replace('/', Path.PathSeparator).Replace('\\', Path.PathSeparator)
                _resDic.Add(Path.GetFullPath(Path.Combine(_workDir, resxJsonResource.Value)), md);
            }

            var resDir = Path.Combine(_workDir, Path.GetDirectoryName(_resxJson.Resources.First().Value));
            if (Directory.Exists(resDir))
            {
                _watcher = new FileSystemWatcher(resDir);
                _watcher.Changed += WatcherOnChanged;
                _watcher.EnableRaisingEvents = true;
            }
            else
            {
                throw new DirectoryNotFoundException(resDir);
            }
            Update();
        }

        private object _lock = new object();
        private Dictionary<string, CancellationTokenSource> _cancellationTokens = new Dictionary<string, CancellationTokenSource>();

        private async void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (Model == null)
            {
                return;
            }
            if (_resDic.ContainsKey(e.FullPath))
            {
                try
                {
                    if (!_cancellationTokens.ContainsKey(e.FullPath))
                    {
                        _cancellationTokens.Add(e.FullPath, new CancellationTokenSource());
                    }
                    else
                    {
                        _cancellationTokens[e.FullPath].Cancel();
                        _cancellationTokens[e.FullPath] = new CancellationTokenSource();
                    }

                    await Task.Delay(200, _cancellationTokens[e.FullPath].Token);
                    using (var bmp = new Bitmap(e.FullPath))
                    {
                        _resDic[e.FullPath].SetData(bmp);
                    }

                    Update();
                }
                catch (ArgumentException) //TODO: still bug when operating fast
                {
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        public void Update()
        {
            WindowDispatcher.InvokeAsync(() =>
            {
                _drawingGroup.Children.Clear();
                if (Painter.Resources?.Count <= 0)
                {
                    Painter.UpdateResource();
                }
                else
                {
                    Model.CollectSpiltedResources().ForEach(md =>
                        Painter.Resources.First(r => r.Part == md.Part && r.Name == md.Name).Data = md.Data);
                }

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
                    _drawingGroup.Children.Add(new ImageDrawing(res.ToImage().ToBitmapSource(),
                        new Rect(res.OriginX - res.Width / 2d, res.OriginY - res.Height / 2d, res.Width, res.Height)));
                    //g.DrawImage(res.ToImage(), new PointF(res.OriginX + width / 2f, res.OriginY + height / 2f));
                    //_renderImage. DrawImage(res.ToImage(), new PointF(res.OriginX + width / 2f - res.Width / 2f, res.OriginY + height / 2f - res.Height / 2f));
                }
            });
            //https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/how-to-draw-an-image-using-imagedrawing

            OnPropertyChanged(nameof(RenderImage));
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
