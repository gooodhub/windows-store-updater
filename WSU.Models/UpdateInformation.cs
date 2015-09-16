using System;
using System.Runtime.Serialization;

namespace WSU.Models
{
    [DataContract]
    public class UpdateInformation
    {
        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public DateTime ReleaseDate { get; set; }
        [DataMember]
        public bool IsEnabled { get; set; }
        [DataMember]
        public string DownloadUrl { get; set; }
    }
}
