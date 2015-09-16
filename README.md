# windows-store-updater
Outil d'installation et de mise à jour à distance d'application Windows 8/Windows 8.1


# Comment l'installer
# Se connecter à une API renvoyant le modèle JSON au format suivant : 
        [DataMember]
        public string Version { get; set; } // Numéro de version du package à installer
        // Par exemple Version = "1.5.0.6"
        [DataMember]
        public DateTime ReleaseDate { get; set; } // Date de publication de la version
        // Par exemple ReleaseDate = {17/08/2015 11:57:00}
        [DataMember]
        public bool IsEnabled { get; set; } // Détermine si la version est disponible ou non true / false
        [DataMember]
        public string DownloadUrl { get; set; } // Url où download le fichier
        // Par exemple DownloadUrl = "http://goood-api.fr/immotouch/1.5.0.6/download"
        
# Activer la restauration des packages nugets
1. Faites un clic droit sur votre solution "wsu" puis "Activer la restauration des packages nugets"
        
# Créer le fichier settings.config
1. Copiez le fichier settings.config.sample présent sous WSU.CLIENT\Exploitation
2. Retirez l'extension .sample et modifiez les clés de valeur pour faire correspondre à votre API

