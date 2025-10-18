using System;

namespace FastFoodApp.Core.Models
{
    public class License
    {
        public int Id { get; set; }
        public string Key { get; set; }           // licencia/ticket
        public string Type { get; set; }          // "Demo" / "Paga"
        public DateTime InstallDate { get; set; }
        public DateTime? ExpiryDate { get; set; } // demo tiene expiry
        public string HardwareHash { get; set; }  // hash de la maquina
    }
}
