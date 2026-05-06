namespace AipptAddIn.Models
{
    public class AvatarAssetOption
    {
        public string AssetId { get; set; }
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public bool IsAnimated { get; set; }

        public AvatarAssetOption()
        {
            AssetId = string.Empty;
            DisplayName = string.Empty;
            FileName = string.Empty;
            Description = string.Empty;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
