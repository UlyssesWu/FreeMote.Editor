using System.ComponentModel;
using System.Runtime.CompilerServices;
using FreeMote.Editor.Annotations;
using FreeMote.Psb;

namespace FreeMote.Editor.Models
{
    class PsbViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PSB Model { get; set; }
        internal PsbPainter Painter { get; set; }

        public void Update()
        { }

        public bool ReplaceTexture()
        {
            return true;
        }
    }
}
