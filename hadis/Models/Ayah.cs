using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace hadis.Models
{
    public class Ayah : INotifyPropertyChanged
    {
        public int Number { get; set; }
        public string ArabicText { get; set; }
        public string Translation { get; set; }
        public string Transliteration { get; set; }

        private bool _isSaved;
        public bool IsSaved
        {
            get => _isSaved;
            set
            {
                if (_isSaved != value)
                {
                    _isSaved = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}