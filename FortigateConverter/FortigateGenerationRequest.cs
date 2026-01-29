namespace FortigateConverter
{
    public class FortigateGenerationRequest
    {
        // Properties representing the request data
        // Property for the raw text list of MAC addresses
        public string MacAddressList { get; set; } = string.Empty;

        // Property for the Fortigate name
        public string FortigateName { get; set; } = string.Empty;

        // Property for the group choice (1-4)
        public int GroupChoice { get; set; }
    }
}
