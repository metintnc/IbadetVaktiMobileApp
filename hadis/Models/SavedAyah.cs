using System;

namespace hadis.Models
{
    public class SavedAyah : Ayah
    {
        public int SureNo { get; set; }
        public string SureName { get; set; }
        public DateTime SavedDate { get; set; }
    }
}
