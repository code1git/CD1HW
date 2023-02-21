namespace CD1HW
{
    public class AppSettings
    {
        public const string Settings = "AppSettings";
        public bool DemoUIOnStart { get; set; }
        public int CamIdx { get; set; }
        public string CameraBackend { get; set; }
        public string NecDemoFilePath { get; set;}
        public string NecDemoFileType { get; set; }
        public string ProductType { get; set; }
        public string SignPadFont { get; set; }
        public string ResultPath { get; set; }
    }
}

