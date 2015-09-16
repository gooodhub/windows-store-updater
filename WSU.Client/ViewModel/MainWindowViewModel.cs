using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using WSU.Models;

namespace WSU.Client.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private UpdateInformation _apiVersion;
        private string _currentVersion;

        public UpdateInformation ApiVersion
        {
            get { return _apiVersion; }
            set
            {
                _apiVersion = value;
                RaisePropertyChanged();
                RaisePropertyChanged("VersionOk");
                RaisePropertyChanged("DisplayMessage");
                RaisePropertyChanged("MessageVersionAPI");
            }
        }

        public string CurrentVersion
        {
            get { return _currentVersion; }
            set
            {
                _currentVersion = value;
                RaisePropertyChanged();
                RaisePropertyChanged("VersionOk");
                RaisePropertyChanged("DisplayMessage");
                RaisePropertyChanged("MessageVersionActuelle");
            }
        }

        public string MessageVersionActuelle
        {
            get { return string.Format("Version installée : {0}", CurrentVersion ?? "Aucune"); }
        }

        public string MessageVersionAPI
        {
            get
            {
                return string.Format("Version disponible : {0}",
                    ApiVersion != null ? ApiVersion.Version : "Récupération ...");
            }

        }

        public string DisplayMessage
        {
            get
            {
                if (!VersionOk.HasValue) return "Vérification en cours ...";

                if (string.IsNullOrWhiteSpace(CurrentVersion))
                    return "Installation Nécessaire";

                return VersionOk.Value
                    ? "Dernière Version Installée"
                    : "Nouvelle Version Disponible";
            }
        }

        public bool? VersionOk
        {
            get
            {
                if (ApiVersion == null || CurrentVersion == null) return null;
                return CurrentVersion == ApiVersion.Version;
            }
        }
    }
}
