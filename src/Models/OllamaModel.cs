using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Agent007.Models
{
    /**
     * Represents a model with its status, name, display name, description, size, and download progress.
     */
    public class OllamaModel : INotifyPropertyChanged
    {
        private Statuses _status;
        private float _downloadProgress = 0.0f;
        public required Statuses Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }


        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Description { get; set; }
        public required string Size { get; set; }
        public required float DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public enum Statuses
        {
            NotInstalled,
            Downloading,
            Installed
        }
    }

}
